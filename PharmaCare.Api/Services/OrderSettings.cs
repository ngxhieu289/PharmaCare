namespace PharmaCare.Api.Services;

public sealed class OrderSettings
{
    public const string SectionName = "Orders";

    public decimal ShippingFee { get; set; } = 30000m;
}
