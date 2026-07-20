using System.Net;
using System.Text.Json;
using MutualFund.Auth.Application.DTOs;
using MutualFund.Auth.Domain.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MutualFund.Auth.API.Middleware
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

        private async Task HandleExceptionAsync(
            HttpContext context, Exception exception)
        {
            var traceId = context.TraceIdentifier;
            var request = $"{context.Request.Method} {context.Request.Path}";

            ErrorResponseDto errorResponse = exception switch
            {
                // All typed Auth exceptions
                AuthException ex => new ErrorResponseDto
                {
                    ErrorCode = ex.ErrorCode,
                    Message = ex.Message,
                    StatusCode = ex.StatusCode,
                    TraceId = traceId
                },

                // Unexpected exceptions
                _ => new ErrorResponseDto
                {
                    ErrorCode = "INTERNAL_SERVER_ERROR",
                    Message = "An unexpected error occurred. Please try again.",
                    StatusCode = 500,
                    TraceId = traceId
                }
            };

            // Log with appropriate level
            if (errorResponse.StatusCode >= 500)
                _logger.LogCritical(exception,
                    "💥 Unhandled exception — TraceId:{TraceId} " +
                    "Request:{Request} Code:{Code}",
                    traceId, request, errorResponse.ErrorCode);
            else
                _logger.LogWarning(
                    "⚠️ Client error — TraceId:{TraceId} " +
                    "Request:{Request} Code:{Code} Message:{Message}",
                    traceId, request,
                    errorResponse.ErrorCode, errorResponse.Message);

            context.Response.StatusCode = errorResponse.StatusCode;
            context.Response.ContentType = "application/json";

            var json = JsonSerializer.Serialize(errorResponse,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition =
                        System.Text.Json.Serialization
                            .JsonIgnoreCondition.WhenWritingNull
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