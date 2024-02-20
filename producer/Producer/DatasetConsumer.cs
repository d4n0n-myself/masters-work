using Confluent.Kafka;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Producer;

public class DatasetConsumer
{
    private readonly ConsumerConfig _config;
    private const string TopicName = "datasets_output";

    public DatasetConsumer(IOptions<ConsumerConfig> optionsSnapshot)
    {
        _config = optionsSnapshot.Value;
    }

    public async Task ConsumeAsync(CancellationToken ct = default)
    {
        using var consumer = new ConsumerBuilder<string, string>(_config).Build();
        await using var connection = new NpgsqlConnection();

        try
        {
            consumer.Subscribe(TopicName);

            while (!ct.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(ct);
                var parameters = new { Id = consumeResult.Message.Key, FileName = consumeResult.Message.Value };
                await connection.ExecuteAsync("INSERT INTO datasets (id, filename) VALUES (@Id, @FileName)",
                    parameters);
            }
        }
        finally
        {
            consumer.Close();
        }
    }
}
