using Microsoft.AspNetCore.Mvc;
using Scrapper.Data;
using Scrapper.Domain;

namespace Scrapper.Controllers
{
    [ApiController]
    [Route("[controller]")] // Базовий маршрут: /metrics
    public class MetricsController : ControllerBase
    {
        private readonly IMetricsRepository _repository;

        public MetricsController(IMetricsRepository repository)
        {
            _repository = repository;
        }

        // Фінальний маршрут: GET /metrics/data (https://localhost:5000/metrics/data?limit=10&device=eth0)
        [HttpGet("data")]
        [ProducesResponseType(typeof(IEnumerable<Metric>), 200)]
        public async Task<ActionResult<IEnumerable<Metric>>> GetMetricsData([FromQuery] int limit = 100, [FromQuery] string? device = null)
        {
            try
            {
                var metrics = await _repository.GetMetricsAsync(limit, device);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching metrics: {ex.Message}");
            }
        }
    }
}

