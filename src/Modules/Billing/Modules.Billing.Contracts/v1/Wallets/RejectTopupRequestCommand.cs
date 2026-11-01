using System;
using System.Collections.Generic;
using System.Text;

using Mediator;

namespace Modules.Billing.Contracts.v1.Wallets;

/// <summary>
/// Operator command — rejects a Pending top-up request. Returns the request id.
/// </summary>
public sealed record RejectTopupRequestCommand(Guid Id, string? Reason) : ICommand<Guid>;