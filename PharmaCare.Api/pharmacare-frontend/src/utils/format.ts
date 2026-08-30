export const money = (value: number) => `${value.toLocaleString('vi-VN')} ₫`
export const dateTime = (value: string) => new Intl.DateTimeFormat('vi-VN', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
export const statusLabel: Record<string, string> = {
  PENDING: 'Đang chờ', APPROVED: 'Đã duyệt', REJECTED: 'Từ chối',
  CONFIRMED: 'Đã xác nhận', COMPLETED: 'Hoàn tất', CANCELLED: 'Đã hủy',
  UNPAID: 'Chưa thanh toán', PAID: 'Đã thanh toán', REFUNDED: 'Đã hoàn tiền',
}
export const statusClass = (status: string) => status === 'COMPLETED' || status === 'APPROVED' || status === 'PAID'
  ? 'bg-emerald-50 text-emerald-700' : status === 'CANCELLED' || status === 'REJECTED' || status === 'REFUNDED'
    ? 'bg-red-50 text-red-700' : 'bg-amber-50 text-amber-700'
