using MediatR;
using FluentValidation;
using Ardalis.Result;
using System.Collections.Concurrent;
using System.Reflection;

namespace SpaceOS.Modules.Joinery.Application.Common.Behaviors;

/// <summary>
/// A MediatR pipeline behavior that executes all registered validators for a request.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // Caches one delegate per Result<T> type so reflection runs only once per closed type.
    private static readonly ConcurrentDictionary<Type, Func<List<ValidationError>, object>> _invalidFactories = new();

    private readonly IReadOnlyCollection<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        ArgumentNullException.ThrowIfNull(validators);
        _validators = validators.ToList().AsReadOnly();
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (_validators.Count == 0)
        {
            return await next().ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, ct))).ConfigureAwait(false);
        var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

        if (failures.Count != 0)
        {
            var errors = failures.Select(f => new ValidationError
            {
                Identifier = f.PropertyName,
                ErrorMessage = f.ErrorMessage
            }).ToList();

            return BuildInvalidResult(errors, failures);
        }

        return await next().ConfigureAwait(false);
    }

    private static TResponse BuildInvalidResult(List<ValidationError> errors, List<FluentValidation.Results.ValidationFailure> failures)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
            return (TResponse)(object)Result.Invalid(errors);

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            // Reflection runs exactly once per closed Result<T> type; subsequent calls use the cached delegate.
            var factory = _invalidFactories.GetOrAdd(responseType, static t =>
            {
                // Result<T> inherits the static Invalid(IEnumerable<ValidationError>) from the base Result class.
                var method = typeof(Result).GetMethod(
                    nameof(Result.Invalid),
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: [typeof(IEnumerable<ValidationError>)],
                    modifiers: null)!;

                // The implicit conversion from Result to Result<T> exists in Ardalis.Result.
                // We invoke the base Result.Invalid and then apply the implicit operator via the declared operator method.
                var implicitOp = t.GetMethod(
                    "op_Implicit",
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: [typeof(Result)],
                    modifiers: null);

                if (implicitOp is not null)
                    return errs => implicitOp.Invoke(null, [method.Invoke(null, [errs])])!;

                // Fallback: Result<T> may expose its own Invalid static method directly.
                var directMethod = t.GetMethod(
                    nameof(Result.Invalid),
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: [typeof(IEnumerable<ValidationError>)],
                    modifiers: null);

                if (directMethod is not null)
                    return errs => directMethod.Invoke(null, [errs])!;

                return _ => throw new InvalidOperationException($"Cannot build an Invalid result for type {t}.");
            });

            return (TResponse)factory(errors);
        }

        throw new ValidationException(failures);
    }
}
