using Microsoft.AspNetCore.Diagnostics;
using ReleasePilot.Api.Endpoints;
using ReleasePilot.Application;
using ReleasePilot.Domain.Exceptions;
using ReleasePilot.Infrastructure;
using Scalar.AspNetCore;
using FluentValidation;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Configure Minimal API JSON serialization to use string representations for enums
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        if (exception is ValidationException validationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
            await context.Response.WriteAsJsonAsync(new
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Errors = errors
            });
        }
        else if (exception is ConcurrentPromotionException || exception is ImmutablePromotionException)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                Title = "A conflict occurred during the request.",
                Status = StatusCodes.Status409Conflict,
                Detail = exception.Message
            });
        }
        else if (exception is DomainException domainException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = "A domain error occurred.",
                Status = StatusCodes.Status400BadRequest,
                Detail = exception.Message
            });
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                Title = "An error occurred while processing your request.",
                Status = StatusCodes.Status500InternalServerError,
                Detail = exception?.Message
            });
        }
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.AddCommandEndpoints();
app.AddQueryEndpoints();

app.Run();
