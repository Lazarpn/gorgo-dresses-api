using GorgoDresses.Core;
using GorgoDresses.Api.ActionResults;
using GorgoDresses.Api.Helpers;
using GorgoDresses.Api.Middleware;
using GorgoDresses.Common.Middleware;
using GorgoDresses.Data;
using GorgoDresses.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;
using NLog.Web;
using System.Globalization;
using System.Reflection;
using System.Text;

Logger logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("App: Starting Application");

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    logger.Info("App: Configuring identity");
    builder.Services.AddIdentity<User, IdentityRole<Guid>>()
        .AddTokenProvider<DataProtectorTokenProvider<User>>(builder.Configuration["jwtIssuer"])
        .AddEntityFrameworkStores<GorgoDressesDbContext>()
        .AddDefaultTokenProviders();

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["jwtIssuer"],
            ValidateIssuer = true,
            ValidAudience = builder.Configuration["jwtAudience"],
            ValidateAudience = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["jwtKey"])),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true
        };
    });

    logger.Info("App: Configuring services");
    builder.Services.AddScoped<AccountManager>();
    builder.Services.AddScoped<UserActivityManager>();
    builder.Services.AddScoped<DressManager>();



    logger.Info("App: Configuring forwarded headers");
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    });

    logger.Info("App: Configuring CORS");
    var allowedDomains = builder.Configuration["allowedDomains"].Split(',');

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy",
            policyBuilder =>
            {
                policyBuilder
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .WithExposedHeaders("refreshed-token")
                    .AllowCredentials()
                    .WithOrigins(allowedDomains);
                policyBuilder.SetPreflightMaxAge(TimeSpan.FromSeconds(600));
            });
    });

    logger.Info("App: Configuring AutoMapper");
    builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

    logger.Info("App: Configuring DB Connections");
    builder.Services.AddDbContext<GorgoDressesDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("GorgoDressesDb")));

    logger.Info("App: Configuring web api / controllers");
    builder.Services.AddMemoryCache();

    builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
        options.SerializerSettings.Culture = new CultureInfo("en-GB");
        options.SerializerSettings.Converters.Add(new StringEnumConverter());
    });

    logger.Info("App: Configuring form options");
    builder.Services.Configure<FormOptions>(options =>
    {
        options.ValueLengthLimit = 52428800; // 50MB
        options.MultipartBodyLengthLimit = 52428800; // 50MB
    });

    logger.Info("App: Configuring validations");
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = _ => new ValidationProblemDetailsResult();
    });

    logger.Info("App: Configuring NLog DI");
    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    builder.Host.UseNLog();

    logger.Info("App: Configuring Swagger");
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "CaloriesTracking API", Version = "v1" });
        options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = "Standard Authorization header using the Bearer scheme. Note: Keyword 'bearer' will be automatically prepended.",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "Bearer"
        });
        options.OperationFilter<SecurityRequirementsOperationFilter>();
        options.DescribeAllParametersInCamelCase();

        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        options.IncludeXmlComments(xmlPath);
    });

    WebApplication app = builder.Build();

    logger.Info("App: Middleware pipeline start");
    if (builder.Environment.IsProduction())
    {
        app.UseHsts();
    }

    var cultureInfo = new CultureInfo("en-GB");
    CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
    CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

    app.UseStatusCodePages();

    app.UseStaticFiles();

    app.UseRouting();
    app.UseCors("CorsPolicy");

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<SessionManagementMiddleware>();

    bool shouldShowSwagger = bool.Parse(builder.Configuration["showSwagger"]);
    if (shouldShowSwagger)
    {
        app.UseSwagger();
        app.UseSwaggerUI(uiOptions =>
        {
            uiOptions.SwaggerEndpoint("/swagger/v1/swagger.json", "CalorieseTracking API V1");
            uiOptions.InjectStylesheet("/swagger-ui/styles.css");
        });
    }

    //StudyStreamDbContext.SeedData(dbContext, userManager, roleManager).Wait();

    if (builder.Environment.IsProduction())
    {
        app.UseHttpsRedirection();
    }

    app.MapControllers().RequireCors("CorsPolicy");

    logger.Info("App: Middleware pipeline end");

    app.Run();
    logger.Info("App: Running the Application");
}
catch (Exception exception)
{
    // NLog: catch setup errors
    logger.Error(exception, "App: Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    NLog.LogManager.Shutdown();
}
