namespace StreamingService.Core.Interfaces
{
    public interface IAudioCapture
    {
        void StartCapture();
        void StopCapture();
        event EventHandler<byte[]> AudioDataAvailable;
    }
}