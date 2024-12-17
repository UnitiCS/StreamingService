namespace StreamingService.Streamer.Configuration
{
    public class StreamerConfig
    {
        public static class Defaults
        {
            public const int DefaultPort = 8888;
            public const int DefaultQuality = 75;
            public const int DefaultFPS = 30;
            public const int DefaultAudioSampleRate = 44100;
            public const int DefaultBufferSize = 8192;
        }

        public static class Limits
        {
            public const int MinPort = 1024;
            public const int MaxPort = 65535;
            public const int MinQuality = 1;
            public const int MaxQuality = 100;
            public const int MinFPS = 1;
            public const int MaxFPS = 60;
        }
    }
}