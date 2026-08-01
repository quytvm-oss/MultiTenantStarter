using Mediator;

namespace Modules.Identity.Contracts.v1.Groups.RemoveUserFromGroup;

public record RemoveUserFromGroupCommand(Guid GroupId, string UserId) : ICommand<Unit>;