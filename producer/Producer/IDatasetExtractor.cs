namespace Producer;

public interface IDatasetExtractor
{
    TrackerType Type { get; }

    Task<Dictionary<string, Stream>> ExtractAsync(ITrackerConfiguration configuration);
}