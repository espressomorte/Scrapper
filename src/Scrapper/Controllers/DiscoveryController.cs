using Microsoft.AspNetCore.Mvc;
using Scrapper.Domain;
using Scrapper.Services;

[ApiController]
[Route("api/[controller]")]
public class DiscoveryController : ControllerBase
{
    private readonly IDiscoveryService _discoveryService;
    private readonly ILogger<DiscoveryController> _logger;

    public DiscoveryController(IDiscoveryService discoveryService, ILogger<DiscoveryController> logger)
    {
        _discoveryService = discoveryService;
        _logger = logger;
    }

    [HttpPost("AddSingle")]
    [Consumes("application/json")]
    public async Task<IActionResult> AddSingle([FromBody] string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return BadRequest("IP address is required.");
        }

        try
        {
            var result = await _discoveryService.AddSingleExporterAsync(ipAddress);

            return result.Status switch
            {
                DiscoveryResultStatus.Added => Ok(result.Setting),
                DiscoveryResultStatus.AlreadyExists => Conflict($"Exporter with IP {ipAddress} already exists."),
                DiscoveryResultStatus.Unreachable => NotFound($"Node Exporter not found or unreachable at {ipAddress}:9100."),
                _ => StatusCode(StatusCodes.Status500InternalServerError, "Unknown error")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AddSingle for IP: {IP}", ipAddress);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred during discovery.");
        }
    }

    /// <summary>
    /// POST /api/Discovery/Scan/{ipPrefix} - Start a network scan for a subnet.
    /// </summary>
    /// <param name="ipPrefix">The subnet prefix to scan (e.g., "192.168.1").</param>
    [HttpPost("Scan/{ipPrefix}")]
    public async Task<IActionResult> ScanNetwork(string ipPrefix)
    {
        var foundExporters = await _discoveryService.ScanNetworkAsync(ipPrefix);

        return Ok(new { Count = foundExporters.Count, Found = foundExporters });
    }
}