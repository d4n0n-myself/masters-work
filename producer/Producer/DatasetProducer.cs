using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Producer;

public class DatasetProducer
{
    private readonly ILogger<DatasetProducer> _logger;
    private readonly ProducerConfig _config;

    public DatasetProducer(IOptions<ProducerConfig> optionsSnapshot, ILogger<DatasetProducer> logger)
    {
        _logger = logger;
        _config = optionsSnapshot.Value;
    }

    private const string TopicName = "datasets_input";

    public async Task<string> ProduceAsync(string value, CancellationToken ct = default)
    {
        using var p = new ProducerBuilder<string, string>(_config)
            .Build();

        try
        {
            var newGuid = Guid.NewGuid().ToString();
            var dr = await p.ProduceAsync(TopicName, new Message<string, string> { Key = newGuid, Value = value }, ct);
            _logger.LogDebug("Delivered '{Value}' with key '{Key}' to '{TopicPartitionOffset}'",
                dr.Value, dr.Key, dr.TopicPartitionOffset);
            return newGuid;
        }
        catch (ProduceException<Null, string> e)
        {
            _logger.LogError("Delivery failed: {Reason}", e.Error.Reason);
            throw new Exception("Delivery failed");
        }
    }
}
