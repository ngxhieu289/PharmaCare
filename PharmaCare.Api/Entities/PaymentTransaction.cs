using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("payment_transactions")]
public class PaymentTransaction
{
    [Key, Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("order_id")]
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    [Required, MaxLength(30), Column("transaction_type")]
    public string TransactionType { get; set; } = PaymentTransactionTypes.Payment;

    [Required, MaxLength(30), Column("method")]
    public string Method { get; set; } = PaymentMethods.Cod;

    [Column("amount")]
    public decimal Amount { get; set; }

    [Required, MaxLength(30), Column("status")]
    public string Status { get; set; } = PaymentTransactionStatuses.Succeeded;

    [MaxLength(100), Column("external_reference")]
    public string? ExternalReference { get; set; }

    [MaxLength(500), Column("note")]
    public string? Note { get; set; }

    [Column("created_by")]
    public Guid CreatedBy { get; set; }
    public User CreatedByUser { get; set; } = null!;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class PaymentMethods
{
    public const string Cod = "COD";
    public const string VietQr = "VIETQR";
    public const string CashPos = "CASH_POS";
}

public static class PaymentStatuses
{
    public const string Unpaid = "UNPAID";
    public const string Paid = "PAID";
    public const string Refunded = "REFUNDED";
}

public static class PaymentTransactionTypes
{
    public const string Payment = "PAYMENT";
    public const string Refund = "REFUND";
}

public static class PaymentTransactionStatuses
{
    public const string Succeeded = "SUCCEEDED";
}
