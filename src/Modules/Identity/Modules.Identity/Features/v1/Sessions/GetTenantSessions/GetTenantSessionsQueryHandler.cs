using Mediator;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Sessions.GetTenantSessions;

using Shared.Persistence;

namespace Modules.Identity.Features.v1.Sessions.GetTenantSessions;

public class GetTenantSessionsQueryHandler(ISessionService sessionService)
    : IQueryHandler<GetTenantSessionsQuery, PagedResponse<UserSessionDto>>
{

    public async ValueTask<PagedResponse<UserSessionDto>> Handle(GetTenantSessionsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        
        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;

        var (item, total) = await sessionService.GetTenantSessionsAsync(
            includeInactive: query.IncludeInactive,
            search: query.Search,
            skip: (page - 1) * size ,
            take: size,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<UserSessionDto>()
        {
            Items = item,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}