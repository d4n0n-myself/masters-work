using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Producer;

[AllowAnonymous]
[Route("[controller]/[action]")]
[ApiController]
public class ProduceController : ControllerBase
{
    private readonly DatasetProducer _producer;

    public ProduceController(DatasetProducer producer)
    {
        _producer = producer;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ProduceAsync(string url, CancellationToken ct = default) =>
        Ok(await _producer.ProduceAsync(url, ct));
}
