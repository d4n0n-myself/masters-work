using Dapper;
using Npgsql;

namespace Producer;

public class PostgreSqlDatasetExtractor : IDatasetExtractor
{
    public TrackerType Type => TrackerType.PostgreSql;

    public async Task<Dictionary<string, Stream>> ExtractAsync(IConfigurationSection section)
    {
        var configuration = section.Get<PostgreSqlConnectionConfiguration>();

        if (configuration == default || string.IsNullOrEmpty(configuration.ConnectionString))
            throw new Exception("Bad PostgreSql connection options for tracker");

        await using var connection = new NpgsqlConnection(configuration.ConnectionString);

        var tableNames = await connection.QueryAsync<string>(
            "select table_name from public.datasets where not processed;");

        var result = new Dictionary<string, Stream>();

        foreach (var tableName in tableNames)
        {
            var parameters = new { TableName = tableName };
            var tableColumns = await connection.QueryAsync<string>(
                "select column_name from information_schema.columns where table_schema = 'public' and table_name = @TableName;",
                parameters);

            // new csv
            // add to stream

            var stream = new MemoryStream();
            stream.Seek(0, SeekOrigin.Begin);
            result.Add(tableName, stream);

            await connection.ExecuteAsync(
                "update public.datasets set processed = true where table_name = @TableName;", parameters);
        }

        return result;
    }
}