using Mediator;

namespace Modules.Identity.Contracts.v1.Sessions.AdminRevokeAllSessions;

public record AdminRevokeAllSessionsCommand(Guid UserId, string? Reason = null) : ICommand<int>;