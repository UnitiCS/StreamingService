using StreamingService.Core.Interfaces;
using System.Drawing;
using System.Drawing.Imaging;

namespace StreamingService.Streamer.Services
{
    public class ScreenCapture : IScreenCapture
    {
        private bool _isCapturing;
        private readonly int _fps;
        private CancellationTokenSource? _cancellationTokenSource;

        public event EventHandler<Bitmap>? FrameCaptured;

        public ScreenCapture(int fps = 30)
        {
            _fps = fps;
        }

        public Bitmap CaptureScreen()
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);

            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            }

            return screenshot;
        }

        public void StartCapture()
        {
            if (_isCapturing) return;

            _isCapturing = true;
            _cancellationTokenSource = new CancellationTokenSource();

            Task.Run(CaptureLoop, _cancellationTokenSource.Token);
        }

        public void StopCapture()
        {
            _isCapturing = false;
            _cancellationTokenSource?.Cancel();
        }

        private async Task CaptureLoop()
        {
            while (_isCapturing)
            {
                try
                {
                    using (var frame = CaptureScreen())
                    {
                        FrameCaptured?.Invoke(this, frame);
                    }

                    await Task.Delay(1000 / _fps);
                }
                catch (Exception)
                {
                    // Добавить логирование
                }
            }
        }
    }
}