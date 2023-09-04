using GorgoDresses.Common.Helpers;
using GorgoDresses.Core;
using Microsoft.AspNetCore.Authorization;

namespace GorgoDresses.Api.Auth;

public class AdministratorUserHandler : AuthorizationHandler<AdministratorUserRequirement>
{
    private readonly UserActivityManager userActivityManager;

    public AdministratorUserHandler(UserActivityManager userActivityManager)
    {
        this.userActivityManager = userActivityManager;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdministratorUserRequirement requirement)
    {
        var isUserAdministrator = await CheckIfUserIsAdministrator(context);

        if (isUserAdministrator)
        {
            context.Succeed(requirement);
        } else
        {
            context.Fail();
        }
    }

    public async Task<bool> CheckIfUserIsAdministrator(AuthorizationHandlerContext context)
    {
        if (!AuthorizationHelper.TryParseUserId(context, out var userId))
        {
            return false;
        }

        var user = await userActivityManager.GetUser(userId);
        var userRole = await userActivityManager.GetUserRole(userId);

        return user != null && user.EmailConfirmed && userRole == UserRoleConstants.Administrator;
    }
}

public class AdministratorUserRequirement : IAuthorizationRequirement
{
}
