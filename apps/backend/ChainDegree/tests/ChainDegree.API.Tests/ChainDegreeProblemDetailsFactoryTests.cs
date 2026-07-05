using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using ChainDegree.API.Filters;

namespace ChainDegree.API.Tests
{
    public class ChainDegreeProblemDetailsFactoryTests
    {
        private readonly ChainDegreeProblemDetailsFactory _factory;
        private readonly Mock<IOptions<ApiBehaviorOptions>> _mockOptions;

        public ChainDegreeProblemDetailsFactoryTests()
        {
            _mockOptions = new Mock<IOptions<ApiBehaviorOptions>>();
            _mockOptions.Setup(o => o.Value).Returns(new ApiBehaviorOptions());
            _factory = new ChainDegreeProblemDetailsFactory(_mockOptions.Object);
        }

        [Fact]
        public void CreateProblemDetails_ShouldEnrichWithTimestampAndTraceId()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = "test-trace-id";
            httpContext.Request.Path = "/api/test";

            // Act
            var problemDetails = _factory.CreateProblemDetails(
                httpContext,
                statusCode: StatusCodes.Status400BadRequest,
                detail: "Bad request occurred");

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
            Assert.Equal("Bad request occurred", problemDetails.Detail);
            Assert.Equal("/api/test", problemDetails.Instance);
            Assert.Equal("https://chaindegree.io/errors/bad-request", problemDetails.Type);

            Assert.True(problemDetails.Extensions.ContainsKey("timestamp"));
            Assert.True(problemDetails.Extensions.ContainsKey("traceId"));
            Assert.Equal("test-trace-id", problemDetails.Extensions["traceId"]);
        }

        [Fact]
        public void CreateProblemDetails_ShouldEnrichWithCorrelationId_WhenRequestHeaderPresent()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Request-Id"] = "correlation-123";

            // Act
            var problemDetails = _factory.CreateProblemDetails(
                httpContext,
                statusCode: StatusCodes.Status500InternalServerError);

            // Assert
            Assert.True(problemDetails.Extensions.ContainsKey("correlationId"));
            Assert.Equal("correlation-123", problemDetails.Extensions["correlationId"]);
        }

        [Fact]
        public void CreateValidationProblemDetails_ShouldHaveValidationFailedErrorCode()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("Name", "Name is required");

            // Act
            var validationProblem = _factory.CreateValidationProblemDetails(
                httpContext,
                modelState,
                statusCode: StatusCodes.Status400BadRequest);

            // Assert
            Assert.Equal("Validation Error", validationProblem.Title);
            Assert.Equal("https://chaindegree.io/errors/bad-request", validationProblem.Type);
            Assert.True(validationProblem.Extensions.ContainsKey("errorCode"));
            Assert.Equal("VALIDATION_FAILED", validationProblem.Extensions["errorCode"]);
            Assert.True(validationProblem.Errors.ContainsKey("Name"));
        }
    }
}
