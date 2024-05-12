using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Core;
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
    public async Task<IActionResult> SingleAsync(string fileName, CancellationToken ct = default)
    {
        const string bucket = "datasets-output";

        var result = new List<string[]>();
        var hadError = false;
        var memStream = new MemoryStream(); 

        var getArgs = new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(fileName)
            .WithCallbackStream(s =>
            {
                s.CopyTo(memStream);
                memStream.Seek(0, SeekOrigin.Begin);
            });

        await _minioClient.GetObjectAsync(getArgs, ct);

        using var reader = new StreamReader(memStream);
        var readToEnd = await reader.ReadToEndAsync(ct);
        _logger.LogDebug(readToEnd);

        try
        {
            using var parser = new CsvParser(new StringReader(readToEnd), CultureInfo.InvariantCulture);
            while (await parser.ReadAsync())
            {
                result.Add(parser.Record);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error response: ");
            hadError = true;
        }
        
        return hadError
            ? new StatusCodeResult(StatusCodes.Status500InternalServerError)
            : Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListDatasetsAsync()
    {
        await using var connection = new NpgsqlConnection(_config.ConnectionString);
        var list = await connection.QueryAsync<string>("SELECT DISTINCT lower(id) FROM results;");
        return Ok(list);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListFilesAsync([Required] string id, CancellationToken ct = default)
    {
        if (!Guid.TryParse(id, out _))
            throw new Exception("cant parse guid");

        await using var connection = new NpgsqlConnection(_config.ConnectionString);
        var parameters = new { Guid = id };
        var files = await connection.QueryAsync<string>(
            "SELECT filename FROM results WHERE id = @Guid", parameters);

        var result = new Dictionary<string, List<string[]>>();

        foreach (var filename in files)
        {
            var data = await SingleAsync(filename, ct);

            if (data is not OkObjectResult okObjectResult) continue;
            var startHandlerNameIndex = filename.IndexOf('_') + 1;
            var endHandlerNameIndex = filename.LastIndexOf('_');
            var fileNameToDisplay = filename[startHandlerNameIndex..endHandlerNameIndex];
            result.Add(fileNameToDisplay, (List<string[]>)okObjectResult.Value);
        }

        return Ok(result);
    }
}