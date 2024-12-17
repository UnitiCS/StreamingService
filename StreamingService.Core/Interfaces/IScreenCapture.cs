using System.Drawing;

namespace StreamingService.Core.Interfaces
{
    public interface IScreenCapture
    {
        Bitmap CaptureScreen();
        void StartCapture();
        void StopCapture();
        event EventHandler<Bitmap> FrameCaptured;
    }
}