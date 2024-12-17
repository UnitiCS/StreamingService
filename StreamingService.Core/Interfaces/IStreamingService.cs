namespace StreamingService.Core.Interfaces
{
    public interface IStreamingService
    {
        Task StartAsync();
        Task StopAsync();
        bool IsStreaming { get; }
        event EventHandler<Exception> ErrorOccurred;
        event EventHandler<string> StatusChanged;
    }
}