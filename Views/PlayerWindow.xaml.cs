using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Threading;
using AppleMusicPlayer.Helpers;
using AppleMusicPlayer.Models;
using AppleMusicPlayer.Services;
using WpfPoint = System.Windows.Point;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;

namespace AppleMusicPlayer.Views;

public partial class PlayerWindow : Window
{
    private const double CompactWidth = 264;
    private const double CompactHeight = 52;
    private const double DefaultExpandedHeight = 232;
    private bool _isExpanded;
    private bool _isSeeking;
    private double _compactWidth = CompactWidth;
    private DateTime _lastSpectrumUpdate = DateTime.MinValue;
    private readonly IMediaSessionService _mediaSession = new AppleMusicSessionService();
    private readonly AudioCaptureService _audioCapture = new();
    private readonly AlbumColourService _albumColourService = new();
    private readonly List<Border> _spectrumBars = new();
    private readonly SettingsService _settingsService = ((App)System.Windows.Application.Current).Settings;
    private readonly TranslateTransform _dragTransform = new();
    private readonly TranslateTransform _compactContentTransform = new();
    private readonly TranslateTransform _expandedContentTransform = new(0, 8);
    private WpfPoint _dragStart;
    private bool _isDragging;
    private AlbumPalette _albumPalette = AlbumPalette.Default;
    private bool _hasPlayedMediaSession;
    private string _currentTrackKey = string.Empty;
    private int _currentArtworkBytes = -1;
    private TimeSpan _lastKnownPosition;
    private DateTime _positionTimestamp = DateTime.UtcNow;
    private bool _isPlaying;
    private readonly DispatcherTimer _progressTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };

    public PlayerWindow()
    {
        InitializeComponent();
        Island.RenderTransform = _dragTransform;
        CompactContent.RenderTransform = _compactContentTransform;
        ExpandedContent.RenderTransform = _expandedContentTransform;
        _progressTimer.Tick += OnProgressTick;
        BuildSpectrumBars();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        WindowPositionHelper.PositionAtPrimaryScreenTop(this);
        _compactWidth = Math.Clamp(_settingsService.Current.CompactWidth, CompactWidth, 320);
        Width = _compactWidth;
        try
        {
            ApplySettings(_settingsService.Current);
        }
        catch (Exception)
        {
            // Visual preferences must not prevent media-session startup.
        }
        _settingsService.SettingsChanged += OnSettingsChanged;
        _mediaSession.TrackChanged += OnTrackChanged;
        _audioCapture.SpectrumChanged += OnSpectrumChanged;

        try
        {
            await _mediaSession.StartAsync();
            _progressTimer.Start();
            if (_settingsService.Current.KeepExpanded)
                Expand();
            if (_settingsService.Current.StartMinimised)
                Hide();
        }
        catch (Exception)
        {
            ApplyTrack(TrackInfo.Empty);
        }
    }

    private void OnTrackChanged(object? sender, TrackInfo track)
    {
        if (track.IsPlaying)
            _hasPlayedMediaSession = true;
        Dispatcher.InvokeAsync(() => ApplyTrack(track));
    }

    private void ApplyTrack(TrackInfo track)
    {
        var settings = _settingsService.Current;
        var trackChanged = track.TrackKey != _currentTrackKey;
        var artworkChanged = (track.Artwork?.Length ?? 0) != _currentArtworkBytes;

        CompactTitle.Text = track.Title;
        CompactArtist.Text = track.Artist;
        ExpandedTitle.Text = track.Title;
        ExpandedArtist.Text = !settings.ShowExpandedAlbum || string.IsNullOrWhiteSpace(track.Album)
            ? track.Artist
            : $"{track.Artist}  •  {track.Album}";
        PlayPauseButton.Content = track.IsPlaying ? "Ⅱ" : "▶";
        RepeatButton.Content = track.RepeatMode == "Track" ? "↻¹" : "↻";
        SetToggleHighlight(ShuffleButton, track.IsShuffleEnabled);
        SetToggleHighlight(RepeatButton, track.RepeatMode != "None");

        if (settings.AutoHideWhenStopped && _hasPlayedMediaSession && !track.IsPlaying)
            Hide();
        else if (track.IsPlaying && !IsVisible)
            Show();

        ProgressSlider.Maximum = Math.Max(1, track.Duration.TotalSeconds);
        _lastKnownPosition = track.Position;
        _positionTimestamp = DateTime.UtcNow;
        _isPlaying = track.IsPlaying;
        if (!_isSeeking)
        {
            ProgressSlider.Value = Math.Clamp(track.Position.TotalSeconds, 0, ProgressSlider.Maximum);
            ElapsedTime.Text = FormatTime(track.Position);
        }
        TotalTime.Text = FormatTime(track.Duration);

        if (settings.EnableWaveform && settings.AnimateWaveformFade)
            AnimateOpacity(CompactWaveform, track.IsPlaying ? 1 : 0.3, AnimationDuration(400));

        // Artwork decoding and palette extraction are expensive; only redo them
        // when the track (or its late-arriving artwork) actually changes.
        if (trackChanged || artworkChanged)
        {
            _currentTrackKey = track.TrackKey;
            _currentArtworkBytes = track.Artwork?.Length ?? 0;
            var image = CreateArtwork(track.Artwork);
            _albumPalette = _albumColourService.Extract(track.Artwork);
            CompactArtwork.Source = image;
            ExpandedArtwork.Source = image;
            CompactArtworkFallback.Visibility = image is null ? Visibility.Visible : Visibility.Collapsed;
            ExpandedArtworkFallback.Visibility = image is null ? Visibility.Visible : Visibility.Collapsed;
            if (image is not null && trackChanged && settings.AnimateArtwork && settings.EnableAnimations && !settings.ReduceMotion)
            {
                var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(AnimationDuration(320)));
                CompactArtwork.BeginAnimation(OpacityProperty, fade);
                ExpandedArtwork.BeginAnimation(OpacityProperty, fade.Clone());
            }
            ApplyBackground(settings);
            UpdateSpectrumColour(settings);
        }
    }

    private void SetToggleHighlight(System.Windows.Controls.Button button, bool active)
    {
        if (active)
            button.Foreground = (Brush)FindResource("Accent");
        else
            button.ClearValue(ForegroundProperty);
    }

    private void OnProgressTick(object? sender, EventArgs e)
    {
        if (!_isPlaying || _isSeeking || !_isExpanded)
            return;

        var estimated = _lastKnownPosition + (DateTime.UtcNow - _positionTimestamp);
        var seconds = Math.Clamp(estimated.TotalSeconds, 0, ProgressSlider.Maximum);
        ProgressSlider.Value = seconds;
        ElapsedTime.Text = FormatTime(TimeSpan.FromSeconds(seconds));
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time < TimeSpan.Zero)
            time = TimeSpan.Zero;
        return time.TotalHours >= 1 ? time.ToString(@"h\:mm\:ss") : time.ToString(@"m\:ss");
    }

    private void OnSettingsChanged(object? sender, AppSettings settings)
    {
        Dispatcher.InvokeAsync(() => ApplySettings(settings));
    }

    private void ApplySettings(AppSettings settings)
    {
        _compactWidth = Math.Clamp(settings.CompactWidth, CompactWidth, 320);
        Topmost = settings.AlwaysOnTop;
        Island.CornerRadius = new CornerRadius(settings.CompactCornerRadius);
        ShadowLayer.CornerRadius = new CornerRadius(settings.CompactCornerRadius);
        CompactArtworkFrame.CornerRadius = new CornerRadius(settings.ArtworkCornerRadius);
        ExpandedArtworkFrame.CornerRadius = new CornerRadius(settings.ArtworkCornerRadius);
        CompactArtworkFrame.Visibility = settings.ShowCompactArtwork ? Visibility.Visible : Visibility.Collapsed;
        ExpandedArtworkFrame.Visibility = settings.ShowExpandedArtwork ? Visibility.Visible : Visibility.Collapsed;
        CompactWaveform.Visibility = settings.EnableWaveform ? Visibility.Visible : Visibility.Collapsed;
        if (settings.EnableWaveform)
            _audioCapture.Start();
        else
            _audioCapture.Stop();
        if (!settings.AnimateWaveformFade)
        {
            CompactWaveform.BeginAnimation(OpacityProperty, null);
            CompactWaveform.Opacity = 1;
        }
        ProgressSlider.Visibility = settings.ShowProgressBar ? Visibility.Visible : Visibility.Collapsed;
        TimeLabels.Visibility = settings.ShowProgressBar ? Visibility.Visible : Visibility.Collapsed;
        ProgressSlider.IsHitTestVisible = settings.AllowProgressSeek;
        ShuffleButton.Visibility = settings.ShowShuffleRepeat ? Visibility.Visible : Visibility.Collapsed;
        RepeatButton.Visibility = settings.ShowShuffleRepeat ? Visibility.Visible : Visibility.Collapsed;
        PreviousButton.Visibility = settings.ShowPreviousNext ? Visibility.Visible : Visibility.Collapsed;
        NextButton.Visibility = settings.ShowPreviousNext ? Visibility.Visible : Visibility.Collapsed;
        CompactTitle.Visibility = settings.ShowCompactTrackText ? Visibility.Visible : Visibility.Collapsed;
        CompactArtist.Visibility = settings.ShowCompactTrackText ? Visibility.Visible : Visibility.Collapsed;
        CompactArtworkFrame.Width = settings.ShowCompactArtwork ? 25 : 0;
        CompactArtworkFrame.Height = settings.ShowCompactArtwork ? 25 : 0;
        ExpandedArtworkFrame.Width = settings.ArtworkSize;
        ExpandedArtworkFrame.Height = settings.ArtworkSize;
        CompactWaveform.Width = Math.Max(52, settings.CompactWidth - 188);
        MaxWidth = Math.Max(ExpandedWidthLimit, settings.ExpandedWidth);
        MaxHeight = Math.Max(DefaultExpandedHeight, settings.ArtworkSize + 164);
        ApplyBackground(settings);
        GlassHelper.TryApply(this, settings.UseGlass || settings.BackgroundMode == "Acrylic / glass");
        _audioCapture.Configure(settings.WaveformSensitivity, settings.WaveformSmoothing);
        if (_spectrumBars.Count != settings.WaveformBars)
            BuildSpectrumBars();
        UpdateSpectrumColour(settings);

        var expandedHeight = GetExpandedHeight(settings);
        if (_isExpanded)
            AnimateSize(Math.Clamp(settings.ExpandedWidth, 320, ExpandedWidthLimit), expandedHeight, AnimationDuration(260));
        else if (Math.Abs(Width - _compactWidth) > 0.5)
            AnimateSize(_compactWidth, CompactHeight, AnimationDuration(180));
    }

    private const double ExpandedWidthLimit = 520;

    private static double GetExpandedHeight(AppSettings settings)
    {
        return Math.Clamp(Math.Max(DefaultExpandedHeight, settings.ArtworkSize + 164), DefaultExpandedHeight, 320);
    }

    private void ApplyBackground(AppSettings settings)
    {
        var opacity = (byte)Math.Clamp(settings.BackgroundOpacity * 255, 140, 255);
        var primary = ParseColour(settings.StaticColor, Color.FromRgb(9, 10, 12));
        Brush background;
        if (settings.UseGlass || settings.BackgroundMode == "Acrylic / glass")
        {
            background = new SolidColorBrush(Color.FromArgb((byte)Math.Min((int)opacity, 226), 28, 30, 36));
        }
        else if (settings.UseAlbumArtworkColours && settings.BackgroundMode == "Album colour")
        {
            background = new SolidColorBrush(Color.FromArgb(opacity, _albumPalette.Primary.R, _albumPalette.Primary.G, _albumPalette.Primary.B));
        }
        else if (settings.UseAlbumArtworkColours && settings.BackgroundMode == "Album gradient")
        {
            background = new LinearGradientBrush(
                Color.FromArgb(opacity, _albumPalette.Primary.R, _albumPalette.Primary.G, _albumPalette.Primary.B),
                Color.FromArgb(opacity, _albumPalette.Secondary.R, _albumPalette.Secondary.G, _albumPalette.Secondary.B), 0);
        }
        else
        {
            background = new SolidColorBrush(Color.FromArgb(opacity, primary.R, primary.G, primary.B));
        }

        Island.Background = background;
        Island.BorderThickness = settings.ShowBorder ? new Thickness(1) : new Thickness(0);
        ShadowLayer.Background = new SolidColorBrush(Color.FromArgb((byte)(settings.ShadowStrength * 160), 0, 0, 0));
    }

    private void UpdateSpectrumColour(AppSettings settings)
    {
        var colour = settings.WaveformColourMode switch
        {
            "White" => Colors.White,
            "Album artwork colour" => _albumPalette.Accent,
            _ => GetAccentColour()
        };
        foreach (var bar in _spectrumBars)
            bar.Background = new SolidColorBrush(colour);
    }

    private static Color ParseColour(string value, Color fallback)
    {
        try
        {
            return (Color)System.Windows.Media.ColorConverter.ConvertFromString(value)!;
        }
        catch
        {
            return fallback;
        }
    }

    private static Color GetAccentColour()
    {
        return System.Windows.Application.Current.Resources["Accent"] switch
        {
            SolidColorBrush brush => brush.Color,
            Color colour => colour,
            _ => Color.FromRgb(255, 55, 95)
        };
    }

    private void BuildSpectrumBars()
    {
        CompactWaveform.Children.Clear();
        _spectrumBars.Clear();
        var count = Math.Clamp(_settingsService.Current.WaveformBars, 10, 30);
        for (var index = 0; index < count; index++)
        {
            var bar = new Border
            {
                Width = 2.5,
                Height = 4,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 55, 95)),
                Opacity = 0.9
            };
            Canvas.SetLeft(bar, index * (76d / count));
            Canvas.SetTop(bar, 12);
            CompactWaveform.Children.Add(bar);
            _spectrumBars.Add(bar);
        }
    }

    private void OnSpectrumChanged(object? sender, float[] spectrum)
    {
        if ((DateTime.UtcNow - _lastSpectrumUpdate).TotalMilliseconds < 33)
            return;

        _lastSpectrumUpdate = DateTime.UtcNow;
        Dispatcher.InvokeAsync(() =>
        {
            for (var index = 0; index < _spectrumBars.Count && index < spectrum.Length; index++)
            {
                var height = Math.Max(3, spectrum[index] * 16 * _settingsService.Current.WaveformHeight);
                _spectrumBars[index].Height = height;
                Canvas.SetTop(_spectrumBars[index], (28 - height) / 2);
            }
        });
    }

    private static BitmapImage? CreateArtwork(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
            return null;

        try
        {
            using var stream = new MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_settingsService.Current.HoverToExpand)
            return;
        await Task.Delay((int)_settingsService.Current.HoverDelayMs);
        if (!IsMouseOver)
            return;
        Expand();
    }

    private void OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!_settingsService.Current.EnableGestures || IsInteractiveSource(e.OriginalSource as DependencyObject))
            return;

        _isDragging = true;
        _dragStart = e.GetPosition(this);
        CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDragging)
            return;

        var delta = e.GetPosition(this).X - _dragStart.X;
        _dragTransform.X = Math.Clamp(delta * 0.55, -110, 110);
    }

    private async void OnMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!_isDragging)
            return;

        _isDragging = false;
        ReleaseMouseCapture();
        var delta = _dragTransform.X;
        if (_settingsService.Current.ReverseSwipeDirection)
            delta *= -1;

        if (Math.Abs(delta) >= 55)
        {
            if (delta < 0)
                await _mediaSession.NextAsync();
            else
                await _mediaSession.PreviousAsync();
        }

        var reset = new DoubleAnimation(0, TimeSpan.FromMilliseconds(AnimationDuration(220)))
        {
            EasingFunction = CreateEasing()
        };
        _dragTransform.BeginAnimation(TranslateTransform.XProperty, reset);
        e.Handled = true;
    }

    private static bool IsInteractiveSource(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is System.Windows.Controls.Button or Slider)
                return true;
            source = VisualTreeHelper.GetParent(source);
        }
        return false;
    }

    private async void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_settingsService.Current.KeepExpanded)
            return;
        await Task.Delay((int)_settingsService.Current.CollapseDelayMs);
        if (!IsMouseOver && !_settingsService.Current.KeepExpanded)
            Collapse();
    }

    private async void OnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton != System.Windows.Input.MouseButton.Middle)
            return;
        e.Handled = true;
        await _mediaSession.PlayPauseAsync();
    }

    private void Expand()
    {
        if (_isExpanded)
            return;

        _isExpanded = true;
        ExpandedContent.Visibility = Visibility.Visible;
        AnimateSize(
            Math.Clamp(_settingsService.Current.ExpandedWidth, 320, ExpandedWidthLimit),
            GetExpandedHeight(_settingsService.Current),
            AnimationDuration(260));
        AnimateContent(CompactContent, _compactContentTransform, 0, 4, AnimationDuration(120));
        AnimateContent(ExpandedContent, _expandedContentTransform, 1, 0, AnimationDuration(260));
    }

    private void Collapse()
    {
        if (!_isExpanded)
            return;

        _isExpanded = false;
        AnimateSize(_compactWidth, CompactHeight, AnimationDuration(240));
        AnimateContent(ExpandedContent, _expandedContentTransform, 0, 8, AnimationDuration(150), () => ExpandedContent.Visibility = Visibility.Collapsed);
        AnimateContent(CompactContent, _compactContentTransform, 1, 0, AnimationDuration(220));
    }

    private int AnimationDuration(int normalDuration)
    {
        if (_settingsService.Current.ReduceMotion || !_settingsService.Current.EnableAnimations)
            return 1;
        return _settingsService.Current.AnimationSpeed switch
        {
            "Slow" => (int)(normalDuration * 1.35),
            "Fast" => (int)(normalDuration * 0.7),
            _ => normalDuration
        };
    }

    private void AnimateSize(double width, double height, int duration)
    {
        var easing = CreateEasing();
        BeginAnimation(WidthProperty, new DoubleAnimation(Width, width, TimeSpan.FromMilliseconds(duration)) { EasingFunction = easing });
        BeginAnimation(HeightProperty, new DoubleAnimation(Height, height, TimeSpan.FromMilliseconds(duration)) { EasingFunction = easing });
        BeginAnimation(LeftProperty, new DoubleAnimation(Left, WindowPositionHelper.GetPrimaryScreenCenteredLeft(this, width), TimeSpan.FromMilliseconds(duration)) { EasingFunction = easing });
    }

    private static void AnimateOpacity(UIElement element, double opacity, int duration, Action? completed = null)
    {
        var animation = new DoubleAnimation(opacity, TimeSpan.FromMilliseconds(duration));
        if (completed is not null)
            animation.Completed += (_, _) => completed();
        element.BeginAnimation(OpacityProperty, animation);
    }

    private void AnimateContent(UIElement element, TranslateTransform transform, double opacity, double y, int duration, Action? completed = null)
    {
        var easing = CreateEasing();
        var opacityAnimation = new DoubleAnimation(opacity, TimeSpan.FromMilliseconds(duration)) { EasingFunction = easing };
        var yAnimation = new DoubleAnimation(y, TimeSpan.FromMilliseconds(duration)) { EasingFunction = easing };
        if (completed is not null)
            opacityAnimation.Completed += (_, _) => completed();
        element.BeginAnimation(OpacityProperty, opacityAnimation);
        transform.BeginAnimation(TranslateTransform.YProperty, yAnimation);
    }

    private EasingFunctionBase CreateEasing()
    {
        return new ExponentialEase
        {
            EasingMode = EasingMode.EaseOut,
            Exponent = 1.4 + (_settingsService.Current.SpringStrength * 2.6)
        };
    }

    private void OnProgressChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isSeeking)
            return;
    }

    private void OnSeekStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
    {
        _isSeeking = true;
    }

    private async void OnSeekCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
    {
        _isSeeking = false;
        var position = TimeSpan.FromSeconds(ProgressSlider.Value);
        _lastKnownPosition = position;
        _positionTimestamp = DateTime.UtcNow;
        ElapsedTime.Text = FormatTime(position);
        await _mediaSession.SeekAsync(position);
    }

    private async void OnPlayPauseClicked(object sender, RoutedEventArgs e) => await _mediaSession.PlayPauseAsync();
    private async void OnPreviousClicked(object sender, RoutedEventArgs e) => await _mediaSession.PreviousAsync();
    private async void OnNextClicked(object sender, RoutedEventArgs e) => await _mediaSession.NextAsync();
    private async void OnShuffleClicked(object sender, RoutedEventArgs e) => await _mediaSession.ToggleShuffleAsync();
    private async void OnRepeatClicked(object sender, RoutedEventArgs e) => await _mediaSession.ToggleRepeatAsync();

    private void OnSettingsClicked(object sender, RoutedEventArgs e)
    {
        var settings = new SettingsWindow(_settingsService) { Owner = this };
        settings.ShowDialog();
    }

    private async void OnClosed(object? sender, EventArgs e)
    {
        _progressTimer.Stop();
        _mediaSession.TrackChanged -= OnTrackChanged;
        _settingsService.SettingsChanged -= OnSettingsChanged;
        _audioCapture.SpectrumChanged -= OnSpectrumChanged;
        _audioCapture.Dispose();
        await _mediaSession.DisposeAsync();
    }
}