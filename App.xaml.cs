using System.Drawing;
using System.IO;
using System.Windows;
using AppleMusicPlayer.Services;
using AppleMusicPlayer.Views;
using Forms = System.Windows.Forms;
using WpfApplication = System.Windows.Application;

namespace AppleMusicPlayer;

public partial class App : WpfApplication
{
    private Forms.NotifyIcon? _trayIcon;
    private Icon? _trayIconImage;
    public SettingsService Settings { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        Settings.LoadAsync().GetAwaiter().GetResult();

        _trayIconImage = CreateTrayIconImage();
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = _trayIconImage ?? SystemIcons.Application,
            Text = "Apple Music Player",
            Visible = true,
            ContextMenuStrip = CreateTrayMenu()
        };
        _trayIcon.DoubleClick += (_, _) => ShowPlayer();
        Settings.SettingsChanged += OnSettingsChanged;
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogException("Dispatcher", e.Exception);
        e.Handled = true;
    }

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
            LogException("AppDomain", exception);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException("Task", e.Exception);
        e.SetObserved();
    }

    private static void LogException(string source, Exception exception)
    {
        try
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AppleMusicPlayer");
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "startup.log"), $"{DateTimeOffset.Now:u} [{source}] {exception}\r\n");
        }
        catch (Exception)
        {
            // Diagnostics must never become another source of failure.
        }
    }

    private void OnSettingsChanged(object? sender, Models.AppSettings settings)
    {
        if (_trayIcon is not null)
            _trayIcon.Visible = settings.EnableTrayIcon;
    }

    /// <summary>Draws a small music-note tray icon so the app is recognisable in the tray.</summary>
    private static Icon? CreateTrayIconImage()
    {
        try
        {
            using var bitmap = new Bitmap(32, 32);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.Clear(System.Drawing.Color.Transparent);
            using var background = new SolidBrush(System.Drawing.Color.FromArgb(255, 22, 23, 28));
            graphics.FillEllipse(background, 0, 0, 31, 31);
            var accent = System.Drawing.Color.FromArgb(255, 255, 55, 95);
            using var noteBrush = new SolidBrush(accent);
            using var notePen = new Pen(accent, 2.4f);
            graphics.FillEllipse(noteBrush, 7f, 19f, 7f, 5.5f);
            graphics.FillEllipse(noteBrush, 18f, 17f, 7f, 5.5f);
            graphics.DrawLine(notePen, 13.5f, 21.5f, 13.5f, 9f);
            graphics.DrawLine(notePen, 24.5f, 19.5f, 24.5f, 7f);
            graphics.DrawLine(notePen, 13.5f, 9f, 24.5f, 7f);
            return Icon.FromHandle(bitmap.GetHicon());
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static Forms.ContextMenuStrip CreateTrayMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Show player", null, (_, _) => ShowPlayer());
        menu.Items.Add("Settings", null, (_, _) => ShowSettings());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => Current.Shutdown());
        return menu;
    }

    private static void ShowSettings()
    {
        if (Current is not App app)
            return;

        var owner = Current.MainWindow;
        var settings = new SettingsWindow(app.Settings) { Owner = owner };
        settings.ShowDialog();
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
        Settings.SettingsChanged -= OnSettingsChanged;
        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        _trayIconImage?.Dispose();

        base.OnExit(e);
    }
}