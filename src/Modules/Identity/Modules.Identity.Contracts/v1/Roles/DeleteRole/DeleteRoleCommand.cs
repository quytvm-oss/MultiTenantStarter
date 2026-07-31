using Mediator;

namespace Modules.Identity.Contracts.v1.Roles.DeleteRole;

public record DeleteRoleCommand(string Id) : ICommand<Unit>;