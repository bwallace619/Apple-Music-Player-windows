namespace AppleMusicPlayer.Services;

public sealed class SpectrumAnalyzer
{
    private readonly float[] _smoothed;
    private readonly double[] _window;
    private double _sensitivity = 0.65;
    private double _smoothing = 0.75;

    public SpectrumAnalyzer(int bandCount = 30, int fftSize = 1024)
    {
        BandCount = bandCount;
        FftSize = fftSize;
        _smoothed = new float[bandCount];
        _window = Enumerable.Range(0, fftSize)
            .Select(index => 0.5 - (0.5 * Math.Cos(2 * Math.PI * index / (fftSize - 1))))
            .ToArray();
    }

    public int BandCount { get; }
    public int FftSize { get; }

    public void Configure(double sensitivity, double smoothing)
    {
        _sensitivity = Math.Clamp(sensitivity, 0.1, 2);
        _smoothing = Math.Clamp(smoothing, 0.05, 1);
    }

    public float[] Analyze(ReadOnlySpan<float> samples, int sampleRate)
    {
        var real = new double[FftSize];
        var imaginary = new double[FftSize];
        var offset = Math.Max(0, samples.Length - FftSize);
        for (var index = 0; index < FftSize && offset + index < samples.Length; index++)
            real[index] = samples[offset + index] * _window[index];

        FourierTransform(real, imaginary);
        var result = new float[BandCount];
        for (var band = 0; band < BandCount; band++)
        {
            var start = Math.Max(1, (int)Math.Pow(FftSize / 2d, (double)band / BandCount));
            var end = Math.Max(start + 1, (int)Math.Pow(FftSize / 2d, (double)(band + 1) / BandCount));
            var energy = 0d;
            for (var bin = start; bin < Math.Min(end, FftSize / 2); bin++)
                energy += Math.Sqrt((real[bin] * real[bin]) + (imaginary[bin] * imaginary[bin]));

            // Keep small changes quiet while allowing sustained musical energy through.
            var target = (float)Math.Clamp(energy / Math.Max(1, end - start) * 2.2 * _sensitivity, 0, 1);
            var attack = 0.18 * (1 - _smoothing) + 0.04;
            var decay = 0.055 * (1 - (_smoothing * 0.75)) + 0.012;
            var smoothing = (float)(target > _smoothed[band] ? attack : decay);
            _smoothed[band] += (target - _smoothed[band]) * smoothing;
            result[band] = _smoothed[band];
        }

        return result;
    }

    private static void FourierTransform(double[] real, double[] imaginary)
    {
        var length = real.Length;
        for (var i = 1; i < length; i++)
        {
            var j = 0;
            var bit = length >> 1;
            for (; (j & bit) != 0; bit >>= 1)
                j ^= bit;
            j ^= bit;
            if (i < j)
            {
                (real[i], real[j]) = (real[j], real[i]);
                (imaginary[i], imaginary[j]) = (imaginary[j], imaginary[i]);
            }
        }

        for (var span = 2; span <= length; span <<= 1)
        {
            var angle = -2 * Math.PI / span;
            var phaseReal = Math.Cos(angle);
            var phaseImaginary = Math.Sin(angle);
            for (var start = 0; start < length; start += span)
            {
                var currentReal = 1d;
                var currentImaginary = 0d;
                var half = span / 2;
                for (var index = 0; index < half; index++)
                {
                    var even = start + index;
                    var odd = even + half;
                    var oddReal = (real[odd] * currentReal) - (imaginary[odd] * currentImaginary);
                    var oddImaginary = (real[odd] * currentImaginary) + (imaginary[odd] * currentReal);
                    real[odd] = real[even] - oddReal;
                    imaginary[odd] = imaginary[even] - oddImaginary;
                    real[even] += oddReal;
                    imaginary[even] += oddImaginary;
                    (currentReal, currentImaginary) = (
                        (currentReal * phaseReal) - (currentImaginary * phaseImaginary),
                        (currentReal * phaseImaginary) + (currentImaginary * phaseReal));
                }
            }
        }
    }
}