using Mediator;

namespace Modules.Identity.Contracts.v1.Tokens.Logout;

public sealed record LogoutCommand(string RefreshToken) : ICommand<Unit>;