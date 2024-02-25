using System.Globalization;
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
            var tableColumns = (await connection.QueryAsync<string>(
                "select column_name from information_schema.columns where table_schema = 'public' and table_name = @TableName;",
                parameters))
                .ToArray();

            var stream = new MemoryStream();
            await using var streamWriter = new StreamWriter(stream, leaveOpen: true);
            await using var csvWriter = new CsvHelper.CsvWriter(streamWriter, CultureInfo.InvariantCulture);

            var records = await connection.QueryAsync($"SELECT {string.Join(",", tableColumns)} FROM public.{tableName}");

            await csvWriter.WriteRecordsAsync(records);

            result.Add(tableName, stream);

            await connection.ExecuteAsync(
                "update public.datasets set processed = true where table_name = @TableName;", parameters);
        }

        return result;
    }
}