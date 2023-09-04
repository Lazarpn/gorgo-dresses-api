using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GorgoDresses.Api.Controllers;
[ApiController]
public class BaseController : ControllerBase
{
    protected Guid? GetCurrentUserId()
    {
        _ = Guid.TryParse(HttpContext.User.FindFirst(ClaimTypes.Name)?.Value, out Guid parsedId);
        return parsedId == Guid.Empty ? null : parsedId;
    }
}
