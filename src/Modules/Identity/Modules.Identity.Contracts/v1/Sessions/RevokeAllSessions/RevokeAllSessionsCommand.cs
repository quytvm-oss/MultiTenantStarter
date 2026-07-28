using Mediator;

namespace Modules.Identity.Contracts.v1.Sessions.RevokeAllSessions;

public record RevokeAllSessionsCommand(Guid? ExceptSessionId = null) : ICommand<int>;