namespace PharmaCare.Api.Services;

public sealed class BootstrapAdminSettings
{
    public const string SectionName = "BootstrapAdmin";

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = "System Administrator";
}
