using System.Net.Sockets;

namespace StreamingService.Common.Utilities
{
    public static class Extensions
    {
        public static byte[] ToByteArray(this Stream stream)
        {
            if (stream is MemoryStream memoryStream)
            {
                return memoryStream.ToArray();
            }

            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public static async Task<byte[]> ToByteArrayAsync(this Stream stream)
        {
            if (stream is MemoryStream memoryStream)
            {
                return memoryStream.ToArray();
            }

            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
                return ms.ToArray();
            }
        }

        public static async Task<int> ReadAsync(this NetworkStream stream, byte[] buffer)
        {
            return await stream.ReadAsync(buffer, 0, buffer.Length);
        }

        public static async Task WriteAsync(this NetworkStream stream, byte[] buffer)
        {
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }
    }
}