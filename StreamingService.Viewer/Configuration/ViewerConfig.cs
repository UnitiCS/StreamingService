namespace StreamingService.Viewer.Configuration
{
    public static class ViewerConfig
    {
        public static class Defaults
        {
            public const string DefaultServerIP = "127.0.0.1";
            public const int DefaultPort = 8888;
            public const int DefaultBufferSize = 1024 * 1024; // 1MB
            public const int DefaultTimeout = 5000; // 5 seconds
        }

        public static class Limits
        {
            public const int MinPort = 1024;
            public const int MaxPort = 65535;
            public const int MinBufferSize = 1024;
            public const int MaxBufferSize = 10 * 1024 * 1024; // 10MB
        }
    }
}