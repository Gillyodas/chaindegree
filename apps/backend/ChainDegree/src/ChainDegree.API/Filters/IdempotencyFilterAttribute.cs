using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ChainDegree.Core.Infrastructure.Persistence;
using ChainDegree.Core.Infrastructure.Persistence.Entities;

namespace ChainDegree.API.Filters
{
    public class IdempotencyFilterAttribute : IAsyncActionFilter
    {
        private readonly ChainDegreeDbContext _dbContext;

        public IdempotencyFilterAttribute(ChainDegreeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;

            if (!request.Headers.TryGetValue("Idempotency-Key", out var keyHeader) || string.IsNullOrEmpty(keyHeader))
            {
                await next();
                return;
            }

            var idempotencyKey = keyHeader.ToString();

            // Check if record exists in database
            var existingRecord = await _dbContext.IdempotencyRecords.FindAsync(idempotencyKey);
            if (existingRecord != null)
            {
                if (existingRecord.ExpiresAt < DateTime.UtcNow)
                {
                    _dbContext.IdempotencyRecords.Remove(existingRecord);
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    // Return cached response
                    var contentResult = new ContentResult
                    {
                        Content = existingRecord.ResponseBodyJson,
                        ContentType = "application/json",
                        StatusCode = existingRecord.ResponseStatusCode
                    };

                    context.Result = contentResult;
                    return;
                }
            }

            var executedContext = await next();

            if (executedContext.Exception == null && executedContext.Result is ObjectResult objectResult)
            {
                var statusCode = objectResult.StatusCode ?? 200;

                // Cache only successful responses (2xx) and client errors (4xx)
                if (statusCode >= 200 && statusCode < 500)
                {
                    var responseJson = System.Text.Json.JsonSerializer.Serialize(objectResult.Value);

                    var record = new IdempotencyRecord
                    {
                        IdempotencyKey = idempotencyKey,
                        ResponseBodyJson = responseJson,
                        ResponseStatusCode = statusCode,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddHours(24)
                    };

                    _dbContext.IdempotencyRecords.Add(record);
                    await _dbContext.SaveChangesAsync();
                }
            }
        }
    }
}
