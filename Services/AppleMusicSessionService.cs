using System.IO;
using AppleMusicPlayer.Models;
using Windows.Media;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace AppleMusicPlayer.Services;

public sealed class AppleMusicSessionService : IMediaSessionService
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private CancellationTokenSource? _lifetime;
    private bool _isSelectingSession;
    private string? _artworkKey;
    private byte[]? _artworkCache;
    private string? _loggedKey;

    public event EventHandler<TrackInfo>? TrackChanged;
    public TrackInfo CurrentTrack { get; private set; } = TrackInfo.Empty;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        _manager.SessionsChanged += OnSessionsChanged;
        await SelectSessionAsync();
        _ = PollAsync(_lifetime.Token);
    }

    public async Task PlayPauseAsync(CancellationToken cancellationToken = default)
    {
        if (_session is not null)
            await _session.TryTogglePlayPauseAsync().AsTask(cancellationToken);
    }

    public async Task NextAsync(CancellationToken cancellationToken = default)
    {
        if (_session is not null)
            await _session.TrySkipNextAsync().AsTask(cancellationToken);
    }

    public async Task PreviousAsync(CancellationToken cancellationToken = default)
    {
        if (_session is not null)
            await _session.TrySkipPreviousAsync().AsTask(cancellationToken);
    }

    public async Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
    {
        if (_session is not null)
            await _session.TryChangePlaybackPositionAsync(position.Ticks).AsTask(cancellationToken);
    }

    public async Task ToggleShuffleAsync(CancellationToken cancellationToken = default)
    {
        if (_session is not null)
            await _session.TryChangeShuffleActiveAsync(!CurrentTrack.IsShuffleEnabled).AsTask(cancellationToken);
    }

    public async Task ToggleRepeatAsync(CancellationToken cancellationToken = default)
    {
        if (_session is not null)
        {
            var nextMode = CurrentTrack.RepeatMode switch
            {
                "None" => MediaPlaybackAutoRepeatMode.List,
                "List" => MediaPlaybackAutoRepeatMode.Track,
                _ => MediaPlaybackAutoRepeatMode.None
            };
            await _session.TryChangeAutoRepeatModeAsync(nextMode).AsTask(cancellationToken);
        }
    }

    private async Task SelectSessionAsync()
    {
        if (_manager is null || _isSelectingSession)
            return;

        _isSelectingSession = true;
        try
        {
            var sessions = _manager.GetSessions();
            var selected = sessions.FirstOrDefault(IsAppleMusicSession)
                           ?? sessions.FirstOrDefault(IsPlayingSession)
                           ?? sessions.FirstOrDefault();

            if (!ReferenceEquals(_session, selected))
            {
                if (_session is not null)
                {
                    _session.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                    _session.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                }

                _session = selected;
                if (_session is not null)
                {
                    _session.MediaPropertiesChanged += OnMediaPropertiesChanged;
                    _session.PlaybackInfoChanged += OnPlaybackInfoChanged;
                }
            }

            if (_session is not null)
                await RefreshAsync();
            else
                Publish(TrackInfo.Empty);
        }
        finally
        {
            _isSelectingSession = false;
        }
    }

    private static bool IsPlayingSession(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            return session.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool IsAppleMusicSession(GlobalSystemMediaTransportControlsSession session)
    {
        var source = session.SourceAppUserModelId;
        return source.Contains("AppleMusic", StringComparison.OrdinalIgnoreCase)
            || source.Contains("Apple", StringComparison.OrdinalIgnoreCase)
            || source.Contains("Music", StringComparison.OrdinalIgnoreCase)
            || source.Contains("iTunes", StringComparison.OrdinalIgnoreCase);
    }

    private async void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
    {
        await SelectSessionAsync();
    }

    private async void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        await RefreshAsync();
    }

    private async void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        await RefreshAsync();
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                await SelectSessionAsync();
            }
            catch (Exception)
            {
                // Session enumeration can fail while Windows is replacing a media session.
            }
        }
    }

    private async Task RefreshAsync()
    {
        if (_session is null)
            return;

        try
        {
            var properties = await _session.TryGetMediaPropertiesAsync();
            var timeline = _session.GetTimelineProperties();
            var playback = _session.GetPlaybackInfo();

            var title = properties.Title ?? "Unknown title";
            var artist = properties.Artist ?? "Unknown artist";
            var album = properties.AlbumTitle ?? string.Empty;
            var key = $"{title}|{artist}|{album}";

            // Artwork is expensive to stream from the session, so only re-read it when the
            // track changes (or when it arrived late and the cache is still empty).
            if (key != _artworkKey || _artworkCache is null)
            {
                _artworkCache = await ReadArtworkAsync(properties.Thumbnail);
                _artworkKey = key;
            }

            if (key != _loggedKey)
            {
                LogMediaState(title, _session.SourceAppUserModelId, _artworkCache?.Length ?? 0);
                _loggedKey = key;
            }

            var repeatMode = playback.AutoRepeatMode switch
            {
                MediaPlaybackAutoRepeatMode.Track => "Track",
                MediaPlaybackAutoRepeatMode.List => "List",
                _ => "None"
            };

            Publish(new TrackInfo(
                title,
                artist,
                album,
                timeline.EndTime - timeline.StartTime,
                timeline.Position - timeline.StartTime,
                playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                playback.IsShuffleActive ?? false,
                repeatMode,
                _artworkCache));
        }
        catch (Exception)
        {
            // A session may disappear between manager enumeration and property access.
        }
    }

    private static async Task<byte[]?> ReadArtworkAsync(IRandomAccessStreamReference? reference)
    {
        if (reference is null)
            return null;

        try
        {
            using var stream = await reference.OpenReadAsync();
            using var input = stream.AsStreamForRead();
            using var memory = new MemoryStream();
            await input.CopyToAsync(memory);
            return memory.ToArray();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void LogMediaState(string? title, string source, int artworkLength)
    {
        try
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AppleMusicPlayer");
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "media.log"), $"{DateTimeOffset.Now:u} source={source} title={title} artworkBytes={artworkLength}\r\n");
        }
        catch (Exception)
        {
        }
    }

    private void Publish(TrackInfo track)
    {
        // Records compare fields (the cached artwork array by reference), so identical
        // polls — e.g. while paused — are dropped instead of waking the UI twice a second.
        if (track == CurrentTrack)
            return;

        CurrentTrack = track;
        TrackChanged?.Invoke(this, track);
    }

    public async ValueTask DisposeAsync()
    {
        if (_manager is not null)
            _manager.SessionsChanged -= OnSessionsChanged;
        if (_session is not null)
        {
            _session.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _session.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        }

        _lifetime?.Cancel();
        _lifetime?.Dispose();
        await Task.CompletedTask;
    }
}