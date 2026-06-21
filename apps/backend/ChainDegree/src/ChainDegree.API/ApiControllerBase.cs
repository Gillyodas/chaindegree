using Microsoft.AspNetCore.Mvc;
using ChainDegree.SharedKernel.Result;
using ChainDegree.SharedKernel.Common.Error;
using Microsoft.AspNetCore.Http;

namespace ChainDegree.Core.API
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>
        /// Xử lý kết quả trả về từ Result Pattern
        /// </summary>
        protected IActionResult ProcessResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                // Nếu thành công, mặc định trả về 200 OK. 
                // Với trường hợp 201 Created, bạn nên override hoặc gọi hàm cụ thể hơn.
                return Ok(result.Value);
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

            // Map ErrorType sang mã HTTP
            var statusCode = result.Error.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };

            // Trả về ProblemDetails theo chuẩn
            return Problem(
                statusCode: statusCode,
                title: GetTitle(result.Error.Type),
                detail: result.Error.Message,
                extensions: new Dictionary<string, object?>
                {
                    { "errorCode", result.Error.Code }
                }
            );
        }

        private static string GetTitle(ErrorType type) => type switch
        {
            ErrorType.Validation => "Bad Request - Validation Error",
            ErrorType.Unauthorized => "Unauthorized Access",
            ErrorType.Forbidden => "Permission Denied",
            ErrorType.NotFound => "Resource Not Found",
            ErrorType.Conflict => "Data Conflict",
            _ => "Server Error"
        };
    }
}
