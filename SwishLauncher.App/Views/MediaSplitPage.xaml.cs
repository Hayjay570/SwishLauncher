using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Media3D;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace SwishLauncher.App.Views;

public sealed partial class MediaSplitPage : Page
{
    private bool _animating;

    // The initial tilt angles (degrees). Negative = left card leans right toward centre.
    private const double LocalTilt     = -12.0;
    private const double StreamingTilt =  12.0;

    public MediaSplitPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    // ── Apply CompositeTransform3D tilt after layout ───────────────────────
    // Transform3D must be set before a Storyboard can target it.
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyTilts();
    }

    private void ApplyTilts()
    {
        LocalCard.Transform3D = new CompositeTransform3D { RotationY = LocalTilt };
        StreamingCard.Transform3D = new CompositeTransform3D { RotationY = StreamingTilt };
    }

    // ── Card click handlers ────────────────────────────────────────────────

    private void LocalCard_Click(object sender, RoutedEventArgs e)
    {
        if (_animating) return;
        AnimateAndNavigate(isLocal: true);
    }

    private void StreamingCard_Click(object sender, RoutedEventArgs e)
    {
        if (_animating) return;
        AnimateAndNavigate(isLocal: false);
    }

    // ── Open-book selection animation ──────────────────────────────────────
    //
    // Selected card: RotationY → 0  (faces forward, 320 ms EaseOut)
    // Other card:    Opacity   → 0  (192 ms EaseOut)
    // On complete: navigate.

    private void AnimateAndNavigate(bool isLocal)
    {
        _animating = true;
        LocalCardButton.IsEnabled     = false;
        StreamingCardButton.IsEnabled = false;

        var selectedCard = isLocal ? LocalCard : StreamingCard;
        var otherCard    = isLocal ? StreamingCard : LocalCard;

        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        var sb   = new Storyboard();

        // --- Rotate selected card to face forward ---
        var rotAnim = new DoubleAnimation
        {
            To             = 0,
            Duration       = new Duration(TimeSpan.FromMilliseconds(320)),
            EasingFunction = ease
        };
        Storyboard.SetTarget(rotAnim, selectedCard);
        // Property path to CompositeTransform3D.RotationY on UIElement.Transform3D
        Storyboard.SetTargetProperty(rotAnim,
            "(UIElement.Transform3D).(CompositeTransform3D.RotationY)");
        sb.Children.Add(rotAnim);

        // --- Fade out the other card ---
        var fadeAnim = new DoubleAnimation
        {
            To             = 0,
            Duration       = new Duration(TimeSpan.FromMilliseconds(192)),
            EasingFunction = ease
        };
        Storyboard.SetTarget(fadeAnim, otherCard);
        Storyboard.SetTargetProperty(fadeAnim, "Opacity");
        sb.Children.Add(fadeAnim);

        sb.Completed += (_, _) =>
        {
            if (isLocal)
                Frame.Navigate(typeof(MediaBrowserPage),
                    null, new DrillInNavigationTransitionInfo());
            else
                Frame.Navigate(typeof(StreamingPlaceholderPage),
                    null, new DrillInNavigationTransitionInfo());
        };

        sb.Begin();
    }

    // ── Reset state on back-navigation ────────────────────────────────────
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _animating = false;
        LocalCardButton.IsEnabled     = true;
        StreamingCardButton.IsEnabled = true;
        LocalCard.Opacity    = 1;
        StreamingCard.Opacity = 1;
        // Re-apply tilts in case Transform3D was mutated during animation
        ApplyTilts();
    }
}
