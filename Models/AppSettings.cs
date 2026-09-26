namespace AppleMusicPlayer.Models;

public sealed class AppSettings
{
    public string BackgroundMode { get; set; } = "Solid";
    public bool UseGlass { get; set; }
    public string StaticColor { get; set; } = "#FF090A0C";
    public double BackgroundOpacity { get; set; } = 0.96;
    public double ShadowStrength { get; set; } = 0.28;
    public bool ShowBorder { get; set; } = true;
    public double ExpandedWidth { get; set; } = 360;
    public bool ShowCompactArtwork { get; set; } = true;
    public bool ShowExpandedArtwork { get; set; } = true;
    public double ArtworkSize { get; set; } = 54;
    public double ArtworkCornerRadius { get; set; } = 10;
    public bool UseAlbumArtworkColours { get; set; } = true;
    public bool LaunchAtStartup { get; set; }
    public bool KeepExpanded { get; set; }
    public bool EnableWaveform { get; set; } = true;
    public bool EnableAnimations { get; set; } = true;
    public bool EnableGestures { get; set; } = true;
    public bool ReverseSwipeDirection { get; set; }
    public double CompactWidth { get; set; } = 264;
    public double CompactCornerRadius { get; set; } = 26;
    public double WaveformSensitivity { get; set; } = 0.65;
    public double WaveformSmoothing { get; set; } = 0.75;
    public double WaveformHeight { get; set; } = 0.7;
    public int WaveformBars { get; set; } = 20;
    public string WaveformColourMode { get; set; } = "Accent";
    public bool AutoHideWhenStopped { get; set; }
    public bool HoverToExpand { get; set; } = true;
    public double HoverDelayMs { get; set; } = 0;
    public double CollapseDelayMs { get; set; } = 180;
    public bool ShowProgressBar { get; set; } = true;
    public bool ShowShuffleRepeat { get; set; } = true;
    public bool ShowPreviousNext { get; set; } = true;
    public bool ShowCompactTrackText { get; set; } = true;
    public bool ShowExpandedAlbum { get; set; } = true;
    public bool AllowProgressSeek { get; set; } = true;
    public string AnimationSpeed { get; set; } = "Normal";
    public double SpringStrength { get; set; } = 0.65;
    public bool AnimateArtwork { get; set; } = true;
    public bool AnimateWaveformFade { get; set; } = true;
    public bool ReduceMotion { get; set; }
    public bool EnableTrayIcon { get; set; } = true;
    public bool AlwaysOnTop { get; set; } = true;
    public bool StartMinimised { get; set; }

    public static AppSettings CreateDefault() => new();
}