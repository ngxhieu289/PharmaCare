import { Link, Navigate, useLocation } from 'react-router-dom'
import type { OrderResponse } from '@/types/api'
import { money } from '@/utils/format'
import VietQrPayment from '@/components/VietQrPayment'

export default function OrderSuccess() {
  const order = (useLocation().state as { order?: OrderResponse } | null)?.order
  if (!order) return <Navigate to="/" replace />
  return <section className="mx-auto max-w-xl rounded-3xl bg-white p-10 text-center shadow-sm"><div className="mx-auto grid h-16 w-16 place-items-center rounded-full bg-emerald-100 text-3xl text-emerald-700">✓</div><h1 className="mt-5 text-2xl font-bold">Đặt hàng thành công</h1><p className="mt-2 text-slate-500">Nhà thuốc sẽ liên hệ qua số điện thoại người nhận để xác nhận.</p><div className="mt-6 rounded-2xl bg-slate-50 p-5 text-left"><div className="flex justify-between"><span>Mã đơn</span><strong>{order.code}</strong></div><div className="mt-3 flex justify-between"><span>Tổng thanh toán</span><strong className="text-primary-700">{money(order.totalAmount)}</strong></div><div className="mt-3 flex justify-between"><span>Hình thức nhận</span><strong>{order.pickupType === 'SHIPPING' ? 'Giao tận nơi' : 'Nhận tại nhà thuốc'}</strong></div></div><VietQrPayment order={order} /><p className="mt-5 text-xs text-slate-500">Hãy lưu mã đơn để đối chiếu khi nhà thuốc liên hệ.</p><Link to="/" className="mt-6 inline-block rounded-xl bg-primary-600 px-6 py-3 font-semibold text-white">Tiếp tục mua sắm</Link></section>
}
