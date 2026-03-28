using System.Text;
using auth_template.Configuration;
using auth_template.Entities;
using auth_template.Entities.Data;
using auth_template.Features.Admin.Services.Dashboard;
using auth_template.Features.Admin.Services.User;
using auth_template.Features.Auth.Configuration;
using auth_template.Features.Auth.Services;
using auth_template.Features.Auth.Utilities.Activity;
using auth_template.Features.Auth.Utilities.Permissions;
using auth_template.Features.Auth.Utilities.Tokens.Jwt;
using auth_template.Features.Auth.Utilities.Tokens.Refresh;
using auth_template.Features.Auth.Utilities.User;
using auth_template.Features.Email.Options;
using auth_template.Features.Email.Services;
using auth_template.Features.Email.Utilities.Client;
using auth_template.Features.Profile.Services.Profile;
using auth_template.Features.Profile.Utilities;
using auth_template.Middleware.DevicePinning;
using auth_template.Middleware.ErrorHandler;
using auth_template.Middleware.TokenRefresh;
using auth_template.Options;
using auth_template.Utilities;
using auth_template.Utilities.Audit;
using auth_template.Utilities.Security.Encryption;
using auth_template.Utilities.Security.Hashing;
using auth_template.Utilities.Security.Passwords;
using auth_template.Utilities.Security.Pepper;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;

var isRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
var builder = WebApplication.CreateBuilder(args);
var isDev = builder.Environment.IsDevelopment();


if (isDev)
{
    DotNetEnv.Env.Load(".env");
    builder.Configuration.AddEnvironmentVariables();
}

#region Configs
IConfigurationSection securityCfg = builder.Configuration.GetSection("Security");
IConfigurationSection databaseCfg = builder.Configuration.GetSection("Database");
IConfigurationSection emailCfg = builder.Configuration.GetSection("Email");
IConfigurationSection corsCfg = builder.Configuration.GetSection("Cors");

builder.Services.Configure<SecurityOptions>(securityCfg);
builder.Services.Configure<DatabaseOptions>(databaseCfg);
builder.Services.Configure<EmailOptions>(emailCfg);
builder.Services.Configure<CorsOptions>(corsCfg);


var corsOptions = corsCfg
    .Get<CorsOptions>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(corsOptions?.AllowedOrigins.Split(",") ?? ["http://localhost:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

string connectionString = "";
if (isDev)
{
    string? dbUser = databaseCfg["User"];
    string? dbPassword = databaseCfg["Password"];
    string? dbName = databaseCfg["Name"];

    connectionString = $"Host=localhost;Port=2345;Database={dbName};Username={dbUser};Password={dbPassword}";
}
else
{
    connectionString = databaseCfg["ConnString"];
}

#endregion

#region DB & Identity
builder.Services.AddLogging();
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseNpgsql(connectionString, opt => { opt.CommandTimeout(15); });
});

builder.Services.AddRateLimiter(opt =>
    {
        opt.OnRejected = RateLimitUtility.OnRateLimitRejection;
        opt.RegisterAllPolicies();
    }
);




builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
{
    options.Lockout.MaxFailedAccessAttempts = AuthConfiguration.AccessFailCountThreshold;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(AuthConfiguration.AccessFailLockoutDurationInMinutes);
}).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
builder.Services.Configure<CookiePolicyOptions>(opt =>
{
    opt.MinimumSameSitePolicy = SameSiteMode.None;
    opt.Secure = CookieSecurePolicy.Always;
    opt.HttpOnly = HttpOnlyPolicy.Always;
});
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.IdleTimeout = TimeSpan.FromMinutes(30);
    opt.Cookie.HttpOnly = true;
    opt.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.None
        : CookieSecurePolicy.Always;
    ;
    opt.Cookie.IsEssential = true;
});


builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressMapClientErrors = true;
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        var response = new
        {
            data = (object)null,
            message = "One or more validation errors have occurred",
            statusCode = 400,
            errors = errors
        };

        return new BadRequestObjectResult(response);
    };
});
builder.Services.Configure<ApiBehaviorOptions>(options => { options.SuppressModelStateInvalidFilter = true; });
#endregion

#region Utils

#region Transient

#endregion


#region Singletons

builder.Services.AddSingleton<IEncryptor, Encryptor>();
builder.Services.AddSingleton<IPepperProvider, PepperProvider>();
builder.Services.AddSingleton<IActivityBuffer, ActivityBuffer>();

#endregion

#region Jobs
builder.Services.AddHostedService<ActivityRegister>();
#endregion
#region Scoped

builder.Services.AddScoped<IEmailSenderClient, EmailSenderClient>();
builder.Services.AddScoped<IAuditUtility, AuditUtility>();
builder.Services.AddScoped<ITokenUtility, TokenUtility>();
builder.Services.AddScoped<IPermissionUtility, PermissionUtility>();
builder.Services.AddScoped<IRefreshGenerator, RefreshGenerator>();
builder.Services.AddScoped<IGeneralHasher, GeneralHasher>();
builder.Services.AddScoped<IJwtGenerator, JwtGenerator>();
builder.Services.AddScoped<IPasswordVerifier, PasswordVerifier>();
builder.Services.AddScoped<IUserUtils, UserUtils>();
builder.Services.AddScoped<IProfileUtility, ProfileUtility>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();

#endregion
#endregion

#region Validation

builder.Services.AddValidatorsFromAssemblyContaining<AppDbContext>();
builder.Services.AddFluentValidationAutoValidation(opt =>
{
    opt.OverrideDefaultResultFactoryWith<ResultFactory>();
    opt.EnableBodyBindingSourceAutomaticValidation = true;
});

#endregion

#region Services

//Auth
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
//Profile
builder.Services.AddScoped<IProfileService, ProfileService>();

#endregion

#region Network & Auth


if (isRunningInContainer)
{
    builder.WebHost.UseUrls("http://0.0.0.0:5377");
}
else
{
    builder.WebHost.UseUrls("https://0.0.0.0:5377");
}

builder.Services.AddHttpClient();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.HttpContext.Items.TryGetValue("JwtToken", out var refreshedToken))
                {
                    context.Token = refreshedToken as string;
                    return Task.CompletedTask;
                }

                var accessToken = context.Request.Cookies["X-Access-Token"];

                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ClockSkew = TimeSpan.Zero,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = securityCfg["IssuerAudiencePair"].Split(";")[0] ??
                          throw new ApplicationException("Provide an issuer in secrets"),
            ValidAudience = securityCfg["IssuerAudiencePair"].Split(";")[1] ??
                            throw new ApplicationException("Provide an audience in secrets"),
            IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityCfg.GetSection("Keys")["JwtSigningKey"])) ??
                throw new ApplicationException("Provide a signing key in secrets"),
        };
    });

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

#endregion

var app = builder.Build();

#region Dev Migrations
if (isDev)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var ctx = services.GetRequiredService<AppDbContext>();
        await ctx.Database.EnsureCreatedAsync();
        await ctx.Database.MigrateAsync();
        Console.WriteLine("DB migration and seeding successful.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration failed");
    }
}
else
{
    Console.WriteLine("Skipping inline migration to speed up startup.");
}
#endregion

#region App Config
app.UseForwardedHeaders();
app.UseRouting();
app.UseMiddleware<TokenRefreshMiddleware>();
app.UseCors("FrontendPolicy");
app.UseCookiePolicy();
app.UseAuthentication();
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
else
{
    app.UseHsts();
}

app.UseSession();
#endregion

#region Middlewares
app.UseMiddleware<ErrorHandlerMiddleware>();
app.UseMiddleware<DeviceIdentifierPinningMiddleware>();
#endregion

#region API
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
#endregion

app.Run();