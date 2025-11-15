using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scrapper.Data;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly MetricsDbContext _context;

    public ConfigController(MetricsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var exporters = await _context.NodeExporterSettings.ToListAsync();
        return Ok(exporters);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var exporter = await _context.NodeExporterSettings.FindAsync(id);
        return exporter == null ? NotFound() : Ok(exporter);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var exporter = await _context.NodeExporterSettings.FindAsync(id);
        if (exporter == null) return NotFound();

        _context.NodeExporterSettings.Remove(exporter);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
