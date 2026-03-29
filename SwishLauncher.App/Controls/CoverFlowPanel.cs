using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Media3D;
using System;
using Windows.Foundation;

namespace SwishLauncher.App.Controls;

/// <summary>
/// Custom Panel that arranges its children in a coverflow layout:
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

    /// <summary>Horizontal offset from centre for each slot (in pixels).</summary>
    private const double SlotSpacing = 220;

    /// <summary>Perspective depth used for all 3D transforms.</summary>
    private const double PerspectiveDepth = 800;

    // Scale per slot distance from centre (0 = centre, 1 = adjacent, 2 = outer)
    private static readonly double[] ScaleByDistance   = [1.0,  0.78, 0.60];
    // Y-axis rotation in degrees per slot distance
    private static readonly double[] RotationByDistance = [0.0,  55.0, 65.0];
    // Opacity per slot distance
    private static readonly double[] OpacityByDistance  = [1.0,  0.75, 0.50];
    // Z translation (push side items slightly back)
    private static readonly double[] ZByDistance        = [0.0, -60.0, -120.0];

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

    private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((CoverFlowPanel)d).InvalidateArrange();

    // ── Measure ───────────────────────────────────────────────────────────

    protected override Size MeasureOverride(Size availableSize)
    {
        // Give every child the same desired size (a square based on available height)
        var childSize = new Size(availableSize.Height * 0.55, availableSize.Height * 0.75);

        foreach (UIElement child in Children)
            child.Measure(childSize);

        return availableSize;
    }

    // ── Arrange ───────────────────────────────────────────────────────────

    protected override Size ArrangeOverride(Size finalSize)
    {
        int count      = Children.Count;
        int selected   = Math.Clamp(SelectedIndex, 0, Math.Max(0, count - 1));
        double centreX = finalSize.Width  / 2.0;
        double centreY = finalSize.Height / 2.0;

        // Build a PlaneProjection for perspective on the panel itself
        if (Projection is not PlaneProjection)
            Projection = new PlaneProjection();

        for (int i = 0; i < count; i++)
        {
            var child    = Children[i];
            int distance = i - selected;          // negative = left of centre
            int absDist  = Math.Abs(distance);

            // Only the 5 visible slots (±2 from centre) are shown
            bool visible = absDist <= 2;
            child.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            if (!visible) { child.Arrange(new Rect(0, 0, 0, 0)); continue; }

            double childW = child.DesiredSize.Width;
            double childH = child.DesiredSize.Height;

            // Position: centre the slot, then offset by distance × spacing
            double x = centreX + distance * SlotSpacing - childW / 2.0;
            double y = centreY - childH / 2.0;

            child.Arrange(new Rect(x, y, childW, childH));

            // ── GPU-accelerated 3D transform via CompositeTransform3D ──────
            // CacheMode="BitmapCache" on each item in XAML ensures the
            // compositor handles these as textured quads — no CPU overdraw.
            var t3d = new CompositeTransform3D
            {
                // Perspective: Y-axis rotation tilts left items away on the right
                // and right items away on the left (mirror the rotation direction)
                RotationY   = RotationByDistance[absDist] * (distance < 0 ? 1 : -1),
                ScaleX      = ScaleByDistance[absDist],
                ScaleY      = ScaleByDistance[absDist],
                TranslateZ  = ZByDistance[absDist],
                // Centre the transform on the item's own centre
                CenterX     = childW / 2.0,
                CenterY     = childH / 2.0,
            };

            child.Transform3D = t3d;
            child.Opacity     = OpacityByDistance[absDist];

            // Centre items render on top; outer items render behind
            Canvas.SetZIndex(child, 10 - absDist);
        }

        return finalSize;
    }
}
