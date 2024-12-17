using System.Collections.Concurrent;

namespace StreamingService.Common.Utilities
{
    public class BufferPool
    {
        private readonly ConcurrentBag<byte[]> _buffers;
        private readonly int _bufferSize;
        private readonly int _maxBuffers;

        public BufferPool(int bufferSize = 8192, int maxBuffers = 100)
        {
            _bufferSize = bufferSize;
            _maxBuffers = maxBuffers;
            _buffers = new ConcurrentBag<byte[]>();
        }

        public byte[] Rent()
        {
            if (_buffers.TryTake(out byte[] buffer))
            {
                return buffer;
            }
            return new byte[_bufferSize];
        }

        public void Return(byte[] buffer)
        {
            if (buffer.Length != _bufferSize || _buffers.Count >= _maxBuffers)
            {
                return;
            }
            _buffers.Add(buffer);
        }
    }
}