using MediatR;
using ReleasePilot.Application.Dtos;

namespace ReleasePilot.Application.Queries;

public sealed record GetApplicationStatusQuery(Guid ApplicationId) : IRequest<ApplicationStatusDto>;
