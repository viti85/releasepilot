using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ReleasePilot.Api.Endpoints.Responses;
using ReleasePilot.Application.Queries;

namespace ReleasePilot.Api.Endpoints;

public static class QueryEndpoints
{
    public static WebApplication AddQueryEndpoints(this WebApplication app)
    {
        app.MapGet("/promotions/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var query = new GetPromotionByIdQuery(id);
            var result = await mediator.Send(query, ct);

            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        app.MapGet("/applications/{id:guid}/status", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var query = new GetApplicationStatusQuery(id);
            var result = await mediator.Send(query, ct);

            return Results.Ok(result);
        });

        app.MapGet("/applications/{id:guid}/promotions", async (
            Guid id,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IMediator mediator,
            CancellationToken ct) =>
        {
            int actualPage = page ?? 1;
            int actualPageSize = Math.Min(pageSize ?? 20, 100);

            var query = new GetPromotionHistoryQuery(id, actualPage, actualPageSize);
            var result = await mediator.Send(query, ct);

            int totalPages = result.TotalCount == 0 
                ? 1 
                : (int)Math.Ceiling((double)result.TotalCount / result.PageSize);

            var response = new PagedResponse<Application.Dtos.PromotionSummaryDto>(
                result.Items,
                result.Page,
                result.PageSize,
                result.TotalCount,
                totalPages);

            return Results.Ok(response);
        });

        return app;
    }
}
