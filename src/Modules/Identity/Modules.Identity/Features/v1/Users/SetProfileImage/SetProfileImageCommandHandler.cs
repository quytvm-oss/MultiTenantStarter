using Core.Context;
using Core.Exceptions;

using Mediator;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Users.SetProfileImage;

namespace Modules.Identity.Features.v1.Users.SetProfileImage;

public class SetProfileImageCommandHandler(IUserProfileService userProfileService, ICurrentUser currentUser)
    : ICommandHandler<SetProfileImageCommand>
{
    public async ValueTask<Unit> Handle(SetProfileImageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty)
            throw new UnauthorizedException("no current user");

        await userProfileService.SetImageUrlAsync(userId.ToString(),
            command.ImageUrl, cancellationToken).ConfigureAwait(false);
        
        return Unit.Value;
    }
}