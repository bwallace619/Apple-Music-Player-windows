using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AppleMusicPlayer.Helpers;

public static class GlassHelper
{
    private const int DwmwaSystemBackdropType = 38;
    private const int DwmsbtMainWindow = 2;

    public static bool TryApply(Window window, bool enabled)
    {
        if (!enabled)
            return false;

        try
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
                return false;
            var backdrop = DwmsbtMainWindow;
            return DwmSetWindowAttribute(handle, DwmwaSystemBackdropType, ref backdrop, sizeof(int)) == 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);
}