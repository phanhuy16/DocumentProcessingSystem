using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DocumentProcessing.API.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Chuyển tiếp request đến middleware tiếp theo
                await _next(context);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                _logger.LogError(ex, "An error occurred while processing the request.");

                // Xử lý lỗi và trả về phản hồi
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var errorResponse = new
                {
                    context.Response.StatusCode,
                    Message = "An unexpected error occurred. Please try again later.",
                    Details = ex.Message // Có thể ẩn chi tiết trong môi trường production
                };

                await context.Response.WriteAsJsonAsync(errorResponse);
            }
        }
    }
}
