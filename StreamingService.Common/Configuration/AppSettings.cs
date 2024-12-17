namespace StreamingService.Common.Configuration
{
    public class AppSettings
    {
        public NetworkSettings Network { get; set; } = new NetworkSettings();
        public VideoSettings Video { get; set; } = new VideoSettings();
        public AudioSettings Audio { get; set; } = new AudioSettings();
    }

    public class NetworkSettings
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 8888;
        public int BufferSize { get; set; } = 8192;
        public int Timeout { get; set; } = 5000;
    }

    public class VideoSettings
    {
        public int Quality { get; set; } = 75;
        public int FPS { get; set; } = 30;
        public bool EnableAdaptiveBitrate { get; set; } = true;
    }

    public class AudioSettings
    {
        public int SampleRate { get; set; } = 44100;
        public int Channels { get; set; } = 1;
        public int BitRate { get; set; } = 128000;
        public bool EnableNoiseSuppression { get; set; } = true;
    }
}