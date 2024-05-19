using Confluent.Kafka;
using Dapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Npgsql;

namespace Producer;

public class DatasetConsumer
{
    private readonly ConsumerConfig _consumerConfig;
    private readonly DatabaseOptions _databaseConfig;
    private const string TopicName = "datasets_output";
    private readonly ILogger<DatasetConsumer> _logger;

    public DatasetConsumer(IOptions<ConsumerConfig> consumerOptions,
        DatabaseOptions databaseOptions, ILogger<DatasetConsumer> logger)
    {
        _consumerConfig = consumerOptions.Value;
        _databaseConfig = databaseOptions;
        _logger = logger;
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
                var type = new { best_model = "", accuracy = "", file_name = "" };
                var xx = JsonConvert.DeserializeAnonymousType(consumeResult.Message.Value, type);

                var parameters = new
                {
                    Id = consumeResult.Message.Key,
                    FileName = xx.file_name,
                    BestModel = xx.best_model,
                    Accuracy = xx.accuracy
                };
                await connection.ExecuteAsync(
                    "INSERT INTO results (id, filename, best_model, accuracy, created_date) " +
                    "VALUES (@Id, @FileName, @BestModel, @Accuracy, now())",
                    parameters);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in handling message");
        }
        finally
        {
            consumer.Close();
        }
    }
}
