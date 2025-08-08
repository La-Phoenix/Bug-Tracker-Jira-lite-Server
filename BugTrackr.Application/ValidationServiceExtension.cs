using BugTrackr.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace BugTrackr.Application;
public static class ValidationServiceExtensions
{
    public static IServiceCollection AddFluentValidationServices(this IServiceCollection services)
    {
        // Register all validators from the Application layer
        services.AddValidatorsFromAssembly(typeof(IApplicationMarker).Assembly);

        // Register MediatR validation behavior globally
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}