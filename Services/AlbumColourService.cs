using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;

namespace AppleMusicPlayer.Services;

public sealed record AlbumPalette(Color Primary, Color Secondary, Color Accent)
{
    public static AlbumPalette Default { get; } = new(
        Color.FromRgb(12, 13, 17),
        Color.FromRgb(30, 31, 38),
        Color.FromRgb(255, 55, 95));
}

public sealed class AlbumColourService
{
    public AlbumPalette Extract(byte[]? artwork)
    {
        if (artwork is null || artwork.Length == 0)
            return AlbumPalette.Default;

        try
        {
            using var stream = new MemoryStream(artwork);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var source = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Bgra32, null, 0);
            var width = Math.Min(48, source.PixelWidth);
            var height = Math.Min(48, source.PixelHeight);
            var scaled = new TransformedBitmap(source, new ScaleTransform((double)width / source.PixelWidth, (double)height / source.PixelHeight));
            var pixels = new byte[scaled.PixelWidth * scaled.PixelHeight * 4];
            scaled.CopyPixels(pixels, scaled.PixelWidth * 4, 0);

            var colours = new List<(double Saturation, Color Colour)>();
            for (var index = 0; index < pixels.Length; index += 16)
            {
                var blue = pixels[index];
                var green = pixels[index + 1];
                var red = pixels[index + 2];
                var max = Math.Max(red, Math.Max(green, blue));
                var min = Math.Min(red, Math.Min(green, blue));
                colours.Add(((max - min) / 255d, Color.FromRgb(red, green, blue)));
            }

            var ordered = colours.OrderByDescending(item => item.Saturation).Take(2).ToArray();
            var primary = Darken(ordered.ElementAtOrDefault(0).Colour, 0.42);
            var secondary = Darken(ordered.ElementAtOrDefault(1).Colour, 0.58);
            var accent = ordered.ElementAtOrDefault(0).Colour;
            return new AlbumPalette(primary, secondary, accent);
        }
        catch (Exception)
        {
            return AlbumPalette.Default;
        }
    }

    private static Color Darken(Color colour, double amount)
    {
        if (colour == default)
            colour = AlbumPalette.Default.Primary;
        return Color.FromRgb(
            (byte)(colour.R * amount),
            (byte)(colour.G * amount),
            (byte)(colour.B * amount));
    }
}