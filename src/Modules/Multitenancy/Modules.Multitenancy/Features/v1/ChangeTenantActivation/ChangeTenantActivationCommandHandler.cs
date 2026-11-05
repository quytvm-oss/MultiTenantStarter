using System;
using System.Collections.Generic;
using System.Text;

using Mediator;

using Modules.Multitenancy.Contracts.Dtos;
using Modules.Multitenancy.Contracts.v1;
using Modules.Multitenancy.Contracts.v1.ChangeTenantActivation;

namespace Modules.Multitenancy.Features.v1.ChangeTenantActivation;

public sealed class ChangeTenantActivationCommandHandler : ICommandHandler<ChangeTenantActivationCommand, TenantLifecycleResultDto>
{
    private readonly ITenantService _tenantService;

    public ChangeTenantActivationCommandHandler(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public async ValueTask<TenantLifecycleResultDto> Handle(ChangeTenantActivationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string message;

        if (command.IsActive)
        {
            message = await _tenantService.ActivateAsync(command.TenantId, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            message = await _tenantService.DeactivateAsync(command.TenantId, cancellationToken).ConfigureAwait(false);
        }

        var status = await _tenantService.GetStatusAsync(command.TenantId, cancellationToken).ConfigureAwait(false);

        return new TenantLifecycleResultDto()
        {
            TenantId = command.TenantId,
            IsActive = status.IsActive,
            Message = message,
            ValidUpto = status.ValidUpto
        };
    }
}
