using GorgoDresses.Core;
using Microsoft.AspNetCore.Authorization;

namespace GorgoDresses.Api.Auth;

public class NotConfirmedEmailHandler : AuthorizationHandler<NotConfirmedEmailRequirement>
{
    private readonly UserActivityManager userActivityManager;

    public NotConfirmedEmailHandler(UserActivityManager userActivityManager)
    {
        this.userActivityManager = userActivityManager;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, NotConfirmedEmailRequirement requirement)
    {
        var emailNotConfirmed = await CheckEmailNotConfirmed(context);

        if (emailNotConfirmed)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }

    public async Task<bool> CheckEmailNotConfirmed(AuthorizationHandlerContext context)
    {
        if (!AuthorizationHelper.TryParseUserId(context, out var userId))
        {
            return false;
        }

        var user = await userActivityManager.GetUser(userId);

        return user != null && !user.EmailConfirmed;
    }
}

public class NotConfirmedEmailRequirement: IAuthorizationRequirement
{
}
