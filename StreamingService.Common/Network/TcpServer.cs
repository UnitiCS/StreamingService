using System.Net;
using System.Net.Sockets;

namespace StreamingService.Common.Network
{
    public class SocketServer : IDisposable
    {
        private readonly Socket _serverSocket;
        private readonly List<Socket> _clients;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly int _bufferSize;
        private bool _isRunning;

        public event EventHandler<Socket>? ClientConnected;
        public event EventHandler<Socket>? ClientDisconnected;
        public event EventHandler<Exception>? ErrorOccurred;

        public IReadOnlyList<Socket> Clients => _clients.AsReadOnly();
        public bool IsRunning => _isRunning;

        public SocketServer(int port, int bufferSize = 8192)
        {
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _clients = new List<Socket>();
            _cancellationTokenSource = new CancellationTokenSource();
            _bufferSize = bufferSize;

            _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            _serverSocket.Listen(100);
        }

        public async Task StartAsync()
        {
            if (_isRunning) return;

            try
            {
                _isRunning = true;

                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var clientSocket = await AcceptClientAsync();
                    if (clientSocket != null)
                    {
                        _clients.Add(clientSocket);
                        ClientConnected?.Invoke(this, clientSocket);
                        _ = HandleClientAsync(clientSocket);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                throw;
            }
        }

        private async Task<Socket> AcceptClientAsync()
        {
            try
            {
                return await Task.Factory.FromAsync(
                    _serverSocket.BeginAccept,
                    _serverSocket.EndAccept,
                    null);
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }

        private async Task HandleClientAsync(Socket clientSocket)
        {
            var buffer = new byte[_bufferSize];

            try
            {
                while (_isRunning && clientSocket.Connected)
                {
                    // Получаем размер данных
                    var headerBuffer = new byte[4];
                    var headerReceived = await ReceiveFullyAsync(clientSocket, headerBuffer);
                    if (headerReceived == 0) break;

                    var dataSize = BitConverter.ToInt32(headerBuffer, 0);
                    if (dataSize <= 0 || dataSize > _bufferSize) continue;

                    // Получаем данные
                    var dataBuffer = new byte[dataSize];
                    var dataReceived = await ReceiveFullyAsync(clientSocket, dataBuffer);
                    if (dataReceived == 0) break;

                    // Здесь можно обработать полученные данные
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
            finally
            {
                await DisconnectClientAsync(clientSocket);
            }
        }

        private async Task<int> ReceiveFullyAsync(Socket socket, byte[] buffer)
        {
            int totalBytesReceived = 0;
            while (totalBytesReceived < buffer.Length)
            {
                try
                {
                    var bytesReceived = await Task.Factory.FromAsync(
                        (callback, state) => socket.BeginReceive(buffer, totalBytesReceived,
                            buffer.Length - totalBytesReceived, SocketFlags.None, callback, state),
                        socket.EndReceive,
                        null);

                    if (bytesReceived == 0)
                        return 0;

                    totalBytesReceived += bytesReceived;
                }
                catch
                {
                    return 0;
                }
            }
            return totalBytesReceived;
        }

        public async Task BroadcastAsync(byte[] data)
        {
            if (data == null || data.Length == 0) return;

            var sizePrefix = BitConverter.GetBytes(data.Length);
            var fullMessage = new byte[sizePrefix.Length + data.Length];
            Buffer.BlockCopy(sizePrefix, 0, fullMessage, 0, sizePrefix.Length);
            Buffer.BlockCopy(data, 0, fullMessage, sizePrefix.Length, data.Length);

            var disconnectedClients = new List<Socket>();

            foreach (var client in _clients)
            {
                try
                {
                    if (!client.Connected)
                    {
                        disconnectedClients.Add(client);
                        continue;
                    }

                    await SendAsync(client, fullMessage);
                }
                catch
                {
                    disconnectedClients.Add(client);
                }
            }

            foreach (var client in disconnectedClients)
            {
                await DisconnectClientAsync(client);
            }
        }

        private async Task SendAsync(Socket socket, byte[] data)
        {
            try
            {
                await Task.Factory.FromAsync(
                    (callback, state) => socket.BeginSend(data, 0, data.Length, SocketFlags.None, callback, state),
                    socket.EndSend,
                    null);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                throw;
            }
        }

        private async Task DisconnectClientAsync(Socket client)
        {
            if (_clients.Remove(client))
            {
                try
                {
                    if (client.Connected)
                    {
                        client.Shutdown(SocketShutdown.Both);
                    }
                    client.Close();
                    ClientDisconnected?.Invoke(this, client);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                }
            }
        }

        public async Task StopAsync()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _cancellationTokenSource.Cancel();

            foreach (var client in _clients.ToList())
            {
                await DisconnectClientAsync(client);
            }

            try
            {
                _serverSocket.Close();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            StopAsync().Wait();

            foreach (var client in _clients.ToList())
            {
                try
                {
                    client.Dispose();
                }
                catch { }
            }
            _clients.Clear();

            try
            {
                _serverSocket.Dispose();
            }
            catch { }

            _cancellationTokenSource.Dispose();
        }
    }
}