using ReleasePilot.Api.Endpoints;
using ReleasePilot.Api.Middleware;
using ReleasePilot.Application;
using ReleasePilot.Infrastructure;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info.Title = "ReleasePilot API";
        document.Info.Version = "v1";
        document.Info.Description = "Promotion engine for application lifecycle management";
        return Task.CompletedTask;
    });
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Register Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgresql",
        tags: ["db", "ready"])
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{builder.Configuration["RabbitMq:Username"]}:{builder.Configuration["RabbitMq:Password"]}@{builder.Configuration["RabbitMq:Host"]}",
        name: "rabbitmq",
        tags: ["messaging", "ready"]);

// Configure Minimal API JSON serialization to use string representations for enums
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

// Global Exception Handling Middleware
app.UseMiddleware<DomainExceptionMiddleware>();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "ReleasePilot";
    options.Theme = ScalarTheme.Purple;
    options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseHttpsRedirection();

// Map Health Check Endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.AddCommandEndpoints();
app.AddQueryEndpoints();

app.Run();
