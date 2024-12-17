namespace StreamingService.Core.Services
{
    public class AdaptiveStreaming
    {
        private readonly int[] availableBitrates = { 500000, 1000000, 2000000, 4000000 };
        private int currentBitrateIndex = 1;
        private readonly object lockObject = new object();

        public int GetOptimalBitrate(long transmissionTime, int dataSize)
        {
            lock (lockObject)
            {
                // Рассчитываем текущую скорость передачи (bits per second)
                double currentBandwidth = (dataSize * 8.0 * 1000) / transmissionTime;

                // Адаптируем битрейт
                if (currentBandwidth < availableBitrates[currentBitrateIndex] * 0.8)
                {
                    if (currentBitrateIndex > 0)
                        currentBitrateIndex--;
                }
                else if (currentBandwidth > availableBitrates[currentBitrateIndex] * 1.2)
                {
                    if (currentBitrateIndex < availableBitrates.Length - 1)
                        currentBitrateIndex++;
                }

                return availableBitrates[currentBitrateIndex];
            }
        }

        public int GetCurrentQuality()
        {
            // Преобразуем битрейт в качество изображения (0-100)
            return (int)((currentBitrateIndex + 1.0) / availableBitrates.Length * 100);
        }
    }
}