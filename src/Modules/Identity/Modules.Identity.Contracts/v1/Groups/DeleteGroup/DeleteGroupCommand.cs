using Mediator;

namespace Modules.Identity.Contracts.v1.Groups.DeleteGroup;

public record DeleteGroupCommand(Guid Id) : ICommand<Unit>;