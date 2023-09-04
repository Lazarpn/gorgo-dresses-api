using GorgoDresses.Common.Helpers;
using GorgoDresses.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GorgoDresses.Api.Controllers;

[Route("api/utilities")]
[ApiController]
public class UtilityController : ControllerBase
{
    private readonly GorgoDressesDbContext db;

    public UtilityController(GorgoDressesDbContext db)
    {
        this.db = db;
    }

    [HttpPost]
    [Route("seed-data")]
    [Authorize(UserRoleConstants.Administrator)]
    public async Task<IActionResult> SeedData()
    {
        await db.SeedData();
        return NoContent();
    }
}
