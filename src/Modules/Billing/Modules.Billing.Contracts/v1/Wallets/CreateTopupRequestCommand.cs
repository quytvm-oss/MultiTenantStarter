using System;
using System.Collections.Generic;
using System.Text;

using Mediator;

namespace Modules.Billing.Contracts.v1.Wallets;

public sealed record CreateTopupRequestCommand(decimal Amount, string? Note) : ICommand<Guid>;
