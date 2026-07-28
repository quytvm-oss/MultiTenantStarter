using Mediator;

namespace Modules.Identity.Contracts.v1.Sessions.RevokeSession;

public record RevokeSessionCommand(Guid SessionId) : ICommand<bool>;