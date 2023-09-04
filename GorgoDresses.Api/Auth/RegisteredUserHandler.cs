using GorgoDresses.Core;
using Microsoft.AspNetCore.Authorization;

namespace GorgoDresses.Api.Auth;

public class RegisteredUserHandler : AuthorizationHandler<RegisteredUserRequirement>
{
    private readonly UserActivityManager userActivityManager;

    public RegisteredUserHandler(UserActivityManager userActivityManager)
    {
        this.userActivityManager = userActivityManager;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RegisteredUserRequirement requirement)
    {
        var isUserRegistered = await CheckIfUserIsRegistered(context);

        if (isUserRegistered)
        {
            context.Succeed(requirement);
        } else
        {
            context.Fail();
        }
    }

    public async Task<bool> CheckIfUserIsRegistered(AuthorizationHandlerContext context)
    {
        if (!AuthorizationHelper.TryParseUserId(context, out var userId))
        {
            return false;
        }

        var user = await userActivityManager.GetUser(userId);

        return user != null;
    }
}

public class RegisteredUserRequirement : IAuthorizationRequirement
{
}
