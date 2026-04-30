using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections;
using System.Collections.Specialized;
using Windows.System;

namespace SwishLauncher.App.Controls;

/// <summary>
/// CoverFlow control — manages a CoverFlowPanel directly instead of going
/// through ItemsControl, which requires an ItemsPanelTemplate that the XAML
/// compiler cannot parse without x:DataType, and that XamlReader.Load cannot
/// resolve for local types without a full assembly context.
///
/// Instead we own the panel as a named child of the root Grid and manually
/// sync item containers whenever ItemsSource or ItemTemplate changes.
/// </summary>
public sealed partial class CoverFlowControl : UserControl
{
    private readonly CoverFlowPanel _panel;

    // ── Dependency properties ──────────────────────────────────────────────

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IList),
            typeof(CoverFlowControl), new PropertyMetadata(null, OnItemsSourceChanged));

    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(nameof(SelectedIndex), typeof(int),
            typeof(CoverFlowControl), new PropertyMetadata(0, OnSelectedIndexChanged));

    public static readonly DependencyProperty ItemTemplateProperty =
        DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate),
            typeof(CoverFlowControl), new PropertyMetadata(null, OnItemTemplateChanged));

    public IList? ItemsSource
    {
        get => (IList?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, Math.Clamp(value, 0, Math.Max(0, ItemCount - 1)));
    }

    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    private int ItemCount => ItemsSource?.Count ?? 0;

    // ── Events ─────────────────────────────────────────────────────────────

    public event EventHandler<object?>? ItemActivated;

    // ── Construction ───────────────────────────────────────────────────────

    public CoverFlowControl()
    {
        InitializeComponent();

        // Insert the panel behind the arrow buttons (index 0)
        _panel = new CoverFlowPanel();
        RootGrid.Children.Insert(0, _panel);

        KeyDown += OnKeyDown;
    }

    // ── Container sync ─────────────────────────────────────────────────────

    /// <summary>
    /// Rebuilds the panel's children from the current ItemsSource using
    /// the current ItemTemplate. Called whenever either changes.
    /// </summary>
    private void RebuildItems()
    {
        _panel.Children.Clear();

        if (ItemsSource is null || ItemTemplate is null) return;

        int index = 0;
        foreach (var item in ItemsSource)
        {
            var capturedIndex = index; // capture for closure
            var presenter = new ContentPresenter
            {
                Content         = item,
                ContentTemplate = ItemTemplate,
                CacheMode       = new Microsoft.UI.Xaml.Media.BitmapCache(),
            };

            // First tap on a side card → select it.
            // Tap on the already-selected centre card → activate it.
            presenter.Tapped += (_, _) =>
            {
                if (SelectedIndex == capturedIndex)
                    ActivateSelected();
                else
                    SelectedIndex = capturedIndex;
            };

            _panel.Children.Add(presenter);
            index++;
        }

        _panel.SelectedIndex = Math.Clamp(SelectedIndex, 0, Math.Max(0, ItemCount - 1));
    }

    // ── Property change callbacks ──────────────────────────────────────────

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (CoverFlowControl)d;

        // Unsubscribe from old collection notifications
        if (e.OldValue is INotifyCollectionChanged oldCol)
            oldCol.CollectionChanged -= ctrl.OnCollectionChanged;

        // Subscribe to new collection notifications so live adds/removes work
        if (e.NewValue is INotifyCollectionChanged newCol)
            newCol.CollectionChanged += ctrl.OnCollectionChanged;

        ctrl.RebuildItems();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => RebuildItems();

    private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (CoverFlowControl)d;
        ctrl._panel.SelectedIndex = (int)e.NewValue;
    }

    private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((CoverFlowControl)d).RebuildItems();

    // ── Navigation ─────────────────────────────────────────────────────────

    public void MoveLeft()
    {
        if (SelectedIndex > 0) SelectedIndex--;
    }

    public void MoveRight()
    {
        if (SelectedIndex < ItemCount - 1) SelectedIndex++;
    }

    public void ActivateSelected()
    {
        if (ItemsSource is not null && SelectedIndex >= 0 && SelectedIndex < ItemCount)
            ItemActivated?.Invoke(this, ItemsSource[SelectedIndex]);
    }

    // ── Input handlers ─────────────────────────────────────────────────────

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Left:
            case VirtualKey.GamepadLeftThumbstickLeft:
            case VirtualKey.GamepadDPadLeft:
                MoveLeft();
                e.Handled = true;
                break;

            case VirtualKey.Right:
            case VirtualKey.GamepadLeftThumbstickRight:
            case VirtualKey.GamepadDPadRight:
                MoveRight();
                e.Handled = true;
                break;

            case VirtualKey.Enter:
            case VirtualKey.Space:
            case VirtualKey.GamepadA:
                ActivateSelected();
                e.Handled = true;
                break;
        }
    }

    private void LeftArrow_Click(object sender, RoutedEventArgs e)  => MoveLeft();
    private void RightArrow_Click(object sender, RoutedEventArgs e) => MoveRight();
}
