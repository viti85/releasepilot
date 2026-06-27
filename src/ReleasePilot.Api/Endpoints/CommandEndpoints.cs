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
        var group = app.MapGroup("/promotions")
            .WithOpenApi()
            .WithTags("Promotions");

        group.MapPost("", async (RequestPromotionRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new RequestPromotionCommand(
                request.ApplicationId,
                request.Version,
                request.TargetEnvironment,
                request.RequestedByUserId);

            var result = await mediator.Send(command, ct);

            return Results.Created($"/promotions/{result.Value}", new { id = result.Value });
        })
        .WithName("RequestPromotion")
        .WithSummary("Request a new promotion")
        .WithDescription("Moves an application version one step forward in the pipeline")
        .Produces<object>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/approve", async (Guid id, ApprovePromotionRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new ApprovePromotionCommand(
                id,
                request.ApproverId,
                request.ApproverRoles);

            await mediator.Send(command, ct);

            return Results.Ok(new { id, status = "Approved" });
        })
        .WithName("ApprovePromotion")
        .WithSummary("Approve a promotion")
        .WithDescription("Approves a pending promotion request")
        .Produces<object>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/start", async (Guid id, StartDeploymentRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new StartDeploymentCommand(
                id,
                request.UserId);

            await mediator.Send(command, ct);

            return Results.Ok(new { id, status = "InProgress" });
        })
        .WithName("StartDeployment")
        .WithSummary("Start deployment")
        .WithDescription("Starts the deployment phase of an approved promotion")
        .Produces<object>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/complete", async (Guid id, CompletePromotionRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new CompletePromotionCommand(
                id,
                request.UserId);

            await mediator.Send(command, ct);

            return Results.Ok(new { id, status = "Completed" });
        })
        .WithName("CompletePromotion")
        .WithSummary("Complete a promotion")
        .WithDescription("Completes the deployment of a promotion, marking it successful")
        .Produces<object>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/rollback", async (Guid id, RollbackPromotionRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new RollbackPromotionCommand(
                id,
                request.UserId,
                request.Reason);

            await mediator.Send(command, ct);

            return Results.Ok(new { id, status = "RolledBack" });
        })
        .WithName("RollbackPromotion")
        .WithSummary("Rollback a promotion")
        .WithDescription("Rolls back a completed promotion to a previous state")
        .Produces<object>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/cancel", async (Guid id, CancelPromotionRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new CancelPromotionCommand(
                id,
                request.UserId);

            await mediator.Send(command, ct);

            return Results.Ok(new { id, status = "Cancelled" });
        })
        .WithName("CancelPromotion")
        .WithSummary("Cancel a promotion")
        .WithDescription("Cancels a pending or active promotion")
        .Produces<object>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
