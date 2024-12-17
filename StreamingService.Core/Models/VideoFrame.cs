namespace StreamingService.Core.Models
{
    public class VideoFrame
    {
        public byte[] Data { get; set; }
        public long Timestamp { get; set; }
        public int Quality { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public byte PacketType { get; } = 0; // 0 для видео

        public VideoFrame(byte[] data, int quality, int width, int height)
        {
            Data = data;
            Quality = quality;
            Width = width;
            Height = height;
            Timestamp = DateTime.UtcNow.Ticks;
        }
    }
}