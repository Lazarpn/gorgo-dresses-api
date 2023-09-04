using CaloriesTracking.Common.Models.User;
using GorgoDresses.Common.Helpers;
using GorgoDresses.Common.Models.Dress;
using GorgoDresses.Common.Models.User;
using GorgoDresses.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GorgoDresses.Api.Controllers;

[Route("api/dresses")]
[ApiController]
public class DressController : BaseController
{
    private readonly DressManager dressManager;

    public DressController(DressManager dressManager)
    {
        this.dressManager = dressManager;
    }

    /// <summary>
    /// Registers a user 
    /// </summary>
    /// <param name="model">UserRegisterModel</param>
    /// <returns>AuthResponse-User informations</returns>
    [HttpPost]
    [Authorize(Roles = UserRoleConstants.Administrator)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<AuthResponseModel>> CreateDress(DressCreateModel model)
    {
        await dressManager.CreateDress(model);
        return NoContent();
    }

    /// <summary>
    /// Registers a user 
    /// </summary>
    /// <param name="model">UserRegisterModel</param>
    /// <returns>AuthResponse-User informations</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DressModel))]
    public async Task<ActionResult<List<DressModel>>> GetDresses()
    {
        var dresses = await dressManager.GetDresses();
        return Ok(dresses);
    }
}
