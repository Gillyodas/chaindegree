using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using ChainDegree.SharedKernel.Result;
using ChainDegree.SharedKernel.Common.Error;
using Microsoft.AspNetCore.Http;

namespace ChainDegree.API
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>
        /// Xử lý kết quả trả về từ Result Pattern cho các request thông thường (200 OK)
        /// </summary>
        protected IActionResult ProcessResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return HandleFailure(result);
        }

        /// <summary>
        /// Xử lý kết quả trả về cho việc tạo mới tài nguyên (201 Created)
        /// </summary>
        protected IActionResult ProcessCreatedResult<T>(Result<T> result, string actionName, object? routeValues)
        {
            if (result.IsSuccess)
            {
                return CreatedAtAction(actionName, routeValues, result.Value);
            }

            return HandleFailure(result);
        }

        /// <summary>
        /// Xử lý kết quả trả về cho các xử lý bất đồng bộ hoặc được chấp nhận (202 Accepted)
        /// </summary>
        protected IActionResult ProcessAcceptedResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                return Accepted(result.Value);
            }

            return HandleFailure(result);
        }

        /// <summary>
        /// Ánh xạ ErrorType sang HTTP Status Code tương ứng
        /// </summary>
        protected IActionResult HandleFailure(Result result)
        {
            if (result.IsSuccess)
            {
                throw new InvalidOperationException("Cannot handle failure for a successful result.");
            }

            var statusCode = result.Error.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };

            return Problem(
                statusCode: statusCode,
                detail: result.Error.Message,
                extensions: new Dictionary<string, object?>
                {
                    { "errorCode", result.Error.Code }
                }
            );
        }
    }
}
