using Confluent.Kafka;
using Microsoft.OpenApi.Models;
using Producer;

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

builder.Services.AddScoped<DatasetProducer>();
builder.Services.AddSingleton<DatasetConsumer>();
builder.Services.Configure<ProducerConfig>(builder.Configuration.GetSection(nameof(ProducerConfig)));
builder.Services.Configure<ConsumerConfig>(builder.Configuration.GetSection(nameof(ConsumerConfig)));

var app = builder.Build();

Task.Run(() => app.Services.GetRequiredService<DatasetConsumer>().ConsumeAsync());

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Dataset producer v1");
    options.RoutePrefix = "swagger";
});

app.MapControllers();

app.MapGet("/", () => "Hello World!");

app.Run();
