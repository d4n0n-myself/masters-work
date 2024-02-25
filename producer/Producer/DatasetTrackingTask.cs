using Minio;
using Minio.DataModel.Args;

namespace Producer;

public class DatasetTrackingTask : IBackgroundTask
{
    private readonly IServiceProvider _container;
    private readonly IMinioClient _minioClient;
    private readonly DatasetProducer _producer;
    private readonly TrackerConfiguration[] _configurations;

    public DatasetTrackingTask(TrackerConfiguration[] options,
        IServiceProvider container,
        IMinioClient minioClient,
        DatasetProducer producer)
    {
        _container = container;
        _minioClient = minioClient;
        _producer = producer;
        _configurations = options;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        var enumerable = _container.GetServices(typeof(IDatasetExtractor))
            .Select(x => (IDatasetExtractor)x)
            .ToArray();

        foreach (var tracker in _configurations)
        {
            var extractor = enumerable.SingleOrDefault(x => x!.Type == tracker.Type);

            if (extractor == default)
                throw new Exception("Unknown tracker");

            var streams = await extractor.ExtractAsync(tracker.Configuration);

            foreach (var (datasetName, stream) in streams)
            {
                const string bucket = "datasets-input";
                await _minioClient.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(datasetName + ".csv")
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length)
                    .WithContentType("text/csv"),
                    ct);

                await _producer.ProduceAsync(datasetName, ct: ct);
            }
        }
    }
}