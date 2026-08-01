using Mediator;

using Modules.Identity.Contracts.DTOs;

namespace Modules.Identity.Contracts.v1.Groups.GetGroupById;

public record GetGroupByIdQuery(Guid Id) : IQuery<GroupDto>;