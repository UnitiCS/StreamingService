using System.Diagnostics;

namespace StreamingService.Common.Utilities
{
    public class PerformanceMonitor
    {
        private readonly Stopwatch _stopwatch;
        private readonly Queue<double> _measurements;
        private readonly int _maxMeasurements;

        public PerformanceMonitor(int maxMeasurements = 100)
        {
            _stopwatch = new Stopwatch();
            _measurements = new Queue<double>();
            _maxMeasurements = maxMeasurements;
        }

        public void Start()
        {
            _stopwatch.Restart();
        }

        public void Stop()
        {
            _stopwatch.Stop();
            AddMeasurement(_stopwatch.Elapsed.TotalMilliseconds);
        }

        private void AddMeasurement(double value)
        {
            _measurements.Enqueue(value);
            if (_measurements.Count > _maxMeasurements)
            {
                _measurements.Dequeue();
            }
        }

        public double GetAverageTime()
        {
            return _measurements.Count > 0 ? _measurements.Average() : 0;
        }

        public double GetFPS()
        {
            var avgTime = GetAverageTime();
            return avgTime > 0 ? 1000.0 / avgTime : 0;
        }
    }
}