using Mediator;

using Modules.Multitenancy.Contracts.Dtos;

namespace Modules.Multitenancy.Contracts.v1.GetTenantStatus;

public sealed record GetTenantStatusQuery(string TenantId) : IQuery<TenantStatusDto>;