using PharmaCare.Api.Dtos;

namespace PharmaCare.Api.Services;

public interface IInventoryService
{
    Task ReceiveAsync(ReceiveInventoryRequest request, Guid actorId, CancellationToken cancellationToken);
    Task AdjustAsync(AdjustInventoryRequest request, Guid actorId, CancellationToken cancellationToken);
    Task TransferAsync(TransferInventoryRequest request, Guid actorId, CancellationToken cancellationToken);
}

public sealed class InventoryOperationException : Exception
{
    public InventoryOperationException(string message) : base(message)
    {
    }
}
