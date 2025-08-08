using BugTrackr.Application.Common.Helpers;
using FluentValidation;
using MediatR;

namespace BugTrackr.Application.Common.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : ApiResponse, new()
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (request is ISkipFluentValidation)
                return await next();

            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                var validationResults = await Task.WhenAll(
                    _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

                var failures = validationResults
                    .SelectMany(r => r.Errors)
                    .Where(f => f != null)
                    .ToList();

                if (failures.Count != 0)
                {
                    var response = new TResponse
                    {
                        StatusCode = 400,
                        Message = "Validation failed",
                        Errors = failures.Select(f => f.ErrorMessage).ToList(),
                        Success = false
                    };

                    return response;
                }
            }

            return await next();
        }
    }


}
