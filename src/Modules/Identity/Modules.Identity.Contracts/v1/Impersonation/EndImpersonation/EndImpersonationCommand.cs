using Mediator;

using Modules.Identity.Contracts.DTOs;

namespace Modules.Identity.Contracts.v1.Impersonation.EndImpersonation;

public record EndImpersonationCommand() : ICommand<TokenResponse>;