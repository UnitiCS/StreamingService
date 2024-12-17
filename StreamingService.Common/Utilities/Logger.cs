namespace StreamingService.Common.Utilities
{
    public class Logger
    {
        private readonly string _logPath;
        private readonly object _lockObject = new object();

        public Logger(string logPath = "streaming_service.log")
        {
            _logPath = logPath;
        }

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

            lock (_lockObject)
            {
                try
                {
                    File.AppendAllText(_logPath, logMessage + Environment.NewLine);
                }
                catch
                {
                    // Игнорируем ошибки записи в лог
                }
            }
        }

        public void LogError(Exception ex)
        {
            Log($"Error: {ex.Message}\nStackTrace: {ex.StackTrace}", LogLevel.Error);
        }
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}