using System.Net;
using System.Text.Json;
using tesisAPI.Exceptions;

namespace tesisAPI.Middlewares
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger;

        public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception error)
            {
                _logger.LogError(error, "Error no controlado.");

                context.Response.ContentType = "application/json";

                var response = context.Response;
                response.StatusCode = error switch
                {
                    UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                    BusinessException => StatusCodes.Status400BadRequest,
                    NotFoundException => StatusCodes.Status404NotFound,
                    ConflictException => StatusCodes.Status409Conflict,
                    _ => StatusCodes.Status500InternalServerError
                };


                var result = JsonSerializer.Serialize(new
                {
                    message = error.Message,
                    errorType = error.GetType().Name,
                    statusCode = response.StatusCode,
                    traceId = context.TraceIdentifier
                });

                await context.Response.WriteAsync(result);
            }
        }

    }
}
