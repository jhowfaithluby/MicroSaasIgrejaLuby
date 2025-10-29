using FluentValidation;
using MediatR;

namespace MicroSaas.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))).ConfigureAwait(false);

            var failures = validationResults
                .SelectMany(result => result.Errors)
                .Where(failure => failure is not null)
                .ToArray();

            if (failures.Length != 0)
            {
                throw new ValidationException(failures);
            }
        }

        return await next().ConfigureAwait(false);
    }
}
