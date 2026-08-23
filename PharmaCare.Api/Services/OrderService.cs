using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PharmaCare.Api.Data;
using PharmaCare.Api.Dtos;
using PharmaCare.Api.Entities;

namespace PharmaCare.Api.Services;

public sealed class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly OrderSettings _settings;

    public OrderService(AppDbContext context, IOptions<OrderSettings> settings)
    {
        _context = context;
        _settings = settings.Value;
    }

    public async Task<Guid> CreateAsync(
        CreateOrderRequest request,
        Guid actorId,
        bool canManageOrders,
        CancellationToken cancellationToken)
    {
        var orderType = request.OrderType.Trim().ToUpperInvariant();
        var pickupType = request.PickupType.Trim().ToUpperInvariant();
        var paymentMethod = request.PaymentMethod.Trim().ToUpperInvariant();
        ValidateOrderShape(request, orderType, pickupType, paymentMethod, canManageOrders);

        var customerId = orderType == OrderTypes.Online
            ? actorId
            : request.CustomerId!.Value;
        if (!await _context.Users.AnyAsync(
            u => u.Id == customerId && u.IsActive, cancellationToken))
        {
            throw new OrderOperationException("Khách hàng không tồn tại hoặc đã bị khóa.");
        }
        if (!await _context.Branches.AnyAsync(
            b => b.Id == request.BranchId && b.IsActive, cancellationToken))
        {
            throw new OrderOperationException("Chi nhánh không tồn tại hoặc đã ngừng hoạt động.");
        }

        var requestedProducts = request.Items.Select(i => i.ProductId).Distinct().ToArray();
        var products = await _context.Products
            .Where(p => requestedProducts.Contains(p.Id) && p.IsActive)
            .ToDictionaryAsync(p => p.Id, cancellationToken);
        if (products.Count != requestedProducts.Length)
        {
            throw new OrderOperationException("Có sản phẩm không tồn tại hoặc đã ngừng bán.");
        }
        var requestedUnitIds = request.Items.Where(item => item.SaleUnitId.HasValue)
            .Select(item => item.SaleUnitId!.Value).Distinct().ToArray();
        var saleUnits = await _context.ProductSaleUnits
            .Where(unit => requestedUnitIds.Contains(unit.Id) && unit.IsActive)
            .ToDictionaryAsync(unit => unit.Id, cancellationToken);
        if (saleUnits.Count != requestedUnitIds.Length)
        {
            throw new OrderOperationException("Có đơn vị bán không tồn tại hoặc đã ngừng sử dụng.");
        }

        await ValidatePrescription(
            request, customerId, products, saleUnits, cancellationToken);

        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        try
        {
            var order = new Order
            {
                Code = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                CustomerId = customerId,
                BranchId = request.BranchId,
                PrescriptionId = request.PrescriptionId,
                OrderType = orderType,
                PickupType = pickupType,
                Status = OrderStatuses.Pending,
                PaymentMethod = paymentMethod,
                PaymentStatus = PaymentStatuses.Unpaid,
                RecipientName = Clean(request.RecipientName),
                RecipientPhone = Clean(request.RecipientPhone),
                GuestEmail = Clean(request.GuestEmail),
                ShippingAddress = Clean(request.ShippingAddress),
                ShippingFee = pickupType == PickupTypes.Shipping ? _settings.ShippingFee : 0m,
                Version = 1
            };

            decimal subtotalBeforeVat = 0m;
            decimal totalVat = 0m;
            decimal grossTotal = 0m;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            foreach (var requestedItem in request.Items)
            {
                var product = products[requestedItem.ProductId];
                ProductSaleUnit saleUnit;
                if (requestedItem.SaleUnitId.HasValue)
                {
                    saleUnit = saleUnits[requestedItem.SaleUnitId.Value];
                    if (saleUnit.ProductId != product.Id)
                        throw new OrderOperationException("Đơn vị bán không thuộc sản phẩm đã chọn.");
                }
                else
                {
                    saleUnit = await _context.ProductSaleUnits.SingleOrDefaultAsync(
                        unit => unit.ProductId == product.Id && unit.IsDefault && unit.IsActive,
                        cancellationToken) ?? new ProductSaleUnit
                        {
                            Id = Guid.Empty,
                            ProductId = product.Id, UnitName = product.Packaging.Split(' ')[0],
                            ConversionFactor = 1, SalePrice = product.UnitPrice, IsDefault = true
                        };
                }
                var remaining = requestedItem.Quantity;
                var inventoryRows = await _context.BranchInventories
                    .Include(i => i.Batch)
                    .Where(i => i.BranchId == request.BranchId &&
                                i.ProductId == requestedItem.ProductId &&
                                i.Batch!.ExpiryDate >= today &&
                                i.QuantityOnHand > i.ReservedQuantity)
                    .OrderBy(i => i.Batch!.ExpiryDate)
                    .ThenBy(i => i.BatchId)
                    .ToListAsync(cancellationToken);

                foreach (var inventory in inventoryRows)
                {
                    if (remaining == 0) break;
                    var availableSaleUnits = (inventory.QuantityOnHand - inventory.ReservedQuantity) /
                                             saleUnit.ConversionFactor;
                    var allocated = Math.Min(remaining, availableSaleUnits);
                    if (allocated <= 0) continue;
                    var allocatedBaseQuantity = checked(allocated * saleUnit.ConversionFactor);

                    inventory.ReservedQuantity += allocatedBaseQuantity;
                    inventory.Version++;
                    remaining -= allocated;

                    var grossLine = RoundMoney(saleUnit.SalePrice * allocated);
                    var baseLine = product.VatRate == 0
                        ? grossLine
                        : RoundMoney(grossLine / (1m + product.VatRate / 100m));
                    var vatLine = grossLine - baseLine;

                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        BatchId = inventory.BatchId,
                        Quantity = allocatedBaseQuantity,
                        SaleUnitId = saleUnit.Id == Guid.Empty ? null : saleUnit.Id,
                        SaleUnitName = saleUnit.UnitName,
                        SaleQuantity = allocated,
                        UnitPrice = saleUnit.SalePrice,
                        VatRate = product.VatRate,
                        VatAmount = vatLine,
                        LineTotal = grossLine
                    });
                    AddInventoryTransaction(
                        inventory, InventoryTransactionTypes.Reserve, allocatedBaseQuantity,
                        actorId, order.Id, order.Code);

                    subtotalBeforeVat += baseLine;
                    totalVat += vatLine;
                    grossTotal += grossLine;
                }

                if (remaining > 0)
                {
                    throw new OrderOperationException(
                        $"Không đủ tồn khả dụng cho sản phẩm {product.Code}. Thiếu {remaining}.");
                }
            }

            order.SubtotalBeforeVat = RoundMoney(subtotalBeforeVat);
            order.TotalVatAmount = RoundMoney(totalVat);
            order.DiscountAmount = await RedeemVoucher(
                request.VoucherCode, customerId, grossTotal, order, cancellationToken);
            order.TotalAmount = RoundMoney(grossTotal + order.ShippingFee - order.DiscountAmount);
            if (order.TotalAmount == 0) order.PaymentStatus = PaymentStatuses.Paid;
            _context.OrderStatusHistories.Add(NewHistory(
                order.Id, null, OrderStatuses.Pending, "Tạo đơn và giữ tồn", actorId));

            _context.Orders.Add(order);
            AddAudit(actorId, "ORDER_CREATE", order, new
            {
                order.Code,
                order.BranchId,
                order.CustomerId,
                order.TotalAmount,
                ItemCount = request.Items.Count
            });

            await SaveAndCommit(transaction, cancellationToken);
            return order.Id;
        }
        catch (OverflowException)
        {
            throw new OrderOperationException("Số lượng hoặc giá trị đơn hàng vượt giới hạn.");
        }
    }

    public Task ConfirmAsync(
        Guid orderId,
        Guid actorId,
        string? note,
        CancellationToken cancellationToken) =>
        ChangeSimpleStatus(orderId, actorId, OrderStatuses.Pending,
            OrderStatuses.Confirmed, note, cancellationToken);

    public async Task CancelAsync(
        Guid orderId,
        Guid actorId,
        string? note,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var order = await LoadOrder(orderId, cancellationToken);
        if (order.Status is not (OrderStatuses.Pending or OrderStatuses.Confirmed))
        {
            throw new OrderOperationException("Chỉ có thể hủy đơn đang chờ hoặc đã xác nhận.");
        }

        foreach (var item in order.OrderItems)
        {
            var inventory = await FindInventory(order.BranchId, item, cancellationToken);
            if (inventory.ReservedQuantity < item.Quantity)
            {
                throw new OrderOperationException("Dữ liệu giữ tồn không còn nhất quán.");
            }
            inventory.ReservedQuantity -= item.Quantity;
            inventory.Version++;
            AddInventoryTransaction(
                inventory, InventoryTransactionTypes.Release, -item.Quantity,
                actorId, order.Id, order.Code);
        }

        ReverseVoucher(order);
        if (order.PaymentStatus == PaymentStatuses.Paid)
        {
            AddPayment(order, PaymentTransactionTypes.Refund, actorId, null,
                Clean(note) ?? "Hoàn tiền do hủy đơn");
            order.PaymentStatus = PaymentStatuses.Refunded;
        }

        var oldStatus = order.Status;
        SetStatus(order, OrderStatuses.Cancelled, actorId, note);
        AddAudit(actorId, "ORDER_CANCEL", order, new { From = oldStatus, order.Status });
        await SaveAndCommit(transaction, cancellationToken);
    }

    public async Task CompleteAsync(
        Guid orderId,
        Guid actorId,
        string? note,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var order = await LoadOrder(orderId, cancellationToken);
        if (order.Status != OrderStatuses.Confirmed)
        {
            throw new OrderOperationException("Chỉ có thể hoàn tất đơn đã xác nhận.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var item in order.OrderItems)
        {
            var inventory = await FindInventory(order.BranchId, item, cancellationToken);
            if (item.Batch!.ExpiryDate < today)
            {
                throw new OrderOperationException($"Lô {item.Batch.BatchNumber} đã hết hạn.");
            }
            if (inventory.ReservedQuantity < item.Quantity ||
                inventory.QuantityOnHand < item.Quantity)
            {
                throw new OrderOperationException("Tồn kho không đủ hoặc dữ liệu giữ tồn không nhất quán.");
            }

            inventory.ReservedQuantity -= item.Quantity;
            inventory.QuantityOnHand -= item.Quantity;
            inventory.Version++;
            AddInventoryTransaction(
                inventory, InventoryTransactionTypes.Sale, -item.Quantity,
                actorId, order.Id, order.Code);
        }

        if (order.PaymentMethod == PaymentMethods.VietQr &&
            order.PaymentStatus != PaymentStatuses.Paid)
        {
            throw new OrderOperationException("Đơn VIETQR phải được xác nhận thanh toán trước khi hoàn tất.");
        }
        if (order.PaymentStatus == PaymentStatuses.Unpaid)
        {
            AddPayment(order, PaymentTransactionTypes.Payment, actorId, null,
                "Thanh toán khi hoàn tất đơn");
            order.PaymentStatus = PaymentStatuses.Paid;
        }
        SetStatus(order, OrderStatuses.Completed, actorId, note);
        AddAudit(actorId, "ORDER_COMPLETE", order, new { order.Status, order.PaymentStatus });
        await SaveAndCommit(transaction, cancellationToken);
    }

    public async Task ConfirmPaymentAsync(
        Guid orderId,
        Guid actorId,
        ConfirmPaymentRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var order = await LoadOrder(orderId, cancellationToken);
        if (order.Status is OrderStatuses.Cancelled or OrderStatuses.Completed)
            throw new OrderOperationException("Không thể xác nhận thanh toán cho đơn đã kết thúc.");
        if (order.PaymentStatus != PaymentStatuses.Unpaid)
            throw new OrderOperationException("Đơn hàng đã được thanh toán hoặc hoàn tiền.");
        if (order.PaymentMethod != PaymentMethods.VietQr)
            throw new OrderOperationException("Chỉ xác nhận thủ công cho thanh toán VIETQR.");

        AddPayment(order, PaymentTransactionTypes.Payment, actorId,
            Clean(request.ExternalReference), Clean(request.Note));
        order.PaymentStatus = PaymentStatuses.Paid;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        order.Version++;
        AddAudit(actorId, "PAYMENT_CONFIRM", order,
            new { order.TotalAmount, order.PaymentMethod, request.ExternalReference });
        await SaveAndCommit(transaction, cancellationToken);
    }

    public async Task RefundPaymentAsync(
        Guid orderId,
        Guid actorId,
        string reason,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var order = await LoadOrder(orderId, cancellationToken);
        if (order.PaymentStatus != PaymentStatuses.Paid)
            throw new OrderOperationException("Chỉ có thể hoàn tiền đơn đã thanh toán.");
        if (order.Status != OrderStatuses.Completed)
            throw new OrderOperationException("Hoàn tiền thủ công chỉ áp dụng cho đơn đã hoàn tất.");

        AddPayment(order, PaymentTransactionTypes.Refund, actorId, null, Clean(reason));
        order.PaymentStatus = PaymentStatuses.Refunded;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        order.Version++;
        AddAudit(actorId, "PAYMENT_REFUND", order, new { order.TotalAmount, Reason = reason });
        await SaveAndCommit(transaction, cancellationToken);
    }

    private async Task ChangeSimpleStatus(
        Guid orderId,
        Guid actorId,
        string expectedStatus,
        string newStatus,
        string? note,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var order = await LoadOrder(orderId, cancellationToken);
        if (order.Status != expectedStatus)
        {
            throw new OrderOperationException(
                $"Đơn phải ở trạng thái {expectedStatus} để chuyển sang {newStatus}.");
        }

        SetStatus(order, newStatus, actorId, note);
        AddAudit(actorId, "ORDER_CONFIRM", order, new { order.Status });
        await SaveAndCommit(transaction, cancellationToken);
    }

    private async Task ValidatePrescription(
        CreateOrderRequest request,
        Guid customerId,
        IReadOnlyDictionary<Guid, Product> products,
        IReadOnlyDictionary<Guid, ProductSaleUnit> saleUnits,
        CancellationToken cancellationToken)
    {
        var rxItems = request.Items.Where(i => products[i.ProductId].RxFlag).ToList();
        if (rxItems.Count == 0)
        {
            return;
        }
        if (!request.PrescriptionId.HasValue)
        {
            throw new OrderOperationException("Đơn hàng có thuốc kê đơn nên cần prescription đã duyệt.");
        }

        var prescription = await _context.Prescriptions
            .AsNoTracking()
            .Include(p => p.Items)
            .SingleOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);
        if (prescription is null || prescription.Status != PrescriptionStatuses.Approved ||
            prescription.CustomerId != customerId || prescription.BranchId != request.BranchId)
        {
            throw new OrderOperationException(
                "Prescription không hợp lệ, chưa được duyệt hoặc không thuộc khách hàng/chi nhánh.");
        }

        foreach (var requestedGroup in rxItems.GroupBy(item => item.ProductId))
        {
            var productId = requestedGroup.Key;
            var approved = prescription.Items
                .SingleOrDefault(i => i.ProductId == productId);
            if (approved is null)
            {
                throw new OrderOperationException("Prescription không duyệt một trong các thuốc Rx đã chọn.");
            }

            var alreadyOrdered = await _context.OrderItems
                .Where(i => i.ProductId == productId &&
                            i.Order!.PrescriptionId == prescription.Id &&
                            i.Order.Status != OrderStatuses.Cancelled)
                .SumAsync(i => (int?)i.Quantity, cancellationToken) ?? 0;
            var requestedBaseQuantity = requestedGroup.Sum(requestedItem => checked(
                requestedItem.Quantity * (requestedItem.SaleUnitId.HasValue
                    ? saleUnits[requestedItem.SaleUnitId.Value].ConversionFactor : 1)));
            if (alreadyOrdered + requestedBaseQuantity > approved.ApprovedQuantity)
            {
                throw new OrderOperationException(
                    $"Số lượng thuốc {products[productId].Code} vượt mức được duyệt.");
            }
        }
    }

    private static void ValidateOrderShape(
        CreateOrderRequest request,
        string orderType,
        string pickupType,
        string paymentMethod,
        bool canManageOrders)
    {
        if (request.Items.Count == 0 || request.Items.Any(i => i.ProductId == Guid.Empty))
            throw new OrderOperationException("Đơn hàng phải có ít nhất một sản phẩm hợp lệ.");
        if (orderType is not (OrderTypes.Online or OrderTypes.Pos))
            throw new OrderOperationException("Loại đơn hàng không hợp lệ.");
        if (orderType == OrderTypes.Pos && (!canManageOrders || !request.CustomerId.HasValue))
            throw new OrderOperationException("Đơn POS cần quyền xử lý đơn và CustomerId.");
        if (pickupType is not (PickupTypes.Shipping or PickupTypes.StorePickup))
            throw new OrderOperationException("Hình thức nhận hàng không hợp lệ.");
        if (paymentMethod is not (PaymentMethods.Cod or PaymentMethods.VietQr or PaymentMethods.CashPos))
            throw new OrderOperationException("Phương thức thanh toán chưa được hỗ trợ.");
        if (orderType == OrderTypes.Online && paymentMethod == PaymentMethods.CashPos)
            throw new OrderOperationException("Đơn online không thể thanh toán CASH_POS.");
        if (pickupType == PickupTypes.Shipping &&
            (string.IsNullOrWhiteSpace(request.RecipientName) ||
             string.IsNullOrWhiteSpace(request.RecipientPhone) ||
             string.IsNullOrWhiteSpace(request.ShippingAddress)))
            throw new OrderOperationException("Đơn giao hàng cần đủ người nhận, số điện thoại và địa chỉ.");
    }

    private async Task<Order> LoadOrder(Guid orderId, CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Batch)
            .Include(o => o.Payments)
            .Include(o => o.VoucherUsage)
            .ThenInclude(u => u!.Voucher)
            .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new OrderOperationException("Không tìm thấy đơn hàng.");
    }

    private async Task<BranchInventory> FindInventory(
        Guid branchId,
        OrderItem item,
        CancellationToken cancellationToken) =>
        await _context.BranchInventories.SingleOrDefaultAsync(
            i => i.BranchId == branchId && i.ProductId == item.ProductId && i.BatchId == item.BatchId,
            cancellationToken) ?? throw new OrderOperationException("Không tìm thấy tồn kho đã giữ cho đơn hàng.");

    private void SetStatus(Order order, string newStatus, Guid actorId, string? note)
    {
        var oldStatus = order.Status;
        order.Status = newStatus;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        order.Version++;
        _context.OrderStatusHistories.Add(
            NewHistory(order.Id, oldStatus, newStatus, note, actorId));
    }

    private static OrderStatusHistory NewHistory(
        Guid orderId,
        string? from,
        string to,
        string? note,
        Guid actorId) => new()
    {
        OrderId = orderId,
        FromStatus = from,
        ToStatus = to,
        Note = Clean(note),
        ChangedBy = actorId
    };

    private void AddInventoryTransaction(
        BranchInventory inventory,
        string type,
        int quantity,
        Guid actorId,
        Guid orderId,
        string orderCode)
    {
        _context.InventoryTransactions.Add(new InventoryTransaction
        {
            BranchId = inventory.BranchId,
            ProductId = inventory.ProductId,
            BatchId = inventory.BatchId,
            TransactionType = type,
            Quantity = quantity,
            BalanceAfter = inventory.QuantityOnHand,
            ReferenceType = "ORDER",
            ReferenceId = orderId.ToString(),
            Note = orderCode,
            CreatedBy = actorId
        });
    }

    private void AddAudit(Guid actorId, string action, Order order, object values)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = actorId,
            Action = action,
            EntityName = nameof(Order),
            EntityId = order.Id.ToString(),
            NewValues = JsonSerializer.Serialize(values)
        });
    }

    private async Task<decimal> RedeemVoucher(
        string? requestedCode,
        Guid customerId,
        decimal merchandiseTotal,
        Order order,
        CancellationToken cancellationToken)
    {
        var code = Clean(requestedCode)?.ToUpperInvariant();
        if (code is null) return 0m;

        var now = DateTimeOffset.UtcNow;
        var voucher = await _context.Vouchers.SingleOrDefaultAsync(
            v => v.Code == code, cancellationToken)
            ?? throw new OrderOperationException("Voucher không tồn tại.");
        if (!voucher.IsActive || voucher.ValidFrom > now ||
            (voucher.ValidUntil.HasValue && voucher.ValidUntil <= now))
            throw new OrderOperationException("Voucher đã hết hạn hoặc chưa có hiệu lực.");
        if (voucher.AssignedCustomerId.HasValue && voucher.AssignedCustomerId != customerId)
            throw new OrderOperationException("Voucher không được cấp cho khách hàng này.");
        if (merchandiseTotal < voucher.MinOrderAmount)
            throw new OrderOperationException("Đơn hàng chưa đạt giá trị tối thiểu của voucher.");
        if (voucher.UsageLimit.HasValue && voucher.UsedCount >= voucher.UsageLimit)
            throw new OrderOperationException("Voucher đã hết lượt sử dụng.");

        var customerUsage = await _context.VoucherUsages.CountAsync(
            u => u.VoucherId == voucher.Id && u.CustomerId == customerId &&
                 u.Status == VoucherUsageStatuses.Redeemed, cancellationToken);
        if (customerUsage >= voucher.PerCustomerLimit)
            throw new OrderOperationException("Khách hàng đã dùng hết lượt của voucher.");

        var discount = voucher.DiscountType == VoucherDiscountTypes.Percentage
            ? RoundMoney(merchandiseTotal * voucher.DiscountValue / 100m)
            : voucher.DiscountValue;
        if (voucher.MaxDiscountAmount.HasValue)
            discount = Math.Min(discount, voucher.MaxDiscountAmount.Value);
        discount = RoundMoney(Math.Min(discount, merchandiseTotal));
        if (discount <= 0) throw new OrderOperationException("Voucher không tạo ra mức giảm hợp lệ.");

        voucher.UsedCount++;
        voucher.Version++;
        order.VoucherCode = voucher.Code;
        _context.VoucherUsages.Add(new VoucherUsage
        {
            VoucherId = voucher.Id,
            OrderId = order.Id,
            CustomerId = customerId,
            DiscountAmount = discount
        });
        return discount;
    }

    private static void ReverseVoucher(Order order)
    {
        var usage = order.VoucherUsage;
        if (usage is null || usage.Status != VoucherUsageStatuses.Redeemed) return;
        usage.Status = VoucherUsageStatuses.Reversed;
        usage.ReversedAt = DateTimeOffset.UtcNow;
        usage.Voucher.UsedCount--;
        usage.Voucher.Version++;
    }

    private void AddPayment(
        Order order,
        string transactionType,
        Guid actorId,
        string? externalReference,
        string? note)
    {
        if (order.TotalAmount <= 0) return;
        _context.PaymentTransactions.Add(new PaymentTransaction
        {
            OrderId = order.Id,
            TransactionType = transactionType,
            Method = order.PaymentMethod,
            Amount = order.TotalAmount,
            ExternalReference = externalReference,
            Note = note,
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
            throw new OrderOperationException("Dữ liệu đơn hàng hoặc tồn kho vừa thay đổi. Vui lòng thử lại.");
        }
        catch (DbUpdateException)
        {
            throw new OrderOperationException("Không thể lưu đơn do xung đột dữ liệu.");
        }
    }

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
