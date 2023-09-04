using GorgoDresses.Common.Helpers;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace GorgoDresses.Api.Middleware;

public class SessionManagementMiddleware
{
    private readonly RequestDelegate next;
    private readonly JwtHelper authHelper;

    public SessionManagementMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        this.next = next;
        authHelper = new JwtHelper(configuration);
    }

    public async Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Request.Headers.TryGetValue(HeaderNames.Authorization, out StringValues authorizationHeader);
            var authorizationHeaderString = authorizationHeader.ToString();

            if (!string.IsNullOrWhiteSpace(authorizationHeaderString))
            {
                var skipTokenRefreshExists = context.Request.Query.ContainsKey("skipTokenRefresh");

                if (!skipTokenRefreshExists)
                {
                    var tokenValue = authorizationHeaderString.Replace("Bearer", string.Empty, StringComparison.InvariantCultureIgnoreCase).Trim();

                    var isTokenValid = authHelper.VerifyToken(tokenValue);

                    if (isTokenValid)
                    {
                        var newToken = authHelper.RegenerateJwtToken(tokenValue);
                        context.Response.Headers.Add("refreshed-token", newToken);
                    }
                }
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}
