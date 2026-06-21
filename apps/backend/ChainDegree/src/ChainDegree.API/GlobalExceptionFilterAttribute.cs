using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ChainDegree.API;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class GlobalExceptionFilterAttribute : ExceptionFilterAttribute
{
    private readonly ILogger<GlobalExceptionFilterAttribute> _logger;

    public GlobalExceptionFilterAttribute(ILogger<GlobalExceptionFilterAttribute> logger)
    {
        _logger = logger;
    }

    public override void OnException(ExceptionContext context)
    {
        _logger.LogCritical(context.Exception,
            "Unhandled exception in {@ControllerAction}",
            $"{context.RouteData.Values["controller"]}/{context.RouteData.Values["action"]}");

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = context.HttpContext.RequestServices
                .GetRequiredService<IHostEnvironment>()
                .IsDevelopment()
                ? context.Exception.Message
                : "An unexpected error occurred"
        };

        context.Result = new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };
    }
}
