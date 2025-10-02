using System.Net;
using System.Text.Json;
using AISAM.Common;

namespace AISAM.API.Middleware
{
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlerMiddleware> _logger;

        public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
        {
            _next = next;
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

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            HttpStatusCode status;
            string message;
            string errorCode;

            switch (exception)
            {
                case UnauthorizedAccessException:
                    status = HttpStatusCode.Unauthorized;
                    message = "Unauthorized";
                    errorCode = "UNAUTHORIZED";
                    break;
                case ArgumentException:
                case InvalidOperationException:
                    status = HttpStatusCode.BadRequest;
                    message = exception.Message;
                    errorCode = "BAD_REQUEST";
                    break;
                case KeyNotFoundException:
                    status = HttpStatusCode.NotFound;
                    message = exception.Message;
                    errorCode = "NOT_FOUND";
                    break;
                default:
                    status = HttpStatusCode.InternalServerError;
                    message = "An unexpected error occurred";
                    errorCode = "INTERNAL_SERVER_ERROR";
                    break;
            }

            context.Response.StatusCode = (int)status;

            var response = GenericResponse<object>.CreateError(
                message,
                status,
                errorCode);

            // Add more detailed error info in development environment
            if (context.RequestServices.GetService(typeof(IWebHostEnvironment)) is IWebHostEnvironment env && env.IsDevelopment())
            {
                response.Error.StackTrace = exception.ToString();
                response.Error.ErrorMessage = exception.Message;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(response, options);
            return context.Response.WriteAsync(json);
        }
    }

    // Extension method to easily add this middleware to the pipeline
    public static class ExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandlerMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlerMiddleware>();
        }
    }
}