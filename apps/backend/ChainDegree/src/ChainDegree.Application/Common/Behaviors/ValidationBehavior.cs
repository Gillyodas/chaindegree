using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.SharedKernel.Common.Error;
using ChainDegree.SharedKernel.Result;
using FluentValidation;
using MediatR;

namespace ChainDegree.Core.Application.Common.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
        where TResponse : Result
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!_validators.Any())
            {
                return await next();
            }

            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                var firstFailure = failures[0];
                var error = Error.Validation(
                    firstFailure.PropertyName,
                    firstFailure.ErrorMessage);

                if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var resultType = typeof(TResponse).GetGenericArguments()[0];
                    var failureMethod = typeof(Result<>)
                        .MakeGenericType(resultType)
                        .GetMethod(nameof(Result.Failure), new[] { typeof(Error), typeof(Exception) });

                    var result = failureMethod?.Invoke(null, new object?[] { error, null });
                    return (TResponse)result!;
                }

                return (TResponse)Result.Failure(error);
            }

            return await next();
        }
    }
}
