using Mediator;

using Modules.Multitenancy.Contracts.Dtos;

namespace Modules.Multitenancy.Contracts.v1.UpdateTenantTheme;

public sealed record UpdateTenantThemeCommand(TenantThemeDto Theme) : ICommand;