namespace PharmaCare.Api.Services;

public sealed class DemoAccountsSettings
{
    public const string SectionName = "DemoAccounts";
    public bool Enabled { get; set; }
    public string PharmacistPassword { get; set; } = string.Empty;
    public string WarehousePassword { get; set; } = string.Empty;
    public string BranchManagerPassword { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
}
