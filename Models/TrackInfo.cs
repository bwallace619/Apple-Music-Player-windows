namespace AppleMusicPlayer.Models;

public sealed record TrackInfo(
    string Title,
    string Artist,
    string Album,
    TimeSpan Duration,
    TimeSpan Position,
    bool IsPlaying,
    bool IsShuffleEnabled,
    string RepeatMode,
    byte[]? Artwork)
{
    /// <summary>Identity of the track itself, ignoring playback position and state.</summary>
    public string TrackKey => $"{Title}|{Artist}|{Album}";

    public static TrackInfo Empty { get; } = new(
        "Nothing playing",
        "Open Apple Music to begin",
        string.Empty,
        TimeSpan.Zero,
        TimeSpan.Zero,
        false,
        false,
        "None",
        null);
}