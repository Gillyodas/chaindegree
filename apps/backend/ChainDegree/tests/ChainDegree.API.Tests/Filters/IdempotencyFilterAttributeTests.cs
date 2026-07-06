using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChainDegree.API.Filters;
using ChainDegree.Core.Infrastructure.Persistence;
using ChainDegree.Core.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ChainDegree.API.Tests.Filters
{
    public class IdempotencyFilterAttributeTests
    {
        private readonly Mock<ChainDegreeDbContext> _mockContext;
        private readonly Mock<DbSet<IdempotencyRecord>> _mockDbSet;
        private readonly IdempotencyFilterAttribute _filter;

        public IdempotencyFilterAttributeTests()
        {
            var mockUserAccessor = new Mock<Core.Application.Abstractions.Auth.ICurrentUserAccessor>();
            var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<ChainDegreeDbContext>>();

            _mockContext = new Mock<ChainDegreeDbContext>(
                new DbContextOptions<ChainDegreeDbContext>(),
                mockUserAccessor.Object,
                mockLogger.Object);

            _mockDbSet = new Mock<DbSet<IdempotencyRecord>>();
            _mockContext.Setup(c => c.Set<IdempotencyRecord>()).Returns(_mockDbSet.Object);

            _filter = new IdempotencyFilterAttribute(_mockContext.Object);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithoutIdempotencyKeyHeader_CallsNext()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var context = new ActionExecutingContext(
                actionContext, 
                new List<IFilterMetadata>(), 
                new Dictionary<string, object?>(), 
                new object());

            bool nextCalled = false;
            ActionExecutionDelegate next = () =>
            {
                nextCalled = true;
                var executedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object());
                return Task.FromResult(executedContext);
            };

            // Act
            await _filter.OnActionExecutionAsync(context, next);

            // Assert
            Assert.True(nextCalled);
            Assert.Null(context.Result);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithNewIdempotencyKey_ExecutesAndSaves()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var key = "test-key-123";
            httpContext.Request.Headers["Idempotency-Key"] = key;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var context = new ActionExecutingContext(
                actionContext, 
                new List<IFilterMetadata>(), 
                new Dictionary<string, object?>(), 
                new object());

            var expectedResponse = new { Message = "Success" };
            ActionExecutionDelegate next = () =>
            {
                var executedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object())
                {
                    Result = new OkObjectResult(expectedResponse)
                };
                return Task.FromResult(executedContext);
            };

            _mockDbSet.Setup(s => s.FindAsync(new object[] { key })).ReturnsAsync((IdempotencyRecord?)null);

            // Act
            await _filter.OnActionExecutionAsync(context, next);

            // Assert
            _mockDbSet.Verify(s => s.Add(It.Is<IdempotencyRecord>(r => r.IdempotencyKey == key)), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithExistingKey_ReturnsCachedResponse()
        {
            // Arrange
            var key = "test-key-cached";
            var record = new IdempotencyRecord
            {
                IdempotencyKey = key,
                ResponseBodyJson = "{\"Message\":\"CachedSuccess\"}",
                ResponseStatusCode = 202,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _mockDbSet.Setup(s => s.FindAsync(new object[] { key })).ReturnsAsync(record);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Idempotency-Key"] = key;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var context = new ActionExecutingContext(
                actionContext, 
                new List<IFilterMetadata>(), 
                new Dictionary<string, object?>(), 
                new object());

            bool nextCalled = false;
            ActionExecutionDelegate next = () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object()));
            };

            // Act
            await _filter.OnActionExecutionAsync(context, next);

            // Assert
            Assert.False(nextCalled);
            var contentResult = Assert.IsType<ContentResult>(context.Result);
            Assert.Equal(202, contentResult.StatusCode);
            Assert.Equal("{\"Message\":\"CachedSuccess\"}", contentResult.Content);
        }
    }
}
