using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AppleMusicPlayer.Models;
using AppleMusicPlayer.Services;
using Forms = System.Windows.Forms;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfCheckBox = System.Windows.Controls.CheckBox;

namespace AppleMusicPlayer.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private bool _initializing = true;

    public SettingsWindow(SettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        RefreshFromSettings();
        _initializing = false;
    }

    private AppSettings Settings => _settingsService.Current;

    private void RefreshFromSettings()
    {
        var settings = Settings;
        SetCombo(BackgroundMode, settings.BackgroundMode);
        SetCombo(WaveformColourMode, settings.WaveformColourMode);
        SetCombo(AnimationSpeed, settings.AnimationSpeed);
        SetCombo(PresetSelector, "Custom");
        StaticColor.Text = settings.StaticColor;
        BackgroundOpacity.Value = settings.BackgroundOpacity;
        ShadowStrength.Value = settings.ShadowStrength;
        CompactWidth.Value = settings.CompactWidth;
        ExpandedWidth.Value = settings.ExpandedWidth;
        CompactCornerRadius.Value = settings.CompactCornerRadius;
        ShowCompactArtwork.IsChecked = settings.ShowCompactArtwork;
        ShowExpandedArtwork.IsChecked = settings.ShowExpandedArtwork;
        UseAlbumArtworkColours.IsChecked = settings.UseAlbumArtworkColours;
        ArtworkSize.Value = settings.ArtworkSize;
        ArtworkCornerRadius.Value = settings.ArtworkCornerRadius;
        EnableWaveform.IsChecked = settings.EnableWaveform;
        WaveformSensitivity.Value = settings.WaveformSensitivity;
        WaveformSmoothing.Value = settings.WaveformSmoothing;
        WaveformHeight.Value = settings.WaveformHeight;
        WaveformBars.Value = settings.WaveformBars;
        LaunchAtStartup.IsChecked = settings.LaunchAtStartup;
        AlwaysOnTop.IsChecked = settings.AlwaysOnTop;
        AutoHideWhenStopped.IsChecked = settings.AutoHideWhenStopped;
        HoverToExpand.IsChecked = settings.HoverToExpand;
        HoverDelayMs.Value = settings.HoverDelayMs;
        CollapseDelayMs.Value = settings.CollapseDelayMs;
        KeepExpanded.IsChecked = settings.KeepExpanded;
        EnableGestures.IsChecked = settings.EnableGestures;
        ReverseSwipeDirection.IsChecked = settings.ReverseSwipeDirection;
        EnableAnimations.IsChecked = settings.EnableAnimations;
        ReduceMotion.IsChecked = settings.ReduceMotion;
        EnableTrayIcon.IsChecked = settings.EnableTrayIcon;
        StartMinimised.IsChecked = settings.StartMinimised;
        ShowProgressBar.IsChecked = settings.ShowProgressBar;
        ShowShuffleRepeat.IsChecked = settings.ShowShuffleRepeat;
        ShowPreviousNext.IsChecked = settings.ShowPreviousNext;
        ShowCompactTrackText.IsChecked = settings.ShowCompactTrackText;
        ShowExpandedAlbum.IsChecked = settings.ShowExpandedAlbum;
        AllowProgressSeek.IsChecked = settings.AllowProgressSeek;
        SpringStrength.Value = settings.SpringStrength;
        AnimateArtwork.IsChecked = settings.AnimateArtwork;
        AnimateWaveformFade.IsChecked = settings.AnimateWaveformFade;
        UseGlass.IsChecked = settings.UseGlass;
        ShowBorder.IsChecked = settings.ShowBorder;
    }

    private void OnBooleanChanged(object sender, RoutedEventArgs e)
    {
        if (_initializing || sender is not WpfCheckBox checkBox)
            return;

        var value = checkBox.IsChecked == true;
        _settingsService.Update(settings =>
        {
            switch (checkBox.Name)
            {
                case nameof(UseGlass): settings.UseGlass = value; break;
                case nameof(ShowBorder): settings.ShowBorder = value; break;
                case nameof(ShowCompactArtwork): settings.ShowCompactArtwork = value; break;
                case nameof(ShowExpandedArtwork): settings.ShowExpandedArtwork = value; break;
                case nameof(UseAlbumArtworkColours): settings.UseAlbumArtworkColours = value; break;
                case nameof(EnableWaveform): settings.EnableWaveform = value; break;
                case nameof(LaunchAtStartup): settings.LaunchAtStartup = value; break;
                case nameof(AlwaysOnTop): settings.AlwaysOnTop = value; break;
                case nameof(AutoHideWhenStopped): settings.AutoHideWhenStopped = value; break;
                case nameof(HoverToExpand): settings.HoverToExpand = value; break;
                case nameof(KeepExpanded): settings.KeepExpanded = value; break;
                case nameof(EnableGestures): settings.EnableGestures = value; break;
                case nameof(ReverseSwipeDirection): settings.ReverseSwipeDirection = value; break;
                case nameof(EnableAnimations): settings.EnableAnimations = value; break;
                case nameof(ReduceMotion): settings.ReduceMotion = value; break;
                case nameof(EnableTrayIcon): settings.EnableTrayIcon = value; break;
                case nameof(StartMinimised): settings.StartMinimised = value; break;
                case nameof(ShowProgressBar): settings.ShowProgressBar = value; break;
                case nameof(ShowShuffleRepeat): settings.ShowShuffleRepeat = value; break;
                case nameof(ShowPreviousNext): settings.ShowPreviousNext = value; break;
                case nameof(ShowCompactTrackText): settings.ShowCompactTrackText = value; break;
                case nameof(ShowExpandedAlbum): settings.ShowExpandedAlbum = value; break;
                case nameof(AllowProgressSeek): settings.AllowProgressSeek = value; break;
                case nameof(AnimateArtwork): settings.AnimateArtwork = value; break;
                case nameof(AnimateWaveformFade): settings.AnimateWaveformFade = value; break;
            }
        });
    }

    private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_initializing || sender is not Slider slider)
            return;

        _settingsService.Update(settings =>
        {
            switch (slider.Name)
            {
                case nameof(BackgroundOpacity): settings.BackgroundOpacity = slider.Value; break;
                case nameof(ShadowStrength): settings.ShadowStrength = slider.Value; break;
                case nameof(CompactWidth): settings.CompactWidth = slider.Value; break;
                case nameof(ExpandedWidth): settings.ExpandedWidth = slider.Value; break;
                case nameof(CompactCornerRadius): settings.CompactCornerRadius = slider.Value; break;
                case nameof(ArtworkSize): settings.ArtworkSize = slider.Value; break;
                case nameof(ArtworkCornerRadius): settings.ArtworkCornerRadius = slider.Value; break;
                case nameof(WaveformSensitivity): settings.WaveformSensitivity = slider.Value; break;
                case nameof(WaveformSmoothing): settings.WaveformSmoothing = slider.Value; break;
                case nameof(WaveformHeight): settings.WaveformHeight = slider.Value; break;
                case nameof(WaveformBars): settings.WaveformBars = (int)slider.Value; break;
                case nameof(HoverDelayMs): settings.HoverDelayMs = slider.Value; break;
                case nameof(CollapseDelayMs): settings.CollapseDelayMs = slider.Value; break;
                case nameof(SpringStrength): settings.SpringStrength = slider.Value; break;
            }
        });
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || sender is not WpfComboBox combo || combo.SelectedItem is not ComboBoxItem item)
            return;

        var value = item.Content?.ToString() ?? string.Empty;
        _settingsService.Update(settings =>
        {
            switch (combo.Name)
            {
                case nameof(BackgroundMode): settings.BackgroundMode = value; break;
                case nameof(WaveformColourMode): settings.WaveformColourMode = value; break;
                case nameof(AnimationSpeed): settings.AnimationSpeed = value; break;
            }
        });
    }

    private void OnStaticColorChanged(object sender, TextChangedEventArgs e)
    {
        if (_initializing || !TryParseColour(StaticColor.Text, out _))
            return;
        _settingsService.Update(settings => settings.StaticColor = StaticColor.Text);
    }

    private void OnChooseColour(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.ColorDialog { FullOpen = true };
        if (!TryParseColour(StaticColor.Text, out var current))
            current = System.Drawing.Color.FromArgb(255, 9, 10, 12);
        dialog.Color = current;
        if (dialog.ShowDialog() == Forms.DialogResult.OK)
            StaticColor.Text = $"#{dialog.Color.A:X2}{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
    }

    private void OnPresetChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || PresetSelector.SelectedItem is not ComboBoxItem item)
            return;
        var preset = item.Content?.ToString();
        if (string.IsNullOrWhiteSpace(preset) || preset == "Custom")
            return;
        ApplyPreset(preset);
        RefreshFromSettings();
    }

    private void OnCalmPresetClicked(object sender, RoutedEventArgs e)
    {
        ApplyPreset("Calm");
        RefreshFromSettings();
    }

    private void ApplyPreset(string preset)
    {
        _settingsService.Update(settings =>
        {
            switch (preset)
            {
                case "Minimal":
                    settings.BackgroundMode = "Solid";
                    settings.UseGlass = false;
                    settings.ShowCompactArtwork = true;
                    settings.WaveformSensitivity = 0.35;
                    settings.WaveformHeight = 0.5;
                    settings.ShadowStrength = 0;
                    break;
                case "Album":
                    settings.BackgroundMode = "Album colour";
                    settings.UseAlbumArtworkColours = true;
                    settings.WaveformColourMode = "Album artwork colour";
                    settings.ArtworkSize = 58;
                    break;
                case "Glass":
                    settings.BackgroundMode = "Acrylic / glass";
                    settings.UseGlass = true;
                    settings.ShadowStrength = 0.2;
                    settings.WaveformColourMode = "White";
                    break;
                case "Calm":
                    settings.WaveformSensitivity = 0.3;
                    settings.WaveformSmoothing = 0.92;
                    settings.WaveformHeight = 0.55;
                    settings.AnimationSpeed = "Slow";
                    settings.SpringStrength = 0.45;
                    break;
                case "Dynamic":
                    settings.BackgroundMode = "Album gradient";
                    settings.WaveformSensitivity = 0.85;
                    settings.WaveformSmoothing = 0.55;
                    settings.WaveformHeight = 0.85;
                    settings.WaveformColourMode = "Album artwork colour";
                    settings.AnimateArtwork = true;
                    break;
            }
        });
    }

    private void OnRestoreDefaultsClicked(object sender, RoutedEventArgs e)
    {
        _settingsService.RestoreDefaults();
        RefreshFromSettings();
    }

    private static void SetCombo(WpfComboBox combo, string value)
    {
        combo.SelectedItem = combo.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
            ?? combo.Items[0];
    }

    private static bool TryParseColour(string text, out System.Drawing.Color colour)
    {
        try
        {
            var mediaColour = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(text)!;
            colour = System.Drawing.Color.FromArgb(mediaColour.A, mediaColour.R, mediaColour.G, mediaColour.B);
            return true;
        }
        catch
        {
            colour = default;
            return false;
        }
    }
}
