namespace PharmaCare.Api.Services;

public sealed class PrescriptionStorageSettings
{
    public const string SectionName = "PrescriptionStorage";

    public string Directory { get; set; } = "Storage/Prescriptions";
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
}
