using System.Linq.Expressions;

using Core.Context;

using Mediator;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Users.SearchUsers;
using Modules.Identity.Data;
using Modules.Identity.Domain;

using Persistence.Pagination;

using Shared.Persistence;

namespace Modules.Identity.Features.v1.Users.SearchUsers;

public class SearchUsersQueryHandler : IQueryHandler<SearchUsersQuery, PagedResponse<UserDto>>
{
    private readonly UserManager<User> _userManager;
    private readonly IdentityDbContext _dbContext;
    private readonly IRequestContext  _requestContext;

    public SearchUsersQueryHandler(UserManager<User> userManager, 
        IdentityDbContext dbContext, 
        IRequestContext requestContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _requestContext = requestContext;
    }

    public async ValueTask<PagedResponse<UserDto>> Handle(SearchUsersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        
        IQueryable<User> users = _userManager.Users.AsNoTracking();
        
        // Apply filter
        if (!string.IsNullOrEmpty(query.Search))
        {
            string term = query.Search.ToLower();
            users = users.Where(u =>
                (u.FirstName != null && u.FirstName.ToLower().Contains(term)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(term)) ||
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.UserName != null && u.UserName.ToLower().Contains(term)));
        }
        if (query.IsActive.HasValue)
        {
            users = users.Where(u => u.IsActive == query.IsActive.Value);
        }

        if (query.EmailConfirmed.HasValue)
        {
            users = users.Where(u => u.EmailConfirmed == query.EmailConfirmed.Value);
        }
        
        if (!string.IsNullOrWhiteSpace(query.RoleId))
        {
            var userIdsInRole = await _dbContext.UserRoles
                .Where(ur => ur.RoleId == query.RoleId)
                .Select(ur => ur.UserId)
                .ToListAsync(cancellationToken);

            users = users.Where(u => userIdsInRole.Contains(u.Id));
        }
        
        //Apply sort
        users = users.ApplySorting(query.Sort,SortableFields,u => u.Id);
        
        // Project to DTO
        var projected = users.Select(u => new UserDto
        {
            Id = u.Id,
            UserName = u.UserName,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            IsActive = u.IsActive,
            EmailConfirmed = u.EmailConfirmed,
            PhoneNumber = u.PhoneNumber,
            ImageUrl = u.ImageUrl != null ? u.ImageUrl.ToString() : null
        });
        
        var pagedResult = await projected.ToPagedResponseAsync(query, cancellationToken).ConfigureAwait(false);
        
        // Resolve image URLs in place — the page is already-materialized UserDto instances we own,
        // so there is no need to allocate a second full list reconstructing every DTO.
        foreach (var u in pagedResult.Items)
        {
            u.ImageUrl = ResolveImageUrl(u.ImageUrl);
        }

        return pagedResult;
    }
    
    private static readonly Dictionary<string, Expression<Func<User, object?>>> SortableFields = 
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["firstname"] = u => u.FirstName,
        ["lastname"] = u => u.LastName,
        ["email"] = u => u.Email,
        ["username"] = u => u.UserName,
        ["isactive"] = u => u.IsActive
    };
    
    private string? ResolveImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
        {
            return imageUrl;
        }

        var origin = _requestContext.Origin;
        if (string.IsNullOrEmpty(origin))
        {
            return imageUrl;
        }

        var relativePath = imageUrl.TrimStart('/');
        return $"{origin}/{relativePath}";
    }
}