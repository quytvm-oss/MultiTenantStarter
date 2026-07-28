using Mediator;

using Modules.Identity.Contracts.DTOs;

namespace Modules.Identity.Contracts.v1.Sessions.GetUserSessions;

public record GetUserSessionsQuery(Guid UserId) : IQuery<List<UserSessionDto>>;