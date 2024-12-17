namespace StreamingService.Core.Models
{
    public class AudioFrame
    {
        public byte[] Data { get; set; }
        public long Timestamp { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public byte PacketType { get; } = 1; // 1 для аудио

        public AudioFrame(byte[] data, int sampleRate, int channels)
        {
            Data = data;
            SampleRate = sampleRate;
            Channels = channels;
            Timestamp = DateTime.UtcNow.Ticks;
        }
    }
}