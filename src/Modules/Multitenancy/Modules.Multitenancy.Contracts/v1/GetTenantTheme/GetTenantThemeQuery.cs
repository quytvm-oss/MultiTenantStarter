using Mediator;

using Modules.Multitenancy.Contracts.Dtos;

namespace Modules.Multitenancy.Contracts.v1.GetTenantTheme;

public record GetTenantThemeQuery : IQuery<TenantThemeDto>;