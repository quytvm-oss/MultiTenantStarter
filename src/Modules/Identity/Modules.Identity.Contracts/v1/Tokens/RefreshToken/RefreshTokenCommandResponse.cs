namespace Modules.Identity.Contracts.v1.Tokens.RefreshToken;

public sealed record RefreshTokenCommandResponse(
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);