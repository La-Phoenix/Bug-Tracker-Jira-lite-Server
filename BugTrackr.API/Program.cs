//using BugTrackr.Infrastructure.Persistence;
//using FluentValidation.AspNetCore;
//using FluentValidation;
//using MediatR;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.OpenApi.Models;
//using BugTrackr.Application;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.IdentityModel.Tokens;
//using System.Text;
//using BugTrackr.Infrastructure.Auth;
//using BugTrackr.Application.Services;
//using BugTrackr.Infrastructure.Persistence.Repositories;
//using BugTrackr.API.Middleware;
//using System.Text.Json;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

//// Register MediatR from Application Layer
//builder.Services.AddMediatR(cfg =>
//    cfg.RegisterServicesFromAssembly(typeof(IApplicationMarker).Assembly));

//// Register FluentValidation from Application Layer
//builder.Services.AddValidatorsFromAssembly(typeof(IApplicationMarker).Assembly);

//// Enables FluentValidation automatic validation integration
//builder.Services.AddFluentValidationAutoValidation();  // Server-side validation
//builder.Services.AddFluentValidationClientsideAdapters(); // Client-side adapters if needed

//// Register FluentValidation (Automatic and Manual)
//builder.Services.AddFluentValidationServices(); // Custom extension method for validation services

//// Register AutoMapper from Application Layer
//builder.Services.AddAutoMapper(typeof(IApplicationMarker).Assembly);



//// ... other services like controllers, db, swagger, etc.
//builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
//builder.Services.AddControllers();
//builder.Services.AddDbContext<BugTrackrDbContext>(options =>
//    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

//builder.Services.AddScoped<IAuthService, AuthService>();
//builder.Services.AddScoped<IJwtService, JwtService>();

//builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
////builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
////    .AddJwtBearer(options =>
////    {
////        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
////        options.TokenValidationParameters = new TokenValidationParameters
////        {
////            ValidateIssuer = true,
////            ValidIssuer = jwtSettings["Issuer"],
////            ValidateAudience = true,
////            ValidAudience = jwtSettings["Audience"],
////            ValidateLifetime = true,
////            ValidateIssuerSigningKey = true,
////            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
////        };
////    });

//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultSignInScheme = "External"; // ✅ specify scheme for external logins
//})
//.AddCookie("External", options =>
//{
//    options.Cookie.SameSite = SameSiteMode.Lax;
//    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
//    options.Cookie.HttpOnly = true;
//})
//.AddJwtBearer(options =>
//{
//    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,
//        ValidIssuer = jwtSettings["Issuer"],
//        ValidAudience = jwtSettings["Audience"],
//        IssuerSigningKey = new SymmetricSecurityKey(
//            Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
//    };
//})
//.AddGoogle(googleOptions =>
//{
//    googleOptions.SignInScheme = "External"; // ✅ tell Google to use external cookie
//    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
//    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
//    googleOptions.CallbackPath = "/api/auth/external/callback";
//    googleOptions.Scope.Add("email");
//    googleOptions.Scope.Add("profile");
//})
//.AddGitHub(githubOptions =>
//{
//    githubOptions.SignInScheme = "External"; // ✅ tell GitHub to use external cookie
//    githubOptions.ClientId = builder.Configuration["Authentication:GitHub:ClientId"];
//    githubOptions.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];
//    githubOptions.CallbackPath = "/api/auth/external/callback";
//    githubOptions.Scope.Add("user:email");
//});


//builder.Services.AddAuthorization();

//builder.Services.AddEndpointsApiExplorer();

//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BugTrackr API", Version = "v1" });

//    var securityScheme = new OpenApiSecurityScheme
//    {
//        Name = "Authorization",
//        Type = SecuritySchemeType.Http,
//        Scheme = "bearer",
//        BearerFormat = "JWT",
//        In = ParameterLocation.Header,
//        Description = "JWT Authorization header using the Bearer scheme.",
//        Reference = new OpenApiReference
//        {
//            Type = ReferenceType.SecurityScheme,
//            Id = "Bearer"
//        }
//    };

//    c.AddSecurityDefinition("Bearer", securityScheme);
//    c.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        { securityScheme, new[] { "Bearer" } }
//    });
//});

//// CORS policy to allow frontend development server
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowFrontend", policy =>
//    {
//        policy.WithOrigins(
//             "http://localhost:5173",         // frontend local dev
//             "http://localhost:8080", // Swagger on port 8080
//             "http://localhost:5215",         // Swagger UI (from launchSettings)
//             "https://bug-tracker-jira-lite-client.vercel.app",
//             "https://localhost:58301",
//            "https://localhost:5215",
//            "https://localhost:7275"
//         )
//         .AllowAnyHeader()
//         .AllowAnyMethod()
//         .AllowCredentials();
//    });
//});
//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();

//var app = builder.Build();


//// Handle 404 errors globally (optional, after exception middleware)
//app.Use(async (context, next) =>
//{
//    await next();

//    if (context.Response.StatusCode == StatusCodes.Status404NotFound && !context.Response.HasStarted)
//    {
//        context.Response.ContentType = "application/json";
//        var response = new
//        {
//            status = 404,
//            title = "The requested resource was not found"
//        };

//        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
//    }
//});

//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<BugTrackrDbContext>();
//    db.Database.Migrate();
//}

//// Development 
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();

//    var logger = app.Services.GetRequiredService<ILogger<Program>>();
//    logger.LogInformation("Swagger is running at /swagger");
//}

//// Global exception handler — must come first!
//app.UseMiddleware<ExceptionHandlingMiddleware>();
////  CORS, HTTPS, and middleware pipeline
//app.UseCors("AllowFrontend");
//app.MapControllers();
//app.UseAuthorization();
//app.UseAuthentication();
////app.UseHttpsRedirection();
//app.Run();

//// Work around when program class is needed 
//public partial class Program;

using BugTrackr.Infrastructure.Persistence;
using FluentValidation.AspNetCore;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using BugTrackr.Application;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BugTrackr.Infrastructure.Auth;
using BugTrackr.Application.Services;
using BugTrackr.Infrastructure.Persistence.Repositories;
using BugTrackr.API.Middleware;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using BugTrackr.Application.Services.Auth;
using BugTrackr.Application.Services.JWT;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Register MediatR from Application Layer
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(IApplicationMarker).Assembly));

// Register FluentValidation from Application Layer
builder.Services.AddValidatorsFromAssembly(typeof(IApplicationMarker).Assembly);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddFluentValidationServices();

// Register AutoMapper from Application Layer
builder.Services.AddAutoMapper(typeof(IApplicationMarker).Assembly);

// Other services
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddControllers();

// CONDITIONAL DATABASE CONFIGURATION
//if (builder.Environment.IsEnvironment("Testing"))
//{
//    builder.Services.AddDbContext<BugTrackrDbContext>(options =>
//        options.UseInMemoryDatabase("TestDatabase"));
//}
//else
//{
    builder.Services.AddDbContext<BugTrackrDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
//}

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// Configure forwarded headers for Render deployment
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Configure Data Protection FIRST - before authentication
var dataProtectionBuilder = builder.Services.AddDataProtection()
    .SetApplicationName("BugTrackr");

if (builder.Environment.IsProduction())
{
    var keysDir = Path.Combine(Path.GetTempPath(), "bugtrackr-keys");
    Directory.CreateDirectory(keysDir);
    dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysDir))
        .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
}
else
{
    var keysDir = Path.Combine(Directory.GetCurrentDirectory(), "keys");
    Directory.CreateDirectory(keysDir);
    dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysDir))
        .SetDefaultKeyLifetime(TimeSpan.FromDays(30));
}

// Get OAuth configuration
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var githubClientId = builder.Configuration["Authentication:GitHub:ClientId"];
var githubClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];

// Configure authentication - environment aware
if (builder.Environment.IsEnvironment("Testing"))
{
    // Minimal auth config for testing (JWT only)
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
        };
    });
}
else
{
    // Get the data protection provider for OAuth state validation
    var tempServiceProvider = builder.Services.BuildServiceProvider();
    var dataProtectionProvider = tempServiceProvider.GetRequiredService<IDataProtectionProvider>();

    // Full authentication configuration for development/production
    var authBuilder = builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = "External";
    });

    // External cookie authentication
    authBuilder.AddCookie("External", options =>
    {
        options.Cookie.Name = "BugTrackr.External";
        options.Cookie.SameSite = SameSiteMode.None; // Required for OAuth cross-site redirects
        options.Cookie.SecurePolicy = builder.Environment.IsProduction()
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.LoginPath = "/api/auth/login";

        // Production-ready event handlers
        options.Events.OnValidatePrincipal = async context =>
        {
            if (context.Principal?.Identity?.IsAuthenticated != true)
            {
                context.RejectPrincipal();
            }
        };
    });

    // JWT Bearer authentication
    authBuilder.AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]!)),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

    // Google OAuth - conditionally register
    if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
    {
        authBuilder.AddGoogle(googleOptions =>
        {
            googleOptions.SignInScheme = "External";
            googleOptions.ClientId = googleClientId;
            googleOptions.ClientSecret = googleClientSecret;
            googleOptions.CallbackPath = "/api/auth/external/callback";
            googleOptions.SaveTokens = true;

            // Clear and set specific scopes
            googleOptions.Scope.Clear();
            googleOptions.Scope.Add("openid");
            googleOptions.Scope.Add("profile");
            googleOptions.Scope.Add("email");

            // Production-ready cookie settings
            googleOptions.CorrelationCookie.Name = "BugTrackr.Google.Correlation";
            googleOptions.CorrelationCookie.SameSite = SameSiteMode.None;
            googleOptions.CorrelationCookie.SecurePolicy = builder.Environment.IsProduction()
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            googleOptions.CorrelationCookie.HttpOnly = true;
            googleOptions.CorrelationCookie.IsEssential = true;
            googleOptions.CorrelationCookie.Expiration = TimeSpan.FromMinutes(15);

            // Configure state validation with shared data protection provider
            googleOptions.StateDataFormat = new PropertiesDataFormat(
                dataProtectionProvider.CreateProtector("Google-OAuth-State-Protection")
            );

            // Event handlers
            googleOptions.Events.OnCreatingTicket = async context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var email = context.Principal?.FindFirst(ClaimTypes.Email)?.Value ??
                           context.Principal?.FindFirst("email")?.Value ?? "Unknown";
                logger.LogInformation("Google OAuth successful for user: {Email}", email);
            };

            googleOptions.Events.OnRemoteFailure = async context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError("Google OAuth failed: {Error} - {ErrorDescription}",
                    context.Failure?.Message, context.Failure?.InnerException?.Message);

                var returnUrl = context.Properties?.Items["ReturnUrl"] ??
                    (builder.Environment.IsProduction()
                        ? "https://bug-tracker-jira-lite-client.vercel.app"
                        : "http://localhost:5173");

                context.Response.Redirect($"{returnUrl}?error=oauth_failed&details={Uri.EscapeDataString(context.Failure?.Message ?? "Unknown error")}");
                context.HandleResponse();
            };

            googleOptions.Events.OnTicketReceived = async context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogDebug("Google OAuth ticket received for scheme: {Scheme}", context.Scheme.Name);
            };
        });
    }

    // GitHub OAuth - conditionally register  
    if (!string.IsNullOrEmpty(githubClientId) && !string.IsNullOrEmpty(githubClientSecret))
    {
        authBuilder.AddGitHub(githubOptions =>
        {
            githubOptions.SignInScheme = "External";
            githubOptions.ClientId = githubClientId;
            githubOptions.ClientSecret = githubClientSecret;
            githubOptions.CallbackPath = "/api/auth/external/callback";
            githubOptions.SaveTokens = true;

            // Clear and set specific scopes
            githubOptions.Scope.Clear();
            githubOptions.Scope.Add("user:email");
            githubOptions.Scope.Add("read:user");

            // Production-ready cookie settings
            githubOptions.CorrelationCookie.Name = "BugTrackr.GitHub.Correlation";
            githubOptions.CorrelationCookie.SameSite = SameSiteMode.None;
            githubOptions.CorrelationCookie.SecurePolicy = builder.Environment.IsProduction()
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            githubOptions.CorrelationCookie.HttpOnly = true;
            githubOptions.CorrelationCookie.IsEssential = true;
            githubOptions.CorrelationCookie.Expiration = TimeSpan.FromMinutes(15);

            // Configure state validation with shared data protection provider
            githubOptions.StateDataFormat = new PropertiesDataFormat(
                dataProtectionProvider.CreateProtector("GitHub-OAuth-State-Protection")
            );

            // Event handlers
            githubOptions.Events.OnCreatingTicket = async context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var login = context.Principal?.FindFirst("login")?.Value ??
                           context.Principal?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                logger.LogInformation("GitHub OAuth successful for user: {Login}", login);
            };

            githubOptions.Events.OnRemoteFailure = async context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError("GitHub OAuth failed: {Error} - {ErrorDescription}",
                    context.Failure?.Message, context.Failure?.InnerException?.Message);

                var returnUrl = context.Properties?.Items["ReturnUrl"] ??
                    (builder.Environment.IsProduction()
                        ? "https://bug-tracker-jira-lite-client.vercel.app"
                        : "http://localhost:5173");

                context.Response.Redirect($"{returnUrl}?error=oauth_failed&details={Uri.EscapeDataString(context.Failure?.Message ?? "Unknown error")}");
                context.HandleResponse();
            };
        });
    }

    // Dispose temporary service provider
    tempServiceProvider.Dispose();
}

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();

// Swagger configuration - only in non-production
if (!builder.Environment.IsProduction())
{
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "BugTrackr API",
            Version = "v1",
            Description = "Bug tracking and project management API"
        });

        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme.",
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        };

        c.AddSecurityDefinition("Bearer", securityScheme);
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { securityScheme, new[] { "Bearer" } }
        });
    });
}

// CORS configuration - environment specific
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsProduction())
        {
            policy.WithOrigins(
                "https://bug-tracker-jira-lite-client.vercel.app"
            );
        }
        else
        {
            policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "http://localhost:8080",
                "http://localhost:5215",
                "https://localhost:5215",
                "https://localhost:7275"
            );
        }

        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

if (builder.Environment.IsProduction())
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

var app = builder.Build();

// Use forwarded headers - MUST be first middleware in production
if (app.Environment.IsProduction())
{
    app.UseForwardedHeaders();
}

// Global 404 handler
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == StatusCodes.Status404NotFound && !context.Response.HasStarted)
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = 404,
            title = "The requested resource was not found"
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});

// Database initialization
if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<BugTrackrDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await BugTrackr.Infrastructure.Persistence.DbInitializer.InitializeAsync(db, logger);
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BugTrackr API v1");
        c.RoutePrefix = "swagger";
    });

    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Swagger is running at /swagger");
}

// Security headers for production
if (app.Environment.IsProduction())
{
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
        await next();
    });

    app.UseHttpsRedirection();
}

// Middleware pipeline - ORDER IS CRITICAL
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
