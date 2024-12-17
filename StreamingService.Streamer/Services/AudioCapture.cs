using NAudio.Wave;
using StreamingService.Core.Interfaces;

namespace StreamingService.Streamer.Services
{
    public class AudioCapture : IAudioCapture
    {
        private WaveInEvent? _waveSource;
        private bool _isCapturing;

        public event EventHandler<byte[]>? AudioDataAvailable;

        public void StartCapture()
        {
            if (_isCapturing) return;

            _waveSource = new WaveInEvent
            {
                WaveFormat = new WaveFormat(44100, 1),
                BufferMilliseconds = 50
            };

            _waveSource.DataAvailable += WaveSource_DataAvailable;

            try
            {
                _waveSource.StartRecording();
                _isCapturing = true;
            }
            catch (Exception)
            {
                _waveSource.Dispose();
                _waveSource = null;
                throw;
            }
        }

        public void StopCapture()
        {
            if (!_isCapturing) return;

            _isCapturing = false;

            if (_waveSource != null)
            {
                _waveSource.StopRecording();
                _waveSource.DataAvailable -= WaveSource_DataAvailable;
                _waveSource.Dispose();
                _waveSource = null;
            }
        }

        private void WaveSource_DataAvailable(object? sender, WaveInEventArgs e)
        {
            AudioDataAvailable?.Invoke(this, e.Buffer);
        }
    }
}