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

        int statusCode;
        string? errorCode;
        string detail;

        var isDev = context.HttpContext.RequestServices
            .GetRequiredService<IHostEnvironment>()
            .IsDevelopment();

        if (context.Exception is IProblemException problemException)
        {
            statusCode = problemException.StatusCode;
            errorCode = problemException.ErrorCode;
            detail = isDev ? context.Exception.Message : problemException.Detail;
        }
        else
        {
            statusCode = StatusCodes.Status500InternalServerError;
            errorCode = null;
            detail = isDev ? context.Exception.Message : "An unexpected error occurred";
        }

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

