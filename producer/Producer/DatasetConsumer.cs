using Confluent.Kafka;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Producer;

public class DatasetConsumer
{
    private readonly ConsumerConfig _consumerConfig;
    private readonly DatabaseOptions _databaseConfig;
    private const string TopicName = "datasets_output";

    public DatasetConsumer(IOptions<ConsumerConfig> consumerOptions,
        IOptions<DatabaseOptions> databaseOptions)
    {
        _consumerConfig = consumerOptions.Value;
        _databaseConfig = databaseOptions.Value;
    }

    public async Task ConsumeAsync(CancellationToken ct = default)
    {
        using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
        await using var connection = new NpgsqlConnection(_databaseConfig.ConnectionString);

        try
        {
            consumer.Subscribe(TopicName);

            while (!ct.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(ct);
                var parameters = new { Id = consumeResult.Message.Key, FileName = consumeResult.Message.Value };
                await connection.ExecuteAsync("INSERT INTO results (id, filename) VALUES (@Id, @FileName)",
                    parameters);
            }
        }
        finally
        {
            consumer.Close();
        }
    }
}
