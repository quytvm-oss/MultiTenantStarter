using System;
using System.Collections.Generic;
using System.Text;

using Mediator;

namespace Modules.Billing.Contracts.v1.Wallets;

/// <summary>
/// Operator command — approves a Pending top-up request, creates and issues a Topup-purpose
/// invoice, and transitions the request to Invoiced. Returns the created invoice id.
/// </summary>
public sealed record ApproveTopupRequestCommand(Guid Id, string? Note) : ICommand<Guid>;
