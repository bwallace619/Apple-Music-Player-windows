using AppleMusicPlayer.Models;

namespace AppleMusicPlayer.Services;

public interface IMediaSessionService : IAsyncDisposable
{
    event EventHandler<TrackInfo>? TrackChanged;
    TrackInfo CurrentTrack { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task PlayPauseAsync(CancellationToken cancellationToken = default);
    Task NextAsync(CancellationToken cancellationToken = default);
    Task PreviousAsync(CancellationToken cancellationToken = default);
    Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default);
    Task ToggleShuffleAsync(CancellationToken cancellationToken = default);
    Task ToggleRepeatAsync(CancellationToken cancellationToken = default);
}