namespace Producer;

public class PostgreSqlConnectionConfiguration : ITrackerConfiguration
{
    public string ConnectionString { get; set; }
}