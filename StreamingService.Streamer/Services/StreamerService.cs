using StreamingService.Core.Interfaces;
using StreamingService.Core.Models;
using StreamingService.Core.Services;
using NAudio.Wave;
using System.Drawing;
using System.Windows.Forms; // Явно указываем Windows.Forms
using System.Net;
using System.Net.Sockets;
using System.Drawing.Imaging;

namespace StreamingService.Streamer.Services
{
    public class StreamerService : IStreamingService
    {
        private readonly StreamSettings _settings;
        private readonly List<TcpClient> _clients;
        private readonly AdaptiveStreaming _adaptiveStreaming;
        private TcpListener? _server;
        private bool _isStreaming;
        private Task? _acceptClientsTask;
        private CancellationTokenSource? _cancellationTokenSource;

        private System.Windows.Forms.Timer _screenCaptureTimer;
        private WaveInEvent _waveIn;
        private Rectangle _screenBounds;
        private bool _isMicrophoneMuted;

        public bool IsStreaming => _isStreaming;
        public bool IsMicrophoneMuted
        {
            get => _isMicrophoneMuted;
            set
            {
                _isMicrophoneMuted = value;
                OnStatusChanged(_isMicrophoneMuted ? "Microphone muted" : "Microphone active");
            }
        }

        public event EventHandler<Exception>? ErrorOccurred;
        public event EventHandler<string>? StatusChanged;
        public event EventHandler<int>? AudioLevelChanged;

        public StreamerService(StreamSettings settings)
        {
            _settings = settings;
            _clients = new List<TcpClient>();
            _adaptiveStreaming = new AdaptiveStreaming();
            _screenBounds = Screen.PrimaryScreen.Bounds;

            // Инициализация таймера захвата экрана
            _screenCaptureTimer = new System.Windows.Forms.Timer();
            _screenCaptureTimer.Tick += CaptureScreen;

            // Инициализация захвата звука
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(44100, 1),
                BufferMilliseconds = 50
            };
            _waveIn.DataAvailable += WaveIn_DataAvailable;
        }

        public async Task StartAsync()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _server = new TcpListener(IPAddress.Any, _settings.Port);
                _server.Start();
                _isStreaming = true;

                OnStatusChanged("Stream started");

                // Запускаем захват экрана
                _screenCaptureTimer.Interval = 1000 / _settings.FPS;
                _screenCaptureTimer.Start();

                // Запускаем захват звука
                _waveIn.StartRecording();

                _acceptClientsTask = AcceptClientsAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                await StopAsync();
            }
        }

        public async Task StopAsync()
        {
            _isStreaming = false;
            _cancellationTokenSource?.Cancel();

            // Останавливаем захват экрана и звука
            _screenCaptureTimer.Stop();
            _waveIn.StopRecording();

            foreach (var client in _clients.ToList())
            {
                client.Close();
            }
            _clients.Clear();

            if (_server != null)
            {
                _server.Stop();
                _server = null;
            }

            if (_acceptClientsTask != null)
            {
                try
                {
                    await _acceptClientsTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelling
                }
            }

            OnStatusChanged("Stream stopped");
        }

        private void CaptureScreen(object? sender, EventArgs e)
        {
            try
            {
                using var bitmap = new Bitmap(_screenBounds.Width, _screenBounds.Height);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(_screenBounds.Location, Point.Empty, _screenBounds.Size);
                }

                // Уменьшаем размер изображения
                var scaleFactor = Math.Min(1.0, 1920.0 / bitmap.Width);
                var newWidth = (int)(bitmap.Width * scaleFactor);
                var newHeight = (int)(bitmap.Height * scaleFactor);

                using var scaledBitmap = new Bitmap(newWidth, newHeight);
                using (var g = Graphics.FromImage(scaledBitmap))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(bitmap, 0, 0, newWidth, newHeight);
                }

                using var ms = new MemoryStream();
                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 50L);

                var codec = ImageCodecInfo.GetImageEncoders()
                    .First(c => c.FormatID == ImageFormat.Jpeg.Guid);

                scaledBitmap.Save(ms, codec, encoderParams);
                var videoFrame = new VideoFrame(ms.ToArray(), 50, newWidth, newHeight);
                _ = BroadcastFrameAsync(videoFrame);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
            }
        }

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            try
            {
                // Вычисляем уровень звука
                int audioLevel = CalculateAudioLevel(e.Buffer);
                AudioLevelChanged?.Invoke(this, audioLevel); // Отправляем уровень звука

                if (!_isMicrophoneMuted)
                {
                    var audioFrame = new AudioFrame(e.Buffer, _waveIn.WaveFormat.SampleRate, _waveIn.WaveFormat.Channels);
                    _ = BroadcastAudioAsync(audioFrame);
                }
                else
                {
                    // Если микрофон выключен, отправляем тишину
                    var silenceData = new byte[e.Buffer.Length];
                    var audioFrame = new AudioFrame(silenceData, _waveIn.WaveFormat.SampleRate, _waveIn.WaveFormat.Channels);
                    _ = BroadcastAudioAsync(audioFrame);
                    AudioLevelChanged?.Invoke(this, 0); // При выключенном микрофоне уровень 0
                }
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
            // Обрабатываем каждый второй байт, так как используем 16-битный звук
            for (int i = 0; i < buffer.Length; i += 2)
            {
                if (i + 1 < buffer.Length)
                {
                    // Преобразуем два байта в одно 16-битное значение
                    short sample = (short)((buffer[i + 1] << 8) | buffer[i]);
                    sum += Math.Abs(sample);
                }
            }

            // Преобразуем сумму в процентное значение (0-100)
            double averageLevel = sum / (double)(buffer.Length / 2);
            int percentLevel = (int)((averageLevel / 32768.0) * 100);
            // Увеличиваем чувствительность
            percentLevel = (int)(percentLevel * 2.5); // Можете настроить этот множитель
            return Math.Min(100, Math.Max(0, percentLevel));
        }

        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var client = await _server!.AcceptTcpClientAsync(cancellationToken);
                    _clients.Add(client);
                    OnStatusChanged($"New viewer connected. Total viewers: {_clients.Count}");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(ex);
                }
            }
        }

        private int _sequenceNumber = 0;

        private async Task BroadcastFrameAsync(VideoFrame frame)
        {
            try
            {
                var packet = new StreamPacket
                {
                    PacketType = 0,
                    DataSize = frame.Data.Length,
                    SequenceNumber = Interlocked.Increment(ref _sequenceNumber),
                    Data = frame.Data
                };

                var packetData = packet.ToBytes();

                foreach (var client in _clients.ToList())
                {
                    try
                    {
                        if (!client.Connected)
                        {
                            _clients.Remove(client);
                            continue;
                        }

                        var stream = client.GetStream();
                        await stream.WriteAsync(packetData);
                        await stream.FlushAsync();
                    }
                    catch
                    {
                        _clients.Remove(client);
                        client.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
            }
        }

        private async Task BroadcastAudioAsync(AudioFrame frame)
        {
            try
            {
                var packet = new StreamPacket
                {
                    PacketType = 1, // 1 для аудио
                    DataSize = frame.Data.Length,
                    SequenceNumber = 0,
                    Data = frame.Data
                };

                var packetData = packet.ToBytes();

                foreach (var client in _clients.ToList())
                {
                    try
                    {
                        if (!client.Connected)
                        {
                            _clients.Remove(client);
                            continue;
                        }

                        var stream = client.GetStream();
                        await stream.WriteAsync(packetData);
                        await stream.FlushAsync();
                    }
                    catch
                    {
                        _clients.Remove(client);
                        client.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
            }
        }

        private void OnErrorOccurred(Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
        }

        private void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(this, status);
        }

        public void Dispose()
        {
            _waveIn?.Dispose();
            _screenCaptureTimer?.Dispose();
            foreach (var client in _clients)
            {
                client.Dispose();
            }
            _cancellationTokenSource?.Dispose();
        }
    }
}