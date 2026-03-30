using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlazorKuma.Data;

namespace BlazorKuma.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MonitorsController : ControllerBase
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public MonitorsController(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // GET: api/monitors/status
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var status = await db.Monitors
            .Select(m => new {
                m.Name,
                m.Target,
                m.IsUp,
                m.LastResponse,
                m.LastCheck
            })
            .ToListAsync();

        return Ok(status);
    }
}