namespace StreamingService.Core.Models
{
    public class StreamSettings
    {
        public int VideoQuality { get; set; } = 75;
        public int FPS { get; set; } = 30;
        public int AudioBitrate { get; set; } = 44100;
        public int Port { get; set; } = 8888;
        public string ServerIP { get; set; } = "127.0.0.1";
        public int BufferSize { get; set; } = 8192;
        public int MaxClients { get; set; } = 100;
    }
}