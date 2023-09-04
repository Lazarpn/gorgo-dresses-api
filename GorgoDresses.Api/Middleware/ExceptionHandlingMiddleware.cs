using GorgoDresses.Common.Enums;
using GorgoDresses.Common.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace GorgoDresses.Common.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate next;
    private readonly IWebHostEnvironment env;
    private readonly ILogger<ExceptionHandlingMiddleware> logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, IWebHostEnvironment env, ILogger<ExceptionHandlingMiddleware> logger)
    {
        this.next = next;
        this.env = env;
        this.logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        HttpStatusCode statusCode;

        List<ExceptionDetail> exceptionDetails = new List<ExceptionDetail>();

        if (exception is ValidationException)
        {
            statusCode = HttpStatusCode.BadRequest;
            exceptionDetails = (exception as ValidationException).Details;
        }
        else if (exception is AuthenticationException)
        {
            statusCode = HttpStatusCode.Unauthorized;
            exceptionDetails = (exception as AuthenticationException).Details;
        }
        else if (exception is AuthorizationException)
        {
            statusCode = HttpStatusCode.Forbidden;
            exceptionDetails = (exception as AuthorizationException).Details;
        }
        else if (exception is NotFoundException)
        {
            statusCode = HttpStatusCode.NotFound;
            exceptionDetails = (exception as NotFoundException).Details;
        }
        else
        {
            var isProduction = env.EnvironmentName.Equals("production", StringComparison.OrdinalIgnoreCase);
            statusCode = HttpStatusCode.InternalServerError;
            exceptionDetails.Add(new ExceptionDetail
            {
                ErrorCode = ErrorCode.InternalServerError,
                Params = new
                {
                    details = isProduction ? "A server error has occured." : exception.Message
                }
            });
        }

        var response = context.Response;
        response.ContentType = "application/json";
        response.StatusCode = (int)statusCode;

        logger.LogError(exception, exception.Message);

        var serializerSettings = new JsonSerializerSettings
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };
        serializerSettings.Converters.Add(new StringEnumConverter());

        var responseString = JsonConvert.SerializeObject(exceptionDetails, serializerSettings);

        await response.WriteAsync(responseString).ConfigureAwait(false);
    }
}
