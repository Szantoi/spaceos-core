using Ardalis.Result;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SpaceOS.Modules.Sales.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs FluentValidation validators before the handler.
/// Returns <see cref="Result"/> Invalid if any validators fail. BE-S-08.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>> log)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any())
            return await next().ConfigureAwait(false);

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, ct)))
            .ConfigureAwait(false))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next().ConfigureAwait(false);

        log.LogDebug("Validation failed for {RequestType} with {ErrorCount} error(s)",
            typeof(TRequest).Name, failures.Count);

        // Build Result.Invalid with all validation errors
        var errors = failures.Select(f => new ValidationError(f.ErrorMessage)).ToArray();

        // Return Result.Invalid cast to TResponse if possible (works for Result and Result<T>)
        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var method = typeof(Result).GetMethod(nameof(Result.Invalid),
                new[] { typeof(IEnumerable<ValidationError>) })!;
            var generic = method.MakeGenericMethod(typeof(TResponse).GetGenericArguments()[0]);
            return (TResponse)generic.Invoke(null, new object[] { (IEnumerable<ValidationError>)errors })!;
        }

        if (typeof(TResponse) == typeof(Result))
        {
            object resultObj = Result.Invalid(errors);
            return (TResponse)resultObj;
        }

        throw new ValidationException(failures);
    }
}
