using NAudio.Wave;

namespace AppleMusicPlayer.Services;

public sealed class AudioCaptureService : IDisposable
{
    private readonly SpectrumAnalyzer _analyzer = new();
    private WasapiLoopbackCapture? _capture;
    private float[] _sampleBuffer = Array.Empty<float>();

    public event EventHandler<float[]>? SpectrumChanged;

    public void Configure(double sensitivity, double smoothing)
    {
        _analyzer.Configure(sensitivity, smoothing);
    }

    public void Start()
    {
        if (_capture is not null)
            return;

        _capture = new WasapiLoopbackCapture();
        _capture.DataAvailable += OnDataAvailable;
        _capture.RecordingStopped += OnRecordingStopped;
        _capture.StartRecording();
    }

    public void Stop()
    {
        if (_capture is null)
            return;

        _capture.StopRecording();
        _capture.Dispose();
        _capture = null;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        try
        {
            var bytesPerSample = _capture?.WaveFormat.BitsPerSample / 8 ?? 4;
            var channels = _capture?.WaveFormat.Channels ?? 2;
            if (bytesPerSample is not (2 or 4) || channels <= 0)
                return;

            var sampleCount = e.BytesRecorded / bytesPerSample / channels;
            if (_sampleBuffer.Length < sampleCount)
                _sampleBuffer = new float[sampleCount];

            for (var sample = 0; sample < sampleCount; sample++)
            {
                var sum = 0d;
                for (var channel = 0; channel < channels; channel++)
                {
                    var offset = (sample * channels + channel) * bytesPerSample;
                    sum += bytesPerSample == 4
                        ? BitConverter.ToSingle(e.Buffer, offset)
                        : (short)BitConverter.ToInt16(e.Buffer, offset) / 32768d;
                }
                _sampleBuffer[sample] = (float)(sum / channels);
            }

            var spectrum = _analyzer.Analyze(_sampleBuffer.AsSpan(0, sampleCount), _capture?.WaveFormat.SampleRate ?? 44100);
            SpectrumChanged?.Invoke(this, spectrum);
        }
        catch (Exception)
        {
            // Audio capture must never be allowed to terminate the player process.
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
            SpectrumChanged?.Invoke(this, new float[_analyzer.BandCount]);
    }

    public void Dispose() => Stop();
}