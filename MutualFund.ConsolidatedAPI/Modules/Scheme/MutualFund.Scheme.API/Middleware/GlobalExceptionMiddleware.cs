using System.Text.Json;
using MutualFund.Scheme.Application.DTOs;
using MutualFund.Scheme.Domain.Exceptions;

namespace MutualFund.Scheme.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
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
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var traceId = context.TraceIdentifier;

            var errorResponse = exception switch
            {
                ValidationException ex => new ErrorResponseDto
                {
                    ErrorCode = ex.ErrorCode,
                    Message = ex.Message,
                    StatusCode = ex.StatusCode,
                    TraceId = traceId,
                    ValidationErrors = ex.Errors
                },
                NotFoundException ex => new ErrorResponseDto
                {
                    ErrorCode = ex.ErrorCode,
                    Message = ex.Message,
                    StatusCode = ex.StatusCode,
                    TraceId = traceId
                },
                NavDataNotFoundException ex => new ErrorResponseDto
                {
                    ErrorCode = ex.ErrorCode,
                    Message = ex.Message,
                    StatusCode = ex.StatusCode,
                    TraceId = traceId
                },
                DuplicateException ex => new ErrorResponseDto
                {
                    ErrorCode = ex.ErrorCode,
                    Message = ex.Message,
                    StatusCode = ex.StatusCode,
                    TraceId = traceId
                },
                SchemeEnrollmentException ex => new ErrorResponseDto
                {
                    ErrorCode = ex.ErrorCode,
                    Message = ex.Message,
                    StatusCode = ex.StatusCode,
                    TraceId = traceId
                },
                FundApprovalException ex => new ErrorResponseDto
                {
                    ErrorCode = ex.ErrorCode,
                    Message = ex.Message,
                    StatusCode = ex.StatusCode,
                    TraceId = traceId
                },
                SchemeApiException ex => new ErrorResponseDto
                {
                    ErrorCode = ex.ErrorCode,
                    Message = ex.Message,
                    StatusCode = ex.StatusCode,
                    TraceId = traceId
                },
                _ => new ErrorResponseDto
                {
                    ErrorCode = "INTERNAL_SERVER_ERROR",
                    Message = "An unexpected error occurred. Please try again.",
                    StatusCode = 500,
                    TraceId = traceId
                }
            };

            var request = $"{context.Request.Method} {context.Request.Path}";

            if (errorResponse.StatusCode >= 500)
                _logger.LogCritical(exception,
                    "Unhandled exception — TraceId: {TraceId} Request: {Request}",
                    traceId, request);
            else
                _logger.LogWarning(
                    "Client error — TraceId: {TraceId} Request: {Request} Code: {Code}",
                    traceId, request, errorResponse.ErrorCode);

            context.Response.StatusCode = errorResponse.StatusCode;
            context.Response.ContentType = "application/json";

            var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition =
                    System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            await context.Response.WriteAsync(json);
        }
    }

    public static class GlobalExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(
            this IApplicationBuilder app) =>
            app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}