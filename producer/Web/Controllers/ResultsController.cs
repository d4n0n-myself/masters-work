using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CsvHelper;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Npgsql;

namespace Web.Controllers;

[AllowAnonymous]
[Route("[controller]/[action]")]
[ApiController]
public class ResultsController : Controller
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<ResultsController> _logger;
    private readonly DatabaseOptions _config;

    public ResultsController(IMinioClient minioClient, ILogger<ResultsController> logger,
        IOptions<DatabaseOptions> options)
    {
        _minioClient = minioClient;
        _logger = logger;
        _config = options.Value;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TestAsync(string fileName, CancellationToken ct = default)
    {
        const string bucket = "datasets-output";

        var result = new List<string[]>();
        var hadError = false;

        var getArgs = new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(fileName)
            .WithCallbackStream(s =>
            {
                using var reader = new StreamReader(s);
                var readToEnd = reader.ReadToEnd();
                _logger.LogDebug(readToEnd);

                try
                {
                    using var parser = new CsvParser(new StringReader(readToEnd), CultureInfo.InvariantCulture);
                    while (parser.Read())
                    {
                        result.Add(parser.Record);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error response: ");
                    hadError = true;
                }
            });
        await _minioClient.GetObjectAsync(getArgs, ct);

        return hadError
            ? new StatusCodeResult(StatusCodes.Status500InternalServerError)
            : Ok(result);
    }

    public async Task<IActionResult> ListDatasetsAsync()
    {
        await using var connection = new NpgsqlConnection(_config.ConnectionString);
        var list = await connection.QueryAsync<string>("SELECT DISTINCT id FROM datasets");
        return Ok(list);
    }

    public async Task<IActionResult> ListFilesAsync([Required] string id, CancellationToken ct = default)
    {
        if (Guid.TryParse(id, out var guid))
            throw new Exception("cant parse guid");
        
        await using var connection = new NpgsqlConnection(_config.ConnectionString);
        var parameters = new { Guid = guid };
        var files = await connection.QueryAsync<string>(
            "SELECT filename FROM datasets WHERE id = @Guid", parameters);

        var result = new Dictionary<string, string[]>();
        
        foreach (var filename in files)
        {
            var data = await TestAsync(filename, ct);

            if (data is OkObjectResult okObjectResult)
                result.Add(filename, okObjectResult.Value as string[]);
        }

        return Ok(result);
    }
}