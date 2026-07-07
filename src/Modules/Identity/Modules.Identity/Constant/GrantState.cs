namespace Modules.Identity.Constant;

public enum GrantState : byte
{
    Active = 0,
    EndedOrRevoked = 1,
    /// <summary>No row found for this jti — treat as revoked (defensive).</summary>
    Unknown = 2,
}