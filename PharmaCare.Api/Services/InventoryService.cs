using System.Data;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Api.Data;
using PharmaCare.Api.Dtos;
using PharmaCare.Api.Entities;

namespace PharmaCare.Api.Services;

public sealed class InventoryService : IInventoryService
{
    private readonly AppDbContext _context;

    public InventoryService(AppDbContext context) => _context = context;

    public async Task ReceiveAsync(
        ReceiveInventoryRequest request,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        await ValidateReferences(request.BranchId, request.ProductId, request.BatchId,
            rejectExpiredBatch: true, cancellationToken);

        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        try
        {
            var inventory = await FindInventory(
                request.BranchId, request.ProductId, request.BatchId, cancellationToken);
            if (inventory is null)
            {
                inventory = new BranchInventory
                {
                    BranchId = request.BranchId,
                    ProductId = request.ProductId,
                    BatchId = request.BatchId,
                    ReorderLevel = request.ReorderLevel,
                    Version = 1
                };
                _context.BranchInventories.Add(inventory);
            }
            else
            {
                inventory.Version++;
            }

            inventory.QuantityOnHand = checked(inventory.QuantityOnHand + request.Quantity);
            inventory.ReorderLevel = request.ReorderLevel;
            AddTransaction(inventory, InventoryTransactionTypes.Import, request.Quantity,
                actorId, "GOODS_RECEIPT", null, request.Note);

            await SaveAndCommit(transaction, cancellationToken);
        }
        catch (OverflowException)
        {
            throw new InventoryOperationException("Số lượng tồn kho vượt giới hạn cho phép.");
        }
    }

    public async Task AdjustAsync(
        AdjustInventoryRequest request,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        if (request.QuantityDelta == 0)
        {
            throw new InventoryOperationException("Số lượng điều chỉnh phải khác 0.");
        }
        await ValidateReferences(request.BranchId, request.ProductId, request.BatchId,
            rejectExpiredBatch: false, cancellationToken);

        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var inventory = await FindInventory(
            request.BranchId, request.ProductId, request.BatchId, cancellationToken)
            ?? throw new InventoryOperationException("Không tìm thấy tồn kho cần điều chỉnh.");

        int newQuantity;
        try
        {
            newQuantity = checked(inventory.QuantityOnHand + request.QuantityDelta);
        }
        catch (OverflowException)
        {
            throw new InventoryOperationException("Số lượng tồn kho vượt giới hạn cho phép.");
        }

        if (newQuantity < inventory.ReservedQuantity)
        {
            throw new InventoryOperationException(
                "Không thể giảm tồn thấp hơn số lượng đang được giữ cho đơn hàng.");
        }

        inventory.QuantityOnHand = newQuantity;
        inventory.Version++;
        var type = request.QuantityDelta > 0
            ? InventoryTransactionTypes.AdjustIn
            : InventoryTransactionTypes.AdjustOut;
        AddTransaction(inventory, type, request.QuantityDelta,
            actorId, "MANUAL_ADJUSTMENT", null, request.Reason);

        await SaveAndCommit(transaction, cancellationToken);
    }

    public async Task TransferAsync(
        TransferInventoryRequest request,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        if (request.FromBranchId == request.ToBranchId)
        {
            throw new InventoryOperationException("Chi nhánh nguồn và đích phải khác nhau.");
        }
        await ValidateReferences(request.FromBranchId, request.ProductId, request.BatchId,
            rejectExpiredBatch: true, cancellationToken);
        if (!await _context.Branches.AnyAsync(
            b => b.Id == request.ToBranchId && b.IsActive, cancellationToken))
        {
            throw new InventoryOperationException("Chi nhánh nhận không tồn tại hoặc đã ngừng hoạt động.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var source = await FindInventory(
            request.FromBranchId, request.ProductId, request.BatchId, cancellationToken)
            ?? throw new InventoryOperationException("Chi nhánh nguồn không có lô thuốc này.");

        if (source.QuantityOnHand - source.ReservedQuantity < request.Quantity)
        {
            throw new InventoryOperationException("Số lượng khả dụng tại chi nhánh nguồn không đủ.");
        }

        var destination = await FindInventory(
            request.ToBranchId, request.ProductId, request.BatchId, cancellationToken);
        if (destination is null)
        {
            destination = new BranchInventory
            {
                BranchId = request.ToBranchId,
                ProductId = request.ProductId,
                BatchId = request.BatchId,
                ReorderLevel = source.ReorderLevel,
                Version = 1
            };
            _context.BranchInventories.Add(destination);
        }
        else
        {
            destination.Version++;
        }

        source.QuantityOnHand -= request.Quantity;
        source.Version++;
        try
        {
            destination.QuantityOnHand = checked(destination.QuantityOnHand + request.Quantity);
        }
        catch (OverflowException)
        {
            throw new InventoryOperationException("Số lượng tồn kho đích vượt giới hạn cho phép.");
        }

        var referenceId = Guid.NewGuid().ToString();
        AddTransaction(source, InventoryTransactionTypes.TransferOut, -request.Quantity,
            actorId, "BRANCH_TRANSFER", referenceId, request.Note);
        AddTransaction(destination, InventoryTransactionTypes.TransferIn, request.Quantity,
            actorId, "BRANCH_TRANSFER", referenceId, request.Note);

        await SaveAndCommit(transaction, cancellationToken);
    }

    private async Task ValidateReferences(
        Guid branchId,
        Guid productId,
        Guid batchId,
        bool rejectExpiredBatch,
        CancellationToken cancellationToken)
    {
        if (!await _context.Branches.AnyAsync(
            b => b.Id == branchId && b.IsActive, cancellationToken))
        {
            throw new InventoryOperationException("Chi nhánh không tồn tại hoặc đã ngừng hoạt động.");
        }
        if (!await _context.Products.AnyAsync(
            p => p.Id == productId && p.IsActive, cancellationToken))
        {
            throw new InventoryOperationException("Sản phẩm không tồn tại hoặc đã ngừng hoạt động.");
        }

        var expiryDate = await _context.Batches
            .Where(b => b.Id == batchId && b.ProductId == productId)
            .Select(b => (DateOnly?)b.ExpiryDate)
            .SingleOrDefaultAsync(cancellationToken);
        if (!expiryDate.HasValue)
        {
            throw new InventoryOperationException("Lô thuốc không tồn tại hoặc không thuộc sản phẩm.");
        }
        if (rejectExpiredBatch && expiryDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InventoryOperationException("Không thể nhập hoặc chuyển lô thuốc đã hết hạn.");
        }
    }

    private Task<BranchInventory?> FindInventory(
        Guid branchId,
        Guid productId,
        Guid batchId,
        CancellationToken cancellationToken) =>
        _context.BranchInventories.SingleOrDefaultAsync(
            i => i.BranchId == branchId && i.ProductId == productId && i.BatchId == batchId,
            cancellationToken);

    private void AddTransaction(
        BranchInventory inventory,
        string type,
        int quantity,
        Guid actorId,
        string? referenceType,
        string? referenceId,
        string? note)
    {
        _context.InventoryTransactions.Add(new InventoryTransaction
        {
            BranchId = inventory.BranchId,
            ProductId = inventory.ProductId,
            BatchId = inventory.BatchId,
            TransactionType = type,
            Quantity = quantity,
            BalanceAfter = inventory.QuantityOnHand,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedBy = actorId
        });
    }

    private async Task SaveAndCommit(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InventoryOperationException(
                "Tồn kho vừa được cập nhật bởi yêu cầu khác. Vui lòng tải lại và thử lại.");
        }
        catch (DbUpdateException)
        {
            throw new InventoryOperationException(
                "Không thể cập nhật tồn kho do xung đột dữ liệu. Vui lòng thử lại.");
        }
    }
}
