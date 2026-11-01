using System;
using System.Collections.Generic;
using System.Text;

using Mediator;

using Modules.Billing.Contracts.Dtos;

using Shared.Persistence;

namespace Modules.Billing.Contracts.v1.Wallets;

public sealed record GetTopupRequestsQuery(
    string? TenantId = null,
    TopupRequestStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagedResponse<TopupRequestDto>>;