using Mediator;

using Modules.Identity.Contracts.DTOs;

namespace Modules.Identity.Contracts.v1.Impersonation.RevokeImpersonationGrant;

public record RevokeImpersonationGrantCommand(
    Guid GrantId,
    string? Reason) : ICommand<ImpersonationGrantDto>;