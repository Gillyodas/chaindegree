using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using ChainDegree.Core.Application.Common.Exceptions;

namespace ChainDegree.API;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class GlobalExceptionFilterAttribute : ExceptionFilterAttribute
{
    private readonly ILogger<GlobalExceptionFilterAttribute> _logger;
    private readonly ProblemDetailsFactory _problemDetailsFactory;

    public GlobalExceptionFilterAttribute(
        ILogger<GlobalExceptionFilterAttribute> logger,
        ProblemDetailsFactory problemDetailsFactory)
    {
        _logger = logger;
        _problemDetailsFactory = problemDetailsFactory;
    }

    public override void OnException(ExceptionContext context)
    {
        _logger.LogCritical(context.Exception,
            "Unhandled exception in {@ControllerAction}",
            $"{context.RouteData.Values["controller"]}/{context.RouteData.Values["action"]}");

        var statusCode = StatusCodes.Status500InternalServerError;
        string? errorCode = null;

        if (context.Exception is RepositoryConcurrencyException)
        {
            statusCode = StatusCodes.Status409Conflict;
            errorCode = "CONCURRENCY_ERROR";
        }
        else if (context.Exception is RepositoryException)
        {
            statusCode = StatusCodes.Status400BadRequest;
            errorCode = "REPOSITORY_ERROR";
        }

        var detail = context.HttpContext.RequestServices
            .GetRequiredService<IHostEnvironment>()
            .IsDevelopment()
            ? context.Exception.Message
            : "An unexpected error occurred";

        var problem = _problemDetailsFactory.CreateProblemDetails(
            context.HttpContext,
            statusCode: statusCode,
            detail: detail);

        if (errorCode != null)
        {
            problem.Extensions["errorCode"] = errorCode;
        }

        context.Result = new ObjectResult(problem)
        {
            StatusCode = statusCode
        };
    }
}

