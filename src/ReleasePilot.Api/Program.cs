using ReleasePilot.Api.Endpoints;
using ReleasePilot.Api.Middleware;
using ReleasePilot.Application;
using ReleasePilot.Infrastructure;
using Scalar.AspNetCore;
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

// Global Exception Handling Middleware
app.UseMiddleware<DomainExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.AddCommandEndpoints();
app.AddQueryEndpoints();

app.Run();
