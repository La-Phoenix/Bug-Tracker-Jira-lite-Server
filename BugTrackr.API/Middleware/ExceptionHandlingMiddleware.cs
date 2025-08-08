using BugTrackr.Application.Common;
using BugTrackr.Application.Exceptions;
using FluentValidation;
using System.Text.Json;

namespace BugTrackr.API.Middleware;
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, IWebHostEnvironment env, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _env = env;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";

            //var errors = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
            var errors = ex.Errors.Select(e => e.ErrorMessage );

            var response = ApiResponse<string>.Failure("Validation Failed", 400, errors);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (AppException ex)
        {
            _logger.LogError(ex, "Caught AppException in middleware"); // ✅ CONFIRM
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<string>.Failure(ex.Message, ex.StatusCode);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<string>.Failure("An unexpected error occurred.", 500);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}