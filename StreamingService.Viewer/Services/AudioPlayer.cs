using Microsoft.VisualBasic;
using NAudio.Wave;

namespace StreamingService.Viewer.Services
{
    public class AudioPlayer : IDisposable
    {
        private readonly WaveOutEvent _waveOut;
        private readonly BufferedWaveProvider _waveProvider;
        private bool _disposed;

        public AudioPlayer(int sampleRate = 44100, int channels = 1)
        {
            _waveOut = new WaveOutEvent();
            _waveProvider = new BufferedWaveProvider(new WaveFormat(sampleRate, channels));
            _waveOut.Init(_waveProvider);
            _waveOut.Play();
        }

        public void AddSamples(byte[] buffer, int offset, int count)
        {
            if (_disposed) return;
            _waveProvider.AddSamples(buffer, offset, count);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _waveOut.Stop();
            _waveOut.Dispose();
        }
    }
}