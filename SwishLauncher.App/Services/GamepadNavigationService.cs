using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using Windows.Gaming.Input;

namespace SwishLauncher.App.Services;

/// <summary>
/// Handles gamepad input for SwishLauncher's two-tier navigation model:
///
///   RT / LT  →  cycle NavigationView tabs (wrap-around)
///   RB / LB  →  move focus between the major focus regions on the current page
///   A        →  invoke the currently focused element (same as Enter/Space)
///   B        →  go back (Frame.GoBack) if the back-stack is non-empty
///   D-pad    →  WinUI XY focus handles this automatically; no code needed here
///
/// Trigger note: In Windows.Gaming.Input, RightTrigger and LeftTrigger are
/// analog double values (0.0–1.0) on GamepadReading, NOT GamepadButtons flags.
/// We treat a value above TRIGGER_THRESHOLD as a press and debounce with a
/// "was pressed last tick" flag so holding the trigger does not repeat.
///
/// Usage — call once in MainWindow constructor:
///   _gamepadNav = GamepadNavigationService.Attach(NavigationViewControl, ContentFrame);
/// </summary>
public sealed class GamepadNavigationService : IDisposable
{
    // ── Constants ──────────────────────────────────────────────────────────
    /// <summary>Analog value above which a trigger is considered "pressed".</summary>
    private const double TriggerThreshold = 0.5;

    // ── State ──────────────────────────────────────────────────────────────
    private readonly NavigationView _navView;
    private readonly Frame _frame;
    private readonly DispatcherQueue _dispatcherQueue;

    private Gamepad? _gamepad;
    private DispatcherTimer? _pollTimer;

    // Digital button debounce
    private GamepadButtons _prevButtons = GamepadButtons.None;

    // Analog trigger debounce — true when trigger was above threshold last tick
    private bool _prevRightTrigger;
    private bool _prevLeftTrigger;

    // Shoulder-button focus regions — set per-page via RegisterFocusRegions
    private readonly List<UIElement> _focusRegions = [];
    private int _focusRegionIndex;

    // ── Lifecycle ──────────────────────────────────────────────────────────
    public GamepadNavigationService(NavigationView navView, Frame frame)
    {
        _navView = navView;
        _frame = frame;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        Gamepad.GamepadAdded   += OnGamepadAdded;
        Gamepad.GamepadRemoved += OnGamepadRemoved;

        // Pick up any gamepad already connected at startup
        if (Gamepad.Gamepads.Count > 0)
            _gamepad = Gamepad.Gamepads[0];

        StartPolling();
    }

    /// <summary>Convenience factory — keeps MainWindow constructor clean.</summary>
    public static GamepadNavigationService Attach(NavigationView navView, Frame frame)
        => new(navView, frame);

    /// <summary>
    /// Pages call this on NavigatedTo to register their focusable regions in
    /// logical order.  RB moves forward, LB moves backward.
    /// </summary>
    public void RegisterFocusRegions(params UIElement[] regions)
    {
        _focusRegions.Clear();
        _focusRegions.AddRange(regions);
        _focusRegionIndex = 0;
    }

    // ── Polling ────────────────────────────────────────────────────────────

    private void StartPolling()
    {
        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) }; // 20 Hz
        _pollTimer.Tick += OnPollTick;
        _pollTimer.Start();
    }

    private void OnPollTick(object? sender, object e)
    {
        if (_gamepad is null) return;

        var reading = _gamepad.GetCurrentReading();

        // ── RT / LT: analog triggers (0.0–1.0) ────────────────────────────
        // "Pressed this tick" = above threshold now AND was not above threshold last tick.
        bool rtNow = reading.RightTrigger >= TriggerThreshold;
        bool ltNow = reading.LeftTrigger  >= TriggerThreshold;

        if (rtNow && !_prevRightTrigger)
            _dispatcherQueue.TryEnqueue(() => ShiftTab(+1));
        else if (ltNow && !_prevLeftTrigger)
            _dispatcherQueue.TryEnqueue(() => ShiftTab(-1));

        _prevRightTrigger = rtNow;
        _prevLeftTrigger  = ltNow;

        // ── RB / LB: digital shoulder buttons (GamepadButtons enum flags) ──
        var buttons = reading.Buttons;
        var pressed = buttons & ~_prevButtons; // newly-down this tick only
        _prevButtons = buttons;

        if (pressed.HasFlag(GamepadButtons.RightShoulder))
            _dispatcherQueue.TryEnqueue(() => ShiftFocusRegion(+1));
        else if (pressed.HasFlag(GamepadButtons.LeftShoulder))
            _dispatcherQueue.TryEnqueue(() => ShiftFocusRegion(-1));

        // ── A: invoke focused element ──────────────────────────────────────
        // Sends VirtualKey.GamepadA to the focused element so Buttons, list
        // items, and CoverFlowControl all respond without needing special cases.
        if (pressed.HasFlag(GamepadButtons.A))
            _dispatcherQueue.TryEnqueue(InvokeFocusedElement);

        // ── B: go back ────────────────────────────────────────────────────
        if (pressed.HasFlag(GamepadButtons.B))
            _dispatcherQueue.TryEnqueue(GoBack);

        // D-pad: WinUI spatial navigation handles this automatically.
    }

    // ── A / B button actions ───────────────────────────────────────────────

    /// <summary>
    /// Invokes the currently focused element via its AutomationPeer.
    /// Works for Button (ButtonAutomationPeer.Invoke) and any control that
    /// exposes IInvokeProvider.  For CoverFlowControl the existing
    /// VirtualKey.GamepadA KeyDown handler fires first, so we don't need to
    /// handle that case here — the focus system delivers the key naturally.
    /// </summary>
    private static void InvokeFocusedElement()
    {
        if (FocusManager.GetFocusedElement() is not UIElement focused) return;

        var peer = Microsoft.UI.Xaml.Automation.Peers
            .FrameworkElementAutomationPeer.FromElement(focused);

        // IInvokeProvider covers Button, HyperlinkButton, AppBarButton, etc.
        if (peer?.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)
            is Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoker)
        {
            invoker.Invoke();
        }
    }

    /// <summary>
    /// Navigates back in the frame's back-stack if one exists.
    /// The NavigationView SelectionChanged + ContentFrame_Navigated in
    /// MainWindow will restore _intendedTag automatically.
    /// </summary>
    private void GoBack()
    {
        if (_frame.CanGoBack)
            _frame.GoBack();
    }

    // ── Tab cycling ────────────────────────────────────────────────────────

    private void ShiftTab(int delta)
    {
        var items = _navView.MenuItems;
        if (items.Count == 0) return;

        int current = items.IndexOf(_navView.SelectedItem);
        if (current < 0) current = 0;

        // Wrap-around in both directions
        int next = ((current + delta) % items.Count + items.Count) % items.Count;
        _navView.SelectedItem = items[next];
    }

    // ── In-page region cycling ─────────────────────────────────────────────

    private void ShiftFocusRegion(int delta)
    {
        if (_focusRegions.Count == 0) return;

        _focusRegionIndex =
            ((_focusRegionIndex + delta) % _focusRegions.Count + _focusRegions.Count)
            % _focusRegions.Count;

        _focusRegions[_focusRegionIndex].Focus(FocusState.Programmatic);
    }

    // ── Gamepad connect / disconnect ───────────────────────────────────────

    private void OnGamepadAdded(object? sender, Gamepad pad)
    {
        if (_gamepad is null) _gamepad = pad;
    }

    private void OnGamepadRemoved(object? sender, Gamepad pad)
    {
        if (_gamepad == pad)
            _gamepad = Gamepad.Gamepads.Count > 0 ? Gamepad.Gamepads[0] : null;
    }

    // ── IDisposable ────────────────────────────────────────────────────────

    public void Dispose()
    {
        _pollTimer?.Stop();
        Gamepad.GamepadAdded   -= OnGamepadAdded;
        Gamepad.GamepadRemoved -= OnGamepadRemoved;
    }
}
