using Mediator;

namespace Modules.Identity.Contracts.v1.Sessions.AdminRevokeSession;

public record AdminRevokeSessionCommand(Guid UserId, Guid SessionId, string? Reason = null) : ICommand<bool>;