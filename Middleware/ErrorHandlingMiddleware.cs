using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SemanticKernelDocumentQA.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next   = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode  = exception switch
            {
                ArgumentNullException       => StatusCodes.Status400BadRequest,
                ArgumentException           => StatusCodes.Status400BadRequest,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                NotSupportedException       => StatusCodes.Status415UnsupportedMediaType,
                FileNotFoundException       => StatusCodes.Status404NotFound,
                _                           => StatusCodes.Status500InternalServerError
            };

            var payload = new
            {
                error = new
                {
                    message   = "An error occurred while processing your request.",
                    details   = exception.Message,
                    timestamp = DateTime.UtcNow
                }
            };

            var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, opts));
        }
    }
}
