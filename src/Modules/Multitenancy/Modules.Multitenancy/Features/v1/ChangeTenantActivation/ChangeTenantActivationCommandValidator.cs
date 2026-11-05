using System;
using System.Collections.Generic;
using System.Text;

using FluentValidation;

using Modules.Multitenancy.Contracts.v1.ChangeTenantActivation;

namespace Modules.Multitenancy.Features.v1.ChangeTenantActivation;

public class ChangeTenantActivationCommandValidator  : AbstractValidator<ChangeTenantActivationCommand>
{
    public ChangeTenantActivationCommandValidator()
    {
        RuleFor(t => t.TenantId).NotEmpty();
    }
}
