using GorgoDresses.Core;
using Microsoft.AspNetCore.Authorization;

namespace GorgoDresses.Api.Auth;

public class EmailConfirmedHandler : AuthorizationHandler<EmailConfirmedRequirement>
{
    private readonly UserActivityManager userActivityManager;

    public EmailConfirmedHandler(UserActivityManager userActivityManager)
    {
        this.userActivityManager = userActivityManager;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, EmailConfirmedRequirement requirement)
    {
        var hasEmailConfirmed = await CheckEmailConfirmed(context);

        if (hasEmailConfirmed)
        {
            context.Succeed(requirement); 
        } else
        {
            context.Fail();
        }
    }

    public async Task<bool> CheckEmailConfirmed(AuthorizationHandlerContext context)
    {
        if(!AuthorizationHelper.TryParseUserId(context, out var userId))
        {
            return false;
        }

        var user = await userActivityManager.GetUser(userId);

        return user != null && user.EmailConfirmed;
    }
}

public class EmailConfirmedRequirement : IAuthorizationRequirement
{
}
