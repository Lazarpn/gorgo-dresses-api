using GorgoDresses.Common.Helpers;
using GorgoDresses.Common.Models.User;
using GorgoDresses.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.X509;

namespace GorgoDresses.Api.Controllers;
[Route("api/accounts")]
[ApiController]
public class AccountController : BaseController
{
    private readonly AccountManager accountManager;

    public AccountController(AccountManager accountManager)
    {
        this.accountManager = accountManager;
    }

    /// <summary>
    /// Registers a user 
    /// </summary>
    /// <param name="model">UserRegisterModel</param>
    /// <returns>AuthResponse-User informations</returns>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponseModel))]
    public async Task<ActionResult<AuthResponseModel>> Register([FromBody] UserRegisterModel model)
    {
        var authResponse = await accountManager.Register(model);
        return Ok(authResponse);
    }

    /// <summary>
    /// Logs a user
    /// </summary>
    /// <param name="model">UserLoginModel</param>
    /// <returns>AuthResponse-User informations</returns>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponseModel))]
    public async Task<ActionResult<AuthResponseModel>> Login([FromBody] UserLoginModel model)
    {
        var authResponse = await accountManager.Login(model);
        return Ok(authResponse);
    }

    /// <summary>
    /// Verifies the user email using the 6-digit verification code
    /// </summary>
    /// <param name="model">Object containing 6-digit verification code</param>
    /// <returns></returns>
    [HttpPut("verify-email")]
    //[Authorize(Policy = Policies.NotConfirmedEmail)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> VerifyEmail([FromBody] UserConfirmEmailModel model)
    {
        await accountManager.ConfirmEmail(GetCurrentUserId().Value, model);
        return NoContent();
    }

    /// <summary>
    /// Resends the verification email to the user
    /// </summary>
    /// <returns>ResentEmailResponse</returns>
    [HttpPut("verify-email/resend")]
    //[Authorize(Policy = Policies.NotConfirmedEmail)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResentEmailResponseModel))]
    public async Task<IActionResult> ResendVerificationEmail()
    {
        var result = await accountManager.ResendVerificationEmail(GetCurrentUserId().Value);
        return Ok(new ResentEmailResponseModel { NewCodeExpiryDate = result });
    }

    /// <summary>
    /// Allows the user to update their email while it's still not confirmed
    /// </summary>
    /// <param name="model"></param>
    [HttpPut("verify-email/change")]
    //[Authorize(Policy = Policies.NotConfirmedEmail)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangeVerificationEmail([FromBody] ChangeEmailModel model)
    {
        await accountManager.ChangeVerificationEmail(GetCurrentUserId().Value, model);
        return NoContent();
    }

    ///<summary>
    /// Starts the password reset process by sending a forgot password email
    /// </summary>
    /// <param name="model"></param>
    [HttpPut("password/forgot")]
    //[Authorize(Policy = Policies.RegisteredUser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
    {
        await accountManager.ForgotPassword(model);
        return NoContent();
    }

    /// <summary>
    /// Verifies and completes the reset password process
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("password/reset")]
    //[Authorize(Policy = Policies.RegisteredUser)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
    {
        await accountManager.ResetPassword(model);
        return NoContent();
    }
}
