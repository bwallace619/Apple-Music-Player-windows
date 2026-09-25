using System.Drawing;
using System.Windows;
using Forms = System.Windows.Forms;
using WpfApplication = System.Windows.Application;

namespace AppleMusicPlayer;

public partial class App : WpfApplication
{
    private Forms.NotifyIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _trayIcon = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Apple Music Player",
            Visible = true,
            ContextMenuStrip = CreateTrayMenu()
        };
    }

    private static Forms.ContextMenuStrip CreateTrayMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Show player", null, (_, _) => ShowPlayer());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => Current.Shutdown());
        return menu;
    }

    private static void ShowPlayer()
    {
        if (Current.MainWindow is not { } window)
            return;

        window.Show();
        window.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }

        base.OnExit(e);
    }
}