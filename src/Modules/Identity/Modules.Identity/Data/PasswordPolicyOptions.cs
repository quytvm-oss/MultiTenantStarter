namespace Modules.Identity.Data;

public class PasswordPolicyOptions
{
    public int PasswordHistoryCount { get; set; } = 5;

    public int PasswordExpiryDays { get; set; } = 90;

    public int PasswordExpiryWarningDays { get; set; } = 14;

    public bool EnforcePasswordExpiry { get; set; } = true;
}