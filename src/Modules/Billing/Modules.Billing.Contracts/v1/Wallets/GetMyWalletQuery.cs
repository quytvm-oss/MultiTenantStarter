using System;
using System.Collections.Generic;
using System.Text;

using Mediator;

using Modules.Billing.Contracts.Dtos;

namespace Modules.Billing.Contracts.v1.Wallets;

public sealed record GetMyWalletQuery() : IQuery<WalletDto>;
