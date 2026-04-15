using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Media3D;
using System;
using Windows.Foundation;

namespace SwishLauncher.App.Controls;

/// <summary>
/// Custom Panel that arranges its children in a coverflow layout with smooth
/// animation driven by a DispatcherTimer lerp loop.
///
///   - Centre child faces the user directly (no tilt, full scale, full opacity)
///   - ±1 children are tilted inward at a shallow angle, slightly scaled down
///   - ±2 children are tilted further, smaller and more transparent
///
/// GPU note: each child should have CacheMode="BitmapCache" set in XAML so
/// CompositeTransform3D operations are handled entirely by the compositor.
/// </summary>
public sealed class CoverFlowPanel : Panel
{
    // ── Layout constants ───────────────────────────────────────────────────

    private const double SlotSpacing = 220;
    private const double PerspectiveDepth = 800;

    // Scale per slot distance from centre (0 = centre, 1 = adjacent, 2 = outer)
    private static readonly double[] ScaleByDistance = [1.0, 0.78, 0.60];
    // Y-axis rotation in degrees per slot distance
    private static readonly double[] RotationByDistance = [0.0, 55.0, 65.0];
    // Opacity per slot distance
    private static readonly double[] OpacityByDistance = [1.0, 0.75, 0.50];
    // Z translation (push side items slightly back)
    private static readonly double[] ZByDistance = [0.0, -60.0, -120.0];

    // ── Animation state ───────────────────────────────────────────────────

    /// <summary>
    /// Fractional index that the arrange pass reads. Smoothly lerps toward
    /// SelectedIndex each timer tick, producing the slide animation.
    /// </summary>
    private double _animatedIndex = 0;

    /// <summary>How fast _animatedIndex catches up. 0 = never, 1 = instant.</summary>
    private const double LerpSpeed = 0.18;

    /// <summary>Stop animating when we're this close to the target.</summary>
    private const double LerpThreshold = 0.001;

    private readonly DispatcherTimer _timer;

    // ── Dependency property: SelectedIndex ────────────────────────────────

    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(
            nameof(SelectedIndex),
            typeof(int),
            typeof(CoverFlowPanel),
            new PropertyMetadata(0, OnSelectedIndexChanged));

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    private static void OnSelectedIndexChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var panel = (CoverFlowPanel)d;
        panel._timer.Start(); // kick off animation toward the new target
    }

    // ── Construction ──────────────────────────────────────────────────────

    public CoverFlowPanel()
    {
        // ~60 fps tick
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += OnTick;
    }

    private void OnTick(object? sender, object e)
    {
        double target = SelectedIndex;
        double delta = target - _animatedIndex;

        if (Math.Abs(delta) < LerpThreshold)
        {
            _animatedIndex = target;
            _timer.Stop();   // settled — stop burning ticks
        }
        else
        {
            _animatedIndex += delta * LerpSpeed;
        }

        InvalidateArrange();
    }

    // ── Measure ───────────────────────────────────────────────────────────

    protected override Size MeasureOverride(Size availableSize)
    {
        var childSize = new Size(
            availableSize.Height * 0.55,
            availableSize.Height * 0.75);

        foreach (UIElement child in Children)
            child.Measure(childSize);

        return availableSize;
    }

    // ── Arrange ───────────────────────────────────────────────────────────

    protected override Size ArrangeOverride(Size finalSize)
    {
        int count = Children.Count;
        double centreX = finalSize.Width / 2.0;
        double centreY = finalSize.Height / 2.0;

        if (Projection is not PlaneProjection)
            Projection = new PlaneProjection();

        for (int i = 0; i < count; i++)
        {
            var child = Children[i];

            // Fractional distance from the animated (non-snapped) centre
            double distance = i - _animatedIndex;
            double absDist = Math.Abs(distance);

            bool visible = absDist <= 2.5;
            child.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            if (!visible) { child.Arrange(new Rect(0, 0, 0, 0)); continue; }

            double childW = child.DesiredSize.Width;
            double childH = child.DesiredSize.Height;

            double x = centreX + distance * SlotSpacing - childW / 2.0;
            double y = centreY - childH / 2.0;
            child.Arrange(new Rect(x, y, childW, childH));

            // Interpolate all visual properties from the lookup tables
            double scale = Interpolate(ScaleByDistance, absDist);
            double rotY = Interpolate(RotationByDistance, absDist)
                                * (distance < 0 ? 1 : -1);
            double opacity = Interpolate(OpacityByDistance, absDist);
            double tz = Interpolate(ZByDistance, absDist);

            child.Transform3D = new CompositeTransform3D
            {
                RotationY = rotY,
                ScaleX = scale,
                ScaleY = scale,
                TranslateZ = tz,
                CenterX = childW / 2.0,
                CenterY = childH / 2.0,
            };

            child.Opacity = opacity;
            Canvas.SetZIndex(child, (int)(100 - absDist * 10));
        }

        return finalSize;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Linearly interpolates within a lookup table indexed by fractional distance.
    /// Clamps at the last table entry for distances beyond the table size.
    /// </summary>
    private static double Interpolate(double[] table, double t)
    {
        t = Math.Max(0, t);
        int lo = (int)Math.Floor(t);
        int hi = lo + 1;

        if (lo >= table.Length - 1) return table[^1];

        double frac = t - lo;
        return table[lo] + frac * (table[hi] - table[lo]);
    }
}