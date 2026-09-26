using System.Windows;
using System.Windows.Forms;
using WpfPoint = System.Windows.Point;

namespace AppleMusicPlayer.Helpers;

public static class WindowPositionHelper
{
    public static void PositionAtPrimaryScreenTop(Window window, double topOffset = 8)
    {
        var workArea = Screen.PrimaryScreen?.WorkingArea
                       ?? throw new InvalidOperationException("The primary display is unavailable.");

        var source = PresentationSource.FromVisual(window);
        var transform = source?.CompositionTarget?.TransformFromDevice;
        var origin = transform?.Transform(new WpfPoint(workArea.Left, workArea.Top))
                 ?? new WpfPoint(workArea.Left, workArea.Top);
        var size = transform?.Transform(new WpfPoint(workArea.Width, workArea.Height))
               ?? new WpfPoint(workArea.Width, workArea.Height);

        window.Left = origin.X + ((size.X - window.Width) / 2);
        window.Top = origin.Y + topOffset;
    }

    public static double GetPrimaryScreenCenteredLeft(Window window, double width)
    {
        var workArea = Screen.PrimaryScreen?.WorkingArea
                       ?? throw new InvalidOperationException("The primary display is unavailable.");
        var source = PresentationSource.FromVisual(window);
        var transform = source?.CompositionTarget?.TransformFromDevice;
        var origin = transform?.Transform(new WpfPoint(workArea.Left, workArea.Top))
                     ?? new WpfPoint(workArea.Left, workArea.Top);
        var size = transform?.Transform(new WpfPoint(workArea.Width, workArea.Height))
                   ?? new WpfPoint(workArea.Width, workArea.Height);
        return origin.X + ((size.X - width) / 2);
    }
}