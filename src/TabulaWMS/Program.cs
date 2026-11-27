using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TabulaWMS.Services;
using TabulaWMS.Models;
using TabulaWMS.Data;
using TabulaWMS.Middleware;
using TabulaWMS.Constants;
using Scalar.AspNetCore;
using Microsoft.OpenApi;



var runTenantMigrations = args.Contains("--migrate-tenants", StringComparer.OrdinalIgnoreCase);
var builderArgs = args
    .Where(argument => !string.Equals(argument, "--migrate-tenants", StringComparison.OrdinalIgnoreCase))
    .ToArray();
var builder = WebApplication.CreateBuilder(builderArgs);

builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<TenantMigrationRunner>();

builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {

        document.Info.Title = "TabulaWMS API";
        document.Info.Version = "v1.0.0";

        // Initialize components if null
        document.Components ??= new OpenApiComponents();

        // Register the Bearer security scheme
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter 'Bearer' [space] followed by your JWT token (e.g. Bearer eyJhbGci...)"
            }
        };

        // Apply the security requirement globally to all endpoints
        var requirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        };

        foreach (var operation in document.Paths.Values
                    .Where(path => path.Operations != null)
                    .SelectMany(path => path.Operations!.Values))
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(requirement);
        }

        return Task.CompletedTask;
    });
});

builder.Services.AddScoped<TenantCommandInterceptor>();

// Global / Public context (no interceptor, targets public schema)
builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Tenant-scoped context
builder.Services.AddDbContext<TabulaContext>((sp, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .AddInterceptors(sp.GetRequiredService<TenantCommandInterceptor>());
});
// Register User
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;

    // Account lockout settings
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<TabulaContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<AuthService>();

var jwtKey = builder.Configuration["JwtSettings:Key"];
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true, // Validate the token's expiration
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.GetAllPermissions())
    {
        options.AddPolicy(permission, policy =>
            policy.RequireAuthenticatedUser().RequireClaim("permission", permission));
    }
});

// 1. Add rate limiting services
builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("login-limiter", opt =>
    {
        opt.PermitLimit = 5;                     // Max 5 requests
        opt.Window = TimeSpan.FromMinutes(5);    // Per 5-minute window
        opt.SegmentsPerWindow = 5;
        opt.QueueLimit = 0;                      // Reject when limit is hit instead of queuing
    });

    // Response when rate limit is exceeded (HTTP 429)
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"message\": \"Too many login attempts. Please try again after 5 minutes.\"}",
            cancellationToken);
    };
});

var app = builder.Build();

if (runTenantMigrations)
{
    using var scope = app.Services.CreateScope();
    var migrationRunner = scope.ServiceProvider.GetRequiredService<TenantMigrationRunner>();
    Environment.ExitCode = await migrationRunner.RunAsync();
    return;
}

// seed SuperAdmin user on startup
using (var scope = app.Services.CreateScope())
{
    if (app.Configuration.GetValue<bool>("TestDatabase:ApplyMigrationsOnStartup"))
    {
        var db = scope.ServiceProvider.GetRequiredService<TabulaContext>();
        await db.Database.MigrateAsync();
    }

    await DbInitializer.SeedSuperAdminAsync(scope.ServiceProvider, app.Configuration);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

app.UseRateLimiter();
app.MapControllers();

app.Run();

public partial class Program { }