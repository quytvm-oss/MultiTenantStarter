using Mediator;

using Modules.Identity.Contracts.DTOs;

namespace Modules.Identity.Contracts.v1.Groups.GetGroups;

public record GetGroupsQuery(string? SearchTerm = null) : IQuery<IEnumerable<GroupDto>>;