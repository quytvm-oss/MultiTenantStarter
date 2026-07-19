using Mediator;

using Shared.Storage;

namespace Modules.Identity.Contracts.v1.Users.UpdateUser;

public class UpdateUserCommand : ICommand<Unit>
{
    public string Id { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public StreamUploadRequest? Image { get; set; }
    public bool DeleteCurrentImage { get; set; }
}