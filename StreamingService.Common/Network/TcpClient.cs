using System.Net.Sockets;

namespace StreamingService.Common.Network
{
    public class NetworkClient : IDisposable
    {
        private readonly Socket _socket;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly int _bufferSize;
        private bool _isConnected;

        public event EventHandler<byte[]>? DataReceived;
        public event EventHandler? Disconnected;
        public event EventHandler<Exception>? ErrorOccurred;

        public bool IsConnected => _isConnected && _socket.Connected;

        public NetworkClient(int bufferSize = 8192)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _cancellationTokenSource = new CancellationTokenSource();
            _bufferSize = bufferSize;
        }

        public async Task ConnectAsync(string host, int port)
        {
            try
            {
                await Task.Factory.FromAsync(
                    (callback, state) => _socket.BeginConnect(host, port, callback, state),
                    _socket.EndConnect,
                    null);

                _isConnected = true;
                _ = ListenAsync();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                throw;
            }
        }

        public async Task SendAsync(byte[] data)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Client is not connected");

            try
            {
                // Отправляем размер данных
                var sizePrefix = BitConverter.GetBytes(data.Length);
                var fullMessage = new byte[sizePrefix.Length + data.Length];
                Buffer.BlockCopy(sizePrefix, 0, fullMessage, 0, sizePrefix.Length);
                Buffer.BlockCopy(data, 0, fullMessage, sizePrefix.Length, data.Length);

                await Task.Factory.FromAsync(
                    (callback, state) => _socket.BeginSend(fullMessage, 0, fullMessage.Length,
                        SocketFlags.None, callback, state),
                    _socket.EndSend,
                    null);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                throw;
            }
        }

        private async Task ListenAsync()
        {
            var buffer = new byte[_bufferSize];

            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested && IsConnected)
                {
                    // Получаем размер данных
                    var headerBuffer = new byte[4];
                    var headerReceived = await ReceiveFullyAsync(headerBuffer);
                    if (headerReceived == 0)
                        break;

                    var dataSize = BitConverter.ToInt32(headerBuffer, 0);
                    if (dataSize <= 0 || dataSize > _bufferSize)
                        continue;

                    // Получаем данные
                    var dataBuffer = new byte[dataSize];
                    var dataReceived = await ReceiveFullyAsync(dataBuffer);
                    if (dataReceived == 0)
                        break;

                    DataReceived?.Invoke(this, dataBuffer);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
            finally
            {
                Disconnect();
            }
        }

        private async Task<int> ReceiveFullyAsync(byte[] buffer)
        {
            int totalBytesReceived = 0;
            while (totalBytesReceived < buffer.Length)
            {
                try
                {
                    var bytesReceived = await Task.Factory.FromAsync(
                        (callback, state) => _socket.BeginReceive(buffer, totalBytesReceived,
                            buffer.Length - totalBytesReceived, SocketFlags.None, callback, state),
                        _socket.EndReceive,
                        null);

                    if (bytesReceived == 0)
                        return 0;

                    totalBytesReceived += bytesReceived;
                }
                catch (Exception)
                {
                    return 0;
                }
            }
            return totalBytesReceived;
        }

        public void Disconnect()
        {
            if (!_isConnected) return;

            _isConnected = false;
            try
            {
                if (_socket.Connected)
                {
                    _socket.Shutdown(SocketShutdown.Both);
                }
                _socket.Close();
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
        }

        public void Dispose()
        {
            try
            {
                _cancellationTokenSource.Cancel();
                Disconnect();
                _socket.Dispose();
                _cancellationTokenSource.Dispose();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
        }
    }
}