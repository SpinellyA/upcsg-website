using FluentValidation;
using MediatR;

namespace UpcsgWeb.Application.Behaviors;

/// <summary>
/// Runs every validator registered for a request before its handler sees it.
///
/// This is the shape check only — "a reason is required", "quantity is at least 1".
/// Whether the move is legal given the current state stays in the aggregate, because
/// that answer depends on data the validator has not loaded. A validator saying an
/// order can be released is a validator guessing.
/// </summary>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next(cancellationToken);
    }
}
