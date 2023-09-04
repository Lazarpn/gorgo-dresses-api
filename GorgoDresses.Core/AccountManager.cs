using AutoMapper;
using Azure.Identity;
using GorgoDresses.Common.Enums;
using GorgoDresses.Common.Exceptions;
using GorgoDresses.Common.Helpers;
using GorgoDresses.Common.Models.User;
using GorgoDresses.Data;
using GorgoDresses.Entities;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Microsoft.IdentityModel.Tokens;
using RTools_NTS.Util;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using CaloriesTracking.Common.Models.User;

namespace GorgoDresses.Core;

public class AccountManager
{
    private readonly GorgoDressesDbContext db;
    //TODO: setup sendgrid-a
    //private readonly SendGridEmailManager emailManager;
    private readonly JwtHelper jwtHelper;
    private readonly IMapper mapper;
    private readonly UserManager<User> userManager;
    private readonly IConfiguration configuration;
    private readonly string ANGULAR_APP_URL;
    private readonly int MINUTES_VERIFICATION_CODE_IS_VALID;

    public AccountManager(
        IMapper mapper,
        UserManager<User> userManager,
        IConfiguration configuration,
        GorgoDressesDbContext db
        //SendGridEmailManager emailManager
        )
    {
        this.mapper = mapper;
        this.configuration = configuration;
        this.userManager = userManager;
        this.db = db;
        //this.emailManager = emailManager;
        jwtHelper = new JwtHelper(configuration);
        ANGULAR_APP_URL = configuration["AngularAppUrl"];
        MINUTES_VERIFICATION_CODE_IS_VALID = Convert.ToInt32(configuration["minutesVerificationCodeIsValid"]);
    }

    public async Task<AuthResponseModel> Login(UserLoginModel model)
    {
        var user = await userManager.FindByEmailAsync(model.Email);
        ValidationHelper.MustExist(user);

        bool isValidPassword = await userManager.CheckPasswordAsync(user, model.Password);

        if (!isValidPassword)
        {
            throw new ValidationException(ErrorCode.InvalidCredentials);
        }

        var roles = await userManager.GetRolesAsync(user);
        var token = jwtHelper.GenerateJwtToken(user.Id, user.Email, roles);

        return new AuthResponseModel
        {
            Token = token,
        };
    }

    public async Task<AuthResponseModel> Register(UserRegisterModel model)
    {
        var user = await userManager.FindByEmailAsync(model.Email);
        ValidationHelper.MustNotExist(user);

        var passwordValidator = new PasswordValidator<User>();
        var passwordValidationResult = await passwordValidator.ValidateAsync(userManager, null, model.Password);

        if (!passwordValidationResult.Succeeded)
        {
            throw new ValidationException(ErrorCode.RequirementsNotMet);
        }

        using var transaction = await db.Database.BeginTransactionAsync();
        var newUser = new User
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            UserName = model.Email,
            EmailConfirmed = false
        };

        try
        {
            var userCreateResult = await userManager.CreateAsync(newUser, model.Password);

            if (!userCreateResult.Succeeded)
            {
                throw new ValidationException(ErrorCode.IdentityError);
            }

            var roleResult = await userManager.AddToRoleAsync(newUser, UserRoleConstants.User);

            if (!roleResult.Succeeded)
            {
                throw new ValidationException(ErrorCode.IdentityError);
            }

            await transaction.CommitAsync();
            await SendVerificationEmail(newUser);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw new ValidationException(ErrorCode.IdentityError);
        }

        var roles = await userManager.GetRolesAsync(newUser);
        var token = jwtHelper.GenerateJwtToken(newUser.Id, newUser.Email, roles);

        return new AuthResponseModel
        {
            Token = token,
        };
    }

    public async Task ForgotPassword(ForgotPasswordModel model)
    {
        var user = await userManager.FindByEmailAsync(model.Email);
        ValidationHelper.MustExist(user);

        var resetPasswordToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetPasswordToken));
        var resetPasswordUrl = $"{ANGULAR_APP_URL}/auth/reset-password/{user.Id}/{encodedToken})";
        //await emailManager.SendForgotPasswordEmail(model.Email, resetPasswordUrl);
    }

    public async Task ResetPassword(ResetPasswordModel model)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == model.UserId);
        ValidationHelper.MustExist(user);

        //var userBeforeChanges = user.ShallowCopy();

        model.Token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
        var result = await userManager.ResetPasswordAsync(user, model.Token, model.Password);

        ValidationHelper.CheckIdentityResult(result, new ErrorOverrideModel
        {
            TextToOverride = "Invalid token.",
            OverrideWith = "This reset password link has already been used. Please go back to Log In page and start the process again."
        });

        //user.LoginSettings.UnsuccessfulLoginAttempts = 0;
        await db.SaveChangesAsync();
    }

    public async Task ConfirmEmail(Guid userId, UserConfirmEmailModel model)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        ValidationHelper.MustExist(user);

        if (user.EmailConfirmed)
        {
            throw new ValidationException(ErrorCode.AlreadyConfirmedEmail);
        }

        if (user.EmailVerificationCode != model.EmailVerificationCode)
        {
            throw new ValidationException(ErrorCode.InvalidConfirmationCode);
        }

        var minutesVerificationCodeIsValid = MINUTES_VERIFICATION_CODE_IS_VALID;
        if (user.DateVerificationCodeSent.Value.AddMinutes(minutesVerificationCodeIsValid) < DateTime.UtcNow)
        {
            throw new ValidationException(ErrorCode.ConfirmationCodeExpired);
        }

        user.EmailConfirmed = true;

        await db.SaveChangesAsync();
    }

    public async Task<DateTime> ResendVerificationEmail(Guid userId)
    {
        //TODO: trebalo bi staviti na 3 max pokusaja
        var user = await userManager.FindByIdAsync(userId.ToString());
        ValidationHelper.MustExist(user);


        if (user.EmailConfirmed)
        {
            throw new ValidationException(ErrorCode.AlreadyConfirmedEmail);
        }

        await SendVerificationEmail(user);
        return user.DateVerificationCodeSent.Value.AddMinutes(MINUTES_VERIFICATION_CODE_IS_VALID);
    }

    public async Task ChangeVerificationEmail(Guid userId, ChangeEmailModel model)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        ValidationHelper.MustExist(user);

        var userWithSameEmail = await userManager.FindByEmailAsync(model.NewEmail);
        ValidationHelper.MustNotExist(userWithSameEmail);

        user.Email = model.NewEmail;
        user.UserName = model.NewEmail;
        await userManager.UpdateAsync(user);

        await SendVerificationEmail(user);
    }

    //TODO:
    //public async Task<AuthResponseModel> GoogleLogin(GoogleLoginModel model)
    //{
    //    try
    //    {
    //        var currentFirebaseInstance = FirebaseAdmin.FirebaseApp.GetInstance("[DEFAULT]");
    //        if (currentFirebaseInstance == null)

    //            FirebaseAdmin.FirebaseApp.Create(new FirebaseAdmin.AppOptions
    //            {
    //                Credential = GoogleCredential.FromJson(FIREBASE_CONFIG_JSON)
    //            });


    //        FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(model.IdToken);

    //        var userSocialLoginModel = new UserSocialLoginModel
    //        {
    //            SocialAccountType = SocialAccountType.Google,
    //            ExpiresIn = decodedToken.ExpirationTimeSeconds.ToString(),
    //            RefreshToken = model.RefreshToken,
    //            Email = decodedToken.Claims["email"].ToString(),
    //            DisplayName = decodedToken.Claims["name"].ToString(),
    //            SocialAccountUserId = decodedToken.Claims["user_id"].ToString(),
    //            ReferralCode = model.ReferralCode,
    //            InviteId = model.Inviteld,
    //            RegisterCode = model.RegisterCode
    //        };

    //        var loginResult = await SocialLogin(userSocialLoginModel);
    //        return loginResult;
    //    }
    //    catch (ValidationException ex) { throw ex; }
    //    catch (NotFoundException ex) { throw ex; }
    //    catch
    //    {
    //        throw new ValidationException(ErrorCode.InvalidGoogleAccount);
    //    };
    //}

    //private async Task<AuthResponseModel> SocialLogin(UserSocialLoginModel model)
    //{

    //}

    private async Task SendVerificationEmail(User user)
    {
        var verificationCode = Utilities.GenerateNumericCode(6);
        //await emailManager.SendVerificationEmail(user.Email, verificationCode);
        user.EmailVerificationCode = verificationCode;
        user.DateVerificationCodeSent = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
    }
}
