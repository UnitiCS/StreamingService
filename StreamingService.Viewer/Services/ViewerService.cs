using StreamingService.Core.Models;
using NAudio.Wave;
using System.Net.Sockets;
using System.Drawing.Imaging;

namespace StreamingService.Viewer.Services
{
    public class ViewerService : IDisposable
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _receiveTask;
        private readonly WaveOut _waveOut;
        private readonly BufferedWaveProvider _waveProvider;
        private bool _isMuted;
        private float _volume = 0.5f;

        public bool IsConnected => _client?.Connected ?? false;

        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                _isMuted = value;
                if (_waveOut != null)
                {
                    _waveOut.Volume = _isMuted ? 0 : _volume;
                }
            }
        }

        public event EventHandler<Bitmap>? FrameReceived;
        public event EventHandler<string>? StatusChanged;
        public event EventHandler<Exception>? ErrorOccurred;
        public event EventHandler<int>? AudioLevelChanged;

        public ViewerService()
        {
            _waveOut = new WaveOut();
            _waveProvider = new BufferedWaveProvider(new WaveFormat(44100, 1));
            _waveOut.Init(_waveProvider);
            _waveOut.Volume = _volume;
            _waveOut.Play();
        }

        public void SetVolume(float volume)
        {
            _volume = volume;
            if (_waveOut != null && !_isMuted)
            {
                _waveOut.Volume = volume;
            }
        }

        public async Task ConnectAsync(string serverIP, int port)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(serverIP, port);
                _stream = _client.GetStream();

                OnStatusChanged("Connected to stream");

                _cancellationTokenSource = new CancellationTokenSource();
                _receiveTask = ReceiveStreamAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                await DisconnectAsync();
                throw new Exception("Failed to connect to stream", ex);
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _cancellationTokenSource?.Cancel();

                if (_receiveTask != null)
                {
                    try
                    {
                        await _receiveTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancelling
                    }
                }

                _stream?.Dispose();
                _client?.Dispose();
                _stream = null;
                _client = null;

                OnStatusChanged("Disconnected");
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
            }
        }

        private async Task ReceiveStreamAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && _stream != null)
                {
                    var packet = await StreamPacket.ReadFromStreamAsync(_stream, cancellationToken);

                    switch (packet.PacketType)
                    {
                        case 0: // Видео
                            try
                            {
                                using var ms = new MemoryStream(packet.Data);
                                using var bitmap = new Bitmap(ms);
                                OnFrameReceived(new Bitmap(bitmap));
                            }
                            catch (Exception ex)
                            {
                                OnErrorOccurred(new Exception("Error processing video frame", ex));
                            }
                            break;

                        case 1: // Аудио
                            try
                            {
                                if (!_isMuted)
                                {
                                    _waveProvider.AddSamples(packet.Data, 0, packet.Data.Length);
                                }
                                int audioLevel = CalculateAudioLevel(packet.Data);
                                AudioLevelChanged?.Invoke(this, audioLevel);
                            }
                            catch (Exception ex)
                            {
                                OnErrorOccurred(new Exception("Error processing audio data", ex));
                            }
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
            }
        }

        private int CalculateAudioLevel(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0) return 0;

            int sum = 0;
            for (int i = 0; i < buffer.Length; i += 2)
            {
                if (i + 1 < buffer.Length)
                {
                    short sample = (short)((buffer[i + 1] << 8) | buffer[i]);
                    sum += Math.Abs(sample);
                }
            }

            double averageLevel = sum / (double)(buffer.Length / 2);
            int percentLevel = (int)((averageLevel / 32768.0) * 100);
            return Math.Min(100, Math.Max(0, percentLevel));
        }

        private void OnFrameReceived(Bitmap frame)
        {
            FrameReceived?.Invoke(this, frame);
        }

        private void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(this, status);
        }

        private void OnErrorOccurred(Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
        }

        public void Dispose()
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            DisconnectAsync().Wait();
            _cancellationTokenSource?.Dispose();
        }
    }
}