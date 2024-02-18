using System.Globalization;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;

namespace Web.Controllers;

[AllowAnonymous]
[Route("[controller]/[action]")]
[ApiController]
public class ResultsController : Controller
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<ResultsController> _logger;

    public ResultsController(IMinioClient minioClient, ILogger<ResultsController> logger)
    {
        _minioClient = minioClient;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Test(string fileName, CancellationToken ct = default)
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
}