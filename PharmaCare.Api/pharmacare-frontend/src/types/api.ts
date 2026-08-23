export interface TokenResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
  roles: string[]
  permissions: string[]
}

export interface UserBranch { id: string; code: string; name: string; isPrimary: boolean }
export interface AdminUser { id: string; email: string; displayName: string; phone: string | null; isActive: boolean; createdAt: string; roles: string[]; branches: UserBranch[] }
export interface RoleResponse { id: string; name: string; description: string | null; permissions: string[] }
export interface PermissionResponse { id: string; code: string; description: string | null }
export interface AuditLogResponse { id: number; userId: string; userName: string; action: string; entityName: string; entityId: string; oldValues: string | null; newValues: string | null; ipAddress: string | null; createdAt: string }
export interface CurrentUser {
  id: string; email: string; username: string | null; displayName: string; phone: string | null
  roles: string[]; permissions: string[]; branches: UserBranch[]
}

export interface ProductResponse {
  id: string; code: string; name: string; activeIngredient: string | null
  indications: string | null; brand: string | null; registrationNumber: string | null
  dosageForm: string | null; manufacturer: string | null; countryOfOrigin: string | null
  shelfLife: string | null; composition: string | null; usageInstructions: string | null
  contraindications: string | null; sideEffects: string | null
  categoryId: string; categoryName: string
  rxFlag: boolean; vatRate: number; packaging: string; unitPrice: number
  storageTemp: string | null; warningText: string | null; imageUrl: string | null
  isActive: boolean; saleUnits: ProductSaleUnit[]
}
export interface ProductSaleUnit { id: string; unitName: string; conversionFactor: number; salePrice: number; isDefault: boolean; isActive: boolean }

export interface ProductAvailability { productId: string; branchId: string; availableQuantity: number; status: 'IN_STOCK' | 'LOW_STOCK' | 'OUT_OF_STOCK'; saleUnitId: string | null; unitName: string | null }
export interface InventoryResponse { branchId: string; branchCode: string; productId: string; productCode: string; productName: string; batchId: string; batchNumber: string; expiryDate: string; quantityOnHand: number; reservedQuantity: number; availableQuantity: number; reorderLevel: number; isLowStock: boolean; isExpired: boolean; version: number }
export interface BatchResponse { id: string; productId: string; productCode: string; productName: string; batchNumber: string; mfgDate: string; expiryDate: string; costPrice: number; isExpired: boolean }
export interface InventoryTransactionResponse { id: string; branchId: string; branchCode: string; productId: string; productCode: string; batchId: string; batchNumber: string; transactionType: string; quantity: number; balanceAfter: number; referenceType: string | null; referenceId: string | null; note: string | null; createdBy: string; createdByName: string; createdAt: string }
export interface BranchResponse { id: string; code: string; name: string; address: string; phone: string | null; province: string | null; district: string | null; ward: string | null; isActive: boolean }
export interface CategoryResponse { id: string; name: string; slug: string; parentId: string | null; parentName: string | null; isActive: boolean }
export interface PrescriptionItem { id: string; productId: string; productCode: string; productName: string; approvedQuantity: number; dosage: string; instructions: string | null }
export interface PrescriptionResponse {
  id: string; customerId: string; customerName: string; branchId: string; branchCode: string; branchName: string
  imageUrl: string; patientName: string; status: 'PENDING' | 'APPROVED' | 'REJECTED'
  pharmacistId: string | null; pharmacistName: string | null; pharmacistNote: string | null
  reviewedAt: string | null; createdAt: string; items: PrescriptionItem[]
}
export interface PaymentTransaction { id: string; transactionType: string; method: string; amount: number; status: string; externalReference: string | null; note: string | null; createdBy: string; createdByName: string; createdAt: string }
export interface OrderItem { id: string; productId: string; productCode: string; productName: string; batchId: string; batchNumber: string; expiryDate: string; quantity: number; baseQuantity: number; saleUnitId: string | null; saleUnitName: string; unitPrice: number; vatRate: number; vatAmount: number; lineTotal: number }
export interface OrderStatusHistory { fromStatus: string | null; toStatus: string; note: string | null; changedBy: string; changedByName: string; changedAt: string }
export interface OrderResponse {
  id: string; code: string; customerId: string; customerName: string; branchId: string; branchCode: string; prescriptionId: string | null
  orderType: string; pickupType: string; status: string; subtotalBeforeVat: number; totalVatAmount: number
  shippingFee: number; discountAmount: number; totalAmount: number; voucherCode: string | null
  paymentMethod: string; paymentStatus: string; recipientName: string | null; recipientPhone: string | null; guestEmail: string | null
  shippingAddress: string | null; createdAt: string; updatedAt: string; items: OrderItem[]
  statusHistory: OrderStatusHistory[]; payments: PaymentTransaction[]
}
export interface VoucherValidation { code: string; isValid: boolean; discountAmount: number; message: string | null }

export interface PagedResponse<T> {
  items: T[]; page: number; pageSize: number; totalItems: number; totalPages: number
}

export interface DashboardResponse {
  period: { from: string; to: string }
  totalOrders: number; completedOrders: number; cancelledOrders: number
  revenueBeforeVat: number; salesIncludingVat: number
  grossSales: number; refundedAmount: number; netRevenue: number
  discountAmount: number; vatAmount: number; shippingRevenue: number; averageOrderValue: number
  pendingPrescriptions: number; lowStockRows: number; expiringBatchRows: number
  ordersByStatus: Array<{ status: string; count: number }>
}
export interface VoucherResponse { id: string; code: string; discountType: 'FIXED_AMOUNT' | 'PERCENTAGE'; discountValue: number; minOrderAmount: number; maxDiscountAmount: number | null; validFrom: string; validUntil: string | null; usageLimit: number | null; perCustomerLimit: number; usedCount: number; assignedCustomerId: string | null; assignedCustomerName: string | null; isActive: boolean; isCurrentlyValid: boolean }
export interface TopProductResponse { productId: string; productCode: string; productName: string; quantitySold: number; grossSales: number; orderCount: number }
export interface InventoryAlertResponse { branchId: string; branchCode: string; productId: string; productCode: string; productName: string; batchId: string; batchNumber: string; expiryDate: string; quantityOnHand: number; reservedQuantity: number; availableQuantity: number; reorderLevel: number; alertType: 'EXPIRED' | 'EXPIRING' | 'LOW_STOCK' }

export interface ApiError { status?: number; message: string; data?: unknown }
