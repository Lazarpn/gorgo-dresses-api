using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace GorgoDresses.Api.Auth;

public static class AuthorizationHelper
{
    public static bool TryParseUserId(AuthorizationHandlerContext context, out Guid userId)
    {
        var nameClaim = context.User.FindFirst(ClaimTypes.Name);

        if (nameClaim == null)
        {
            userId = default;
            return false;
        }

        _ = Guid.TryParse(nameClaim.Value, out userId);

        if(userId == Guid.Empty)
        {
            return false;
        }

        return true;
    }
}
