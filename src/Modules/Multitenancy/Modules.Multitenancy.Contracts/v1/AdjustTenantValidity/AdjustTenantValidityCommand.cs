using Mediator;

namespace Modules.Multitenancy.Contracts.v1.AdjustTenantValidity;

public sealed record AdjustTenantValidityCommand(string TenantId,  DateTime ValidUpto) : IQuery<AdjustTenantValidityCommandResponse>;

public sealed record AdjustTenantValidityCommandResponse(string TenantId,  DateTime ValidUpto);