namespace Producer;

public interface IBackgroundTask
{
    Task ExecuteAsync(CancellationToken ct);
}