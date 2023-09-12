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
    /// <returns>Returns dress admin basic info</returns>
    [HttpPost]
    [Authorize(Roles = UserRoleConstants.Administrator)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = (typeof(DressAdminBasicInfoModel)))]
    public async Task<ActionResult<DressAdminBasicInfoModel>> CreateDress([FromForm] DressCreateModel model)
    {
        var dress = await dressManager.CreateDress(GetCurrentUserId().Value, model);
        return Ok(dress);
    }

    /// <summary>
    /// Registers a user 
    /// </summary>
    /// <returns>Returns dresses basic info for admin</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DressAdminBasicInfoModel))]
    public async Task<ActionResult<List<DressAdminBasicInfoModel>>> GetDressesAdminBasicInfo()
    {
        var dresses = await dressManager.GetAdminDressesBasicInfo();
        return Ok(dresses);
    }

    /// <summary>
    /// Registers a user 
    /// </summary>
    /// <returns>Returns dresses basic info</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = UserRoleConstants.Administrator)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DressAdminModel))]
    public async Task<ActionResult<DressAdminModel>> GetDressAdmin(Guid id)
    {
        var dresses = await dressManager.GetDressAdmin(id);
        return Ok(dresses);
    }

    /// <summary>
    /// Deletes a dress
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = UserRoleConstants.Administrator)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDress(Guid id)
    {
        await dressManager.DeleteDress(id);
        return NoContent();
    }

    /// <summary>
    /// Gets dresses basic info 
    /// </summary>
    /// <returns>Returns dresses basic info</returns>
    [HttpGet("basic")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DressBasicInfoModel))]
    public async Task<ActionResult<List<DressBasicInfoModel>>> GetDressesBasicInfo()
    {
        var dresses = await dressManager.GetDressesBasicInfo();
        return Ok(dresses);
    }

    /// <summary>
    /// Gets Dress Types 
    /// </summary>
    /// <returns>Returns dresses basic info</returns>
    [HttpGet("types")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DressTypeModel))]
    public async Task<ActionResult<List<DressTypeModel>>> GetDressTypes()
    {
        var dressTypes = await dressManager.GetDressTypes();
        return Ok(dressTypes);
    }
}
