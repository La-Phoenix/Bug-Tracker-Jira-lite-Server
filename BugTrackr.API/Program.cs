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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register MediatR from Application Layer
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(IApplicationMarker).Assembly));

// Register FluentValidation from Application Layer
builder.Services.AddValidatorsFromAssembly(typeof(IApplicationMarker).Assembly);

// Enables FluentValidation automatic validation integration
builder.Services.AddFluentValidationAutoValidation();  // Server-side validation
builder.Services.AddFluentValidationClientsideAdapters(); // Client-side adapters if needed

// Register FluentValidation (Automatic and Manual)
builder.Services.AddFluentValidationServices(); // Custom extension method for validation services

// Register AutoMapper from Application Layer
builder.Services.AddAutoMapper(typeof(IApplicationMarker).Assembly);



// ... other services like controllers, db, swagger, etc.
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddControllers();
builder.Services.AddDbContext<BugTrackrDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BugTrackr API", Version = "v1" });

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

// CORS policy to allow frontend development server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
             "http://localhost:5173",         // frontend local dev
             "http://localhost:8080", // Swagger on port 8080
             "http://localhost:5215",         // Swagger UI (from launchSettings)
             "https://localhost:58301",
            "https://localhost:5215",
            "https://localhost:7275"
         )
         .AllowAnyHeader()
         .AllowAnyMethod();
    });
});
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// Global exception handler — must come first!
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Handle 404 errors globally (optional, after exception middleware)
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BugTrackrDbContext>();
    db.Database.Migrate();
}

// Development 
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Swagger is running at /swagger");
}

//  CORS, HTTPS, and middleware pipeline
app.UseCors("AllowFrontend");
app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();
//app.UseHttpsRedirection();
app.Run();

// Work around when program class is needed 
public partial class Program;