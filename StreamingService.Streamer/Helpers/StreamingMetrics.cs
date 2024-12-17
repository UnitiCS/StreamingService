namespace StreamingService.Streamer.Helpers
{
    public class StreamingMetrics
    {
        private readonly Queue<long> _frameTimings;
        private readonly object _lock = new object();

        public StreamingMetrics()
        {
            _frameTimings = new Queue<long>();
        }

        public void AddFrameTiming(long milliseconds)
        {
            lock (_lock)
            {
                _frameTimings.Enqueue(milliseconds);
                if (_frameTimings.Count > 100) // Храним статистику по последним 100 кадрам
                {
                    _frameTimings.Dequeue();
                }
            }
        }

        public double GetAverageFrameTime()
        {
            lock (_lock)
            {
                return _frameTimings.Count > 0 ? _frameTimings.Average() : 0;
            }
        }

        public double GetCurrentFPS()
        {
            var avgFrameTime = GetAverageFrameTime();
            return avgFrameTime > 0 ? 1000.0 / avgFrameTime : 0;
        }
    }
}