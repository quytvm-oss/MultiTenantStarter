using Mediator;

using Modules.Identity.Contracts.DTOs;

namespace Modules.Identity.Contracts.v1.Users.GetUserById;

public sealed record GetUserQuery(string Id) : IQuery<UserDto>;