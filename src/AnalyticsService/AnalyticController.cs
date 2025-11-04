using Microsoft.AspNetCore.Mvc;
using Scrapper.Domain; // Потрібний для Metric
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnalyticsService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly AnalyticsService _analyticsService;

        public AnalyticsController(AnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        /// <summary>
        /// Отримує агреговані дані використання мережі (Total, Average, Min, Max) 
        /// для всіх пристроїв за вказану кількість останніх метрик.
        /// </summary>
        [HttpGet("network-summary")]
        [ProducesResponseType(typeof(NetworkUsageSummary), 200)]
        public async Task<ActionResult<NetworkUsageSummary>> GetNetworkSummary([FromQuery] int limit = 100, [FromQuery] string metricNameFilter = "node_network_receive_bytes_total")
        {
            var summary = await _analyticsService.GetAggregatedNetworkUsageAsync(limit, metricNameFilter);
            return Ok(summary);
        }
    }
}
