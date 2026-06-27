using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ReleasePilot.Api.Endpoints.Requests;
using ReleasePilot.Application.Commands;

namespace ReleasePilot.Api.Endpoints;

public static class CommandEndpoints
{
    public static WebApplication AddCommandEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/promotions");

        group.MapPost("", async (RequestPromotionRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new RequestPromotionCommand(
                request.ApplicationId,
                request.Version,
                request.TargetEnvironment,
                request.RequestedByUserId);

            var result = await mediator.Send(command, ct);

            return Results.Created($"/promotions/{result.Value}", new { id = result.Value });
        });

        group.MapPost("/{id:guid}/approve", async (Guid id, ApprovePromotionRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new ApprovePromotionCommand(
                id,
                request.ApproverId,
                request.ApproverRoles);

            await mediator.Send(command, ct);

            return Results.Ok(new { id, status = "Approved" });
        });

        group.MapPost("/{id:guid}/start", async (Guid id, StartDeploymentRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new StartDeploymentCommand(
                id,
                request.UserId);

            await mediator.Send(command, ct);

            return Results.Ok(new { id, status = "InProgress" });
        });

        group.MapPost("/{id:guid}/complete", async (Guid id, CompletePromotionRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new CompletePromotionCommand(
                id,
                request.UserId);

            await mediator.Send(command, ct);

            return Results.Ok(new { id, status = "Completed" });
        });

        group.MapPost("/{id:guid}/rollback", async (Guid id, RollbackPromotionRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new RollbackPromotionCommand(
                id,
                request.UserId,
                request.Reason);

            await mediator.Send(command, ct);

            return Results.Ok(new { id, status = "RolledBack" });
        });

        group.MapPost("/{id:guid}/cancel", async (Guid id, CancelPromotionRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new CancelPromotionCommand(
                id,
                request.UserId);

            await mediator.Send(command, ct);

            return Results.Ok(new { id, status = "Cancelled" });
        });

        return app;
    }
}
