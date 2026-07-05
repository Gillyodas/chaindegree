using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ChainDegree.API.Filters;
using ChainDegree.Core.Application.Common.Exceptions;

namespace ChainDegree.API.Tests
{
    public class GlobalExceptionFilterAttributeTests
    {
        private readonly Mock<ILogger<GlobalExceptionFilterAttribute>> _mockLogger;
        private readonly Mock<ProblemDetailsFactory> _mockFactory;
        private readonly Mock<IHostEnvironment> _mockEnv;
        private readonly GlobalExceptionFilterAttribute _filter;

        public GlobalExceptionFilterAttributeTests()
        {
            _mockLogger = new Mock<ILogger<GlobalExceptionFilterAttribute>>();
            _mockFactory = new Mock<ProblemDetailsFactory>();
            _mockEnv = new Mock<IHostEnvironment>();

            _filter = new GlobalExceptionFilterAttribute(_mockLogger.Object, _mockFactory.Object);
        }

        private ExceptionContext CreateExceptionContext(Exception exception, HttpContext httpContext)
        {
            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor());

            return new ExceptionContext(actionContext, new List<IFilterMetadata>())
            {
                Exception = exception
            };
        }

        [Fact]
        public void OnException_WithIProblemException_ShouldMapStatusCodeAndErrorCode()
        {
            // Arrange
            var services = new ServiceCollection();
            _mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Production);
            services.AddSingleton(_mockEnv.Object);
            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
            var exception = new RepositoryException("Database connection failed");
            var context = CreateExceptionContext(exception, httpContext);
            context.RouteData.Values["controller"] = "Test";
            context.RouteData.Values["action"] = "Run";

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = "Database connection failed"
            };

            _mockFactory.Setup(f => f.CreateProblemDetails(
                httpContext,
                StatusCodes.Status400BadRequest,
                null,
                null,
                "Database connection failed",
                null
            )).Returns(problemDetails);

            // Act
            _filter.OnException(context);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);

            var responseProblem = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal("REPOSITORY_ERROR", responseProblem.Extensions["errorCode"]);
        }

        [Fact]
        public void OnException_WithGenericException_ShouldFallbackTo500()
        {
            // Arrange
            var services = new ServiceCollection();
            _mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Production);
            services.AddSingleton(_mockEnv.Object);
            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
            var exception = new Exception("Critical system crash");
            var context = CreateExceptionContext(exception, httpContext);
            context.RouteData.Values["controller"] = "Test";
            context.RouteData.Values["action"] = "Run";

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred"
            };

            _mockFactory.Setup(f => f.CreateProblemDetails(
                httpContext,
                StatusCodes.Status500InternalServerError,
                null,
                null,
                "An unexpected error occurred",
                null
            )).Returns(problemDetails);

            // Act
            _filter.OnException(context);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);

            var responseProblem = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.False(responseProblem.Extensions.ContainsKey("errorCode"));
        }
    }
}
