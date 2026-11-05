using System;
using System.Collections.Generic;
using System.Text;

using Mediator;

using Modules.Multitenancy.Contracts.Dtos;

namespace Modules.Multitenancy.Contracts.v1.ChangeTenantActivation;

public sealed record ChangeTenantActivationCommand(string TenantId, bool IsActive)
    : ICommand<TenantLifecycleResultDto>;