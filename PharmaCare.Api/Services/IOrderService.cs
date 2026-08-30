using PharmaCare.Api.Dtos;

namespace PharmaCare.Api.Services;

public interface IOrderService
{
    Task<Guid> CreateAsync(
        CreateOrderRequest request,
        Guid actorId,
        bool canManageOrders,
        CancellationToken cancellationToken);

    Task ConfirmAsync(Guid orderId, Guid actorId, string? note, CancellationToken cancellationToken);
    Task CancelAsync(Guid orderId, Guid actorId, string? note, CancellationToken cancellationToken);
    Task CompleteAsync(Guid orderId, Guid actorId, string? note, CancellationToken cancellationToken);
    Task ConfirmPaymentAsync(Guid orderId, Guid actorId, ConfirmPaymentRequest request, CancellationToken cancellationToken);
    Task RefundPaymentAsync(Guid orderId, Guid actorId, string reason, CancellationToken cancellationToken);
}

public sealed class OrderOperationException : Exception
{
    public OrderOperationException(string message) : base(message)
    {
    }
}
