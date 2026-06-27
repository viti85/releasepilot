using System.Text.Json;
using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ReleasePilot.Domain.Exceptions;

namespace ReleasePilot.Api.Middleware;

public sealed class DomainExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DomainExceptionMiddleware> _logger;

    public DomainExceptionMiddleware(RequestDelegate next, ILogger<DomainExceptionMiddleware> logger)
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

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, title, detail, type) = MapException(ex);

        // Logging requirements:
        // Log every 5xx as Error (with stack trace), every 4xx as Warning (without stack trace for domain/validation/notfound exceptions)
        if (statusCode >= 500)
        {
            _logger.LogError(ex, "HTTP {StatusCode} - Internal Server Error: {Message}", statusCode, ex.Message);
        }
        else
        {
            _logger.LogWarning("HTTP {StatusCode} - Warning: {Title}. Detail: {Detail}", statusCode, title, detail);
        }

        context.Response.StatusCode = statusCode;

        var responseObj = new Dictionary<string, object?>
        {
            { "type", type },
            { "title", title },
            { "status", statusCode },
            { "detail", detail },
            { "instance", context.Request.Path.Value }
        };

        if (ex is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
            responseObj.Add("errors", errors);
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsJsonAsync(responseObj, options, contentType: "application/problem+json");
    }

    private static (int StatusCode, string Title, string Detail, string Type) MapException(Exception ex)
    {
        var type = GetErrorType(ex);
        
        return ex switch
        {
            ValidationException => (
                StatusCodes.Status400BadRequest,
                "Validation error",
                "One or more validation errors occurred.",
                type
            ),
            UnauthorizedApprovalException => (
                StatusCodes.Status403Forbidden,
                "Unauthorized approval",
                ex.Message,
                type
            ),
            _ when ex.GetType().Name.Contains("NotFound") => (
                StatusCodes.Status404NotFound,
                "Not found",
                ex.Message,
                type
            ),
            ConcurrentPromotionException => (
                StatusCodes.Status409Conflict,
                "Concurrent promotion",
                ex.Message,
                type
            ),
            ImmutablePromotionException => (
                StatusCodes.Status409Conflict,
                "Immutable promotion",
                ex.Message,
                type
            ),
            EnvironmentSkippedException => (
                StatusCodes.Status422UnprocessableEntity,
                "Environment skipped",
                ex.Message,
                type
            ),
            DomainException => (
                StatusCodes.Status422UnprocessableEntity,
                GetErrorTitle(ex),
                ex.Message,
                type
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal server error",
                "An unexpected error occurred.",
                type
            )
        };
    }

    private static string GetErrorType(Exception exception)
    {
        var typeName = exception.GetType().Name;
        if (typeName.EndsWith("Exception"))
        {
            typeName = typeName.Substring(0, typeName.Length - "Exception".Length);
        }

        var kebabCase = Regex.Replace(typeName, "(?<!^)([A-Z])", "-$1").ToLower();
        return $"https://releasepilot.dev/errors/{kebabCase}";
    }

    private static string GetErrorTitle(Exception exception)
    {
        var typeName = exception.GetType().Name;
        if (typeName.EndsWith("Exception"))
        {
            typeName = typeName.Substring(0, typeName.Length - "Exception".Length);
        }

        // Add space and convert to Sentence case (e.g. EnvironmentSkipped -> Environment skipped)
        var spaced = Regex.Replace(typeName, "(?<!^)([A-Z])", " $1");
        if (spaced.Length > 0)
        {
            return char.ToUpper(spaced[0]) + spaced.Substring(1).ToLower();
        }
        return spaced;
    }
}
