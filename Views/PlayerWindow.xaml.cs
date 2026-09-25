using System.Windows;
using System.Windows.Media.Animation;
using AppleMusicPlayer.Helpers;

namespace AppleMusicPlayer.Views;

public partial class PlayerWindow : Window
{
    private const double CompactWidth = 264;
    private const double CompactHeight = 52;
    private const double ExpandedWidth = 360;
    private const double ExpandedHeight = 218;
    private bool _isExpanded;

    public PlayerWindow()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        WindowPositionHelper.PositionAtPrimaryScreenTop(this);
    }

    private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        Expand();
    }

    private async void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        await Task.Delay(180);
        if (!IsMouseOver)
            Collapse();
    }

    private void Expand()
    {
        if (_isExpanded)
            return;

        _isExpanded = true;
        ExpandedContent.Visibility = Visibility.Visible;
        AnimateSize(ExpandedWidth, ExpandedHeight, 260);
        AnimateOpacity(CompactContent, 0, 100);
        AnimateOpacity(ExpandedContent, 1, 230);
    }

    private void Collapse()
    {
        if (!_isExpanded)
            return;

        _isExpanded = false;
        AnimateSize(CompactWidth, CompactHeight, 220);
        AnimateOpacity(ExpandedContent, 0, 120, () => ExpandedContent.Visibility = Visibility.Collapsed);
        AnimateOpacity(CompactContent, 1, 190);
    }

    private void AnimateSize(double width, double height, int duration)
    {
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        BeginAnimation(WidthProperty, new DoubleAnimation(Width, width, TimeSpan.FromMilliseconds(duration)) { EasingFunction = easing });
        BeginAnimation(HeightProperty, new DoubleAnimation(Height, height, TimeSpan.FromMilliseconds(duration)) { EasingFunction = easing });
        Width = width;
        Height = height;
        WindowPositionHelper.PositionAtPrimaryScreenTop(this);
    }

    private static void AnimateOpacity(UIElement element, double opacity, int duration, Action? completed = null)
    {
        var animation = new DoubleAnimation(opacity, TimeSpan.FromMilliseconds(duration));
        if (completed is not null)
            animation.Completed += (_, _) => completed();
        element.BeginAnimation(OpacityProperty, animation);
    }
}