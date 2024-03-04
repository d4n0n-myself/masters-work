using Confluent.Kafka;
using Core;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.OpenApi.Models;
using Minio;
using Producer;
using DatabaseOptions = Producer.DatabaseOptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Dataset producer",
        Version = "v1",
    });
});

builder.Services.AddSingleton<DatasetProducer>();
builder.Services.AddSingleton<DatasetConsumer>();
builder.Services.Configure<ProducerConfig>(builder.Configuration.GetSection(nameof(ProducerConfig)));
builder.Services.Configure<ConsumerConfig>(builder.Configuration.GetSection(nameof(ConsumerConfig)));

var configurationSections = builder.Configuration.GetSection("Trackers")
    .GetChildren()
    .Select(x =>
    {
        var cf = new TrackerConfiguration();
        x.Bind(cf);
        return cf;
    })
    .ToArray();

builder.Services.AddSingleton(configurationSections);

var minioOptions = new MinioOptions();
builder.Configuration.GetSection(nameof(MinioOptions)).Bind(minioOptions);
builder.Services.AddSingleton(minioOptions);
var databaseOptions = new DatabaseOptions();
builder.Configuration.GetSection(nameof(DatabaseOptions)).Bind(databaseOptions);
builder.Services.AddSingleton(databaseOptions);

// Add Hangfire services.
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(databaseOptions.ConnectionString)));

// Add the processing server as IHostedService
builder.Services.AddHangfireServer();

// Add Minio using the custom endpoint and configure additional settings for default MinioClient initialization
builder.Services.AddMinio(configureClient => configureClient
    .WithEndpoint(minioOptions.Endpoint)
    .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey)
    .WithSSL(false));

builder.Services.AddSingleton<DatasetTrackingTask>();
builder.Services.AddSingleton<IDatasetExtractor, PostgreSqlDatasetExtractor>();

var app = builder.Build();
app.UseHangfireDashboard();

var tasksTypes = AppDomain.CurrentDomain.GetAssemblies()
    .SelectMany(x => x.GetTypes())
    .Where(x => typeof(IBackgroundTask).IsAssignableFrom(x) && x != typeof(IBackgroundTask));

foreach (var tasksType in tasksTypes)
{
    var service = (IBackgroundTask) app.Services.GetRequiredService(tasksType);
    var jobId = tasksType.FullName;
    RecurringJob.RemoveIfExists(jobId);
    RecurringJob.AddOrUpdate(jobId, 
        () => service.ExecuteAsync(default),
        Cron.Minutely);
}

Task.Run(() => app.Services.GetRequiredService<DatasetConsumer>().ConsumeAsync());

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Dataset producer v1");
    options.RoutePrefix = "swagger";
});

app.MapControllers();
app.MapHangfireDashboard();

app.MapGet("/", () => "Hello World!");

app.Run();
