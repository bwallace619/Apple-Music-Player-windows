using System.IO;
using System.Text.Json;
using AppleMusicPlayer.Models;
using Microsoft.Win32;

namespace AppleMusicPlayer.Services;

public sealed class SettingsService
{
    private const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AppleMusicPlayer",
        "settings.json");
    private readonly SemaphoreSlim _saveGate = new(1, 1);
    private CancellationTokenSource? _pendingSave;

    public AppSettings Current { get; private set; } = new();
    public event EventHandler<AppSettings>? SettingsChanged;

    public async Task LoadAsync()
    {
        if (File.Exists(_path))
        {
            try
            {
                await using var stream = File.OpenRead(_path);
                Current = await JsonSerializer.DeserializeAsync<AppSettings>(stream) ?? AppSettings.CreateDefault();
            }
            catch (Exception)
            {
                Current = AppSettings.CreateDefault();
            }
        }
        ApplyStartup(Current.LaunchAtStartup);
    }

    public async Task SaveAsync(AppSettings settings)
    {
        Current = settings;
        SettingsChanged?.Invoke(this, Current);
        await SaveCurrentAsync();
    }

    public void Update(Action<AppSettings> update)
    {
        update(Current);
        SettingsChanged?.Invoke(this, Current);
        ScheduleSave();
    }

    /// <summary>
    /// Coalesces rapid updates (e.g. a slider being dragged) into a single disk write.
    /// </summary>
    private void ScheduleSave()
    {
        _pendingSave?.Cancel();
        var pending = _pendingSave = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(400, pending.Token);
                await SaveCurrentAsync();
            }
            catch (OperationCanceledException)
            {
                // Superseded by a newer update.
            }
        });
    }

    public void RestoreDefaults()
    {
        Current = AppSettings.CreateDefault();
        SettingsChanged?.Invoke(this, Current);
        _ = SaveCurrentAsync();
    }

    private async Task SaveCurrentAsync()
    {
        await _saveGate.WaitAsync();
        try
        {
            var snapshot = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            await File.WriteAllTextAsync(_path, snapshot);
        }
        finally
        {
            _saveGate.Release();
        }
        ApplyStartup(Current.LaunchAtStartup);
    }

    private static void ApplyStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled)
            key?.SetValue("AppleMusicPlayer", Environment.ProcessPath ?? string.Empty);
        else
            key?.DeleteValue("AppleMusicPlayer", false);
    }
}