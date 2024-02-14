using System.Text.Json;
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

    public async Task ProduceAsync(DatasetMessageArgs value, CancellationToken ct = default)
    {
        using var p = new ProducerBuilder<Null, DatasetMessageArgs>(_config)
            .SetKeySerializer(new KafkaJsonSerializer<Null>(new JsonSerializerOptions() { }))
            .SetValueSerializer(new KafkaJsonSerializer<DatasetMessageArgs>())
            .Build();

        try
        {
            var dr = await p.ProduceAsync(TopicName, new Message<Null, DatasetMessageArgs>() { Value = value }, ct);
            _logger.LogInformation($"Delivered '{dr.Value}' to '{dr.TopicPartitionOffset}'");
        }
        catch (ProduceException<Null, string> e)
        {
            _logger.LogInformation($"Delivery failed: {e.Error.Reason}");
        }
    }
}
