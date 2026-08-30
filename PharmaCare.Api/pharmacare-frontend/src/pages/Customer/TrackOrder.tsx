import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { useLocation } from 'react-router-dom'
import client from '@/api/client'
import type { ApiError, OrderResponse } from '@/types/api'
import { dateTime, money, statusClass, statusLabel } from '@/utils/format'
import VietQrPayment from '@/components/VietQrPayment'

const steps = ['PENDING', 'CONFIRMED', 'COMPLETED']

export default function TrackOrder() {
  const initial = useLocation().state as { code?: string; phone?: string; order?: OrderResponse } | null
  const [code, setCode] = useState(initial?.code ?? '')
  const [phone, setPhone] = useState(initial?.phone ?? '')
  const [order, setOrder] = useState<OrderResponse | null>(initial?.order ?? null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const load = useCallback(async () => {
    if (!code.trim() || !phone.trim()) return
    const response = await client.get<OrderResponse>('/orders/guest/track', { params: { code: code.trim(), phone: phone.trim() } })
    setOrder(response.data)
  }, [code, phone])

  async function submit(event: FormEvent) {
    event.preventDefault(); setLoading(true); setError('')
    try { await load() } catch (reason) { setOrder(null); setError((reason as ApiError).message) } finally { setLoading(false) }
  }

  useEffect(() => {
    if (!order || order.status === 'COMPLETED' || order.status === 'CANCELLED') return
    const timer = window.setInterval(() => load().catch(() => undefined), 15000)
    return () => window.clearInterval(timer)
  }, [load, order])

  const currentStep = order ? steps.indexOf(order.status) : -1
  return <section className="mx-auto max-w-3xl"><div className="rounded-3xl bg-white p-7 shadow-sm"><h1 className="text-2xl font-bold">Tra cứu đơn hàng</h1><p className="mt-2 text-slate-500">Dành cho khách đặt hàng không cần tài khoản.</p><form onSubmit={submit} className="mt-5 grid gap-3 sm:grid-cols-[1fr_1fr_auto]"><input required value={code} onChange={(e) => setCode(e.target.value.toUpperCase())} placeholder="Mã đơn hàng" className="rounded-xl border p-3" /><input required value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="Số điện thoại đặt hàng" className="rounded-xl border p-3" /><button disabled={loading} className="rounded-xl bg-primary-600 px-5 py-3 font-semibold text-white">{loading ? 'Đang tìm…' : 'Tra cứu'}</button></form>{error && <p className="mt-4 rounded-xl bg-red-50 p-3 text-red-700">{error}</p>}</div>
  {order && <div className="mt-5 rounded-3xl bg-white p-7 shadow-sm"><div className="flex flex-wrap justify-between gap-3"><div><h2 className="text-xl font-bold">{order.code}</h2><p className="text-sm text-slate-500">Cập nhật gần nhất: {dateTime(order.updatedAt)}</p></div><span className={`h-fit rounded-full px-3 py-1 text-sm font-semibold ${statusClass(order.status)}`}>{statusLabel[order.status]}</span></div>
    {order.status === 'CANCELLED' ? <p className="mt-5 rounded-xl bg-red-50 p-4 text-red-700">Đơn hàng đã bị hủy. Xem lịch sử bên dưới để biết ghi chú.</p> : <div className="mt-7 grid grid-cols-3 gap-2">{steps.map((step, index) => <div key={step} className={`rounded-xl p-3 text-center text-sm font-semibold ${index <= currentStep ? 'bg-primary-600 text-white' : 'bg-slate-100 text-slate-400'}`}>{index + 1}. {statusLabel[step]}</div>)}</div>}
    <div className="mt-6 grid gap-4 sm:grid-cols-2"><div className="rounded-xl bg-slate-50 p-4"><strong>Nhận hàng</strong><p>{order.pickupType === 'SHIPPING' ? 'Giao tận nơi' : 'Nhận tại nhà thuốc'}</p><p className="text-sm text-slate-500">{order.branchName}</p></div><div className="rounded-xl bg-slate-50 p-4"><strong>Thanh toán</strong><p>{money(order.totalAmount)} · {statusLabel[order.paymentStatus]}</p><p className="text-sm text-slate-500">{order.paymentMethod}</p></div></div>
    <VietQrPayment order={order} />
    <div className="mt-6"><h3 className="font-bold">Lịch sử xử lý</h3>{order.statusHistory.map((item, index) => <div key={`${item.changedAt}-${index}`} className="mt-3 border-l-2 border-primary-200 pl-4"><strong>{statusLabel[item.toStatus] || item.toStatus}</strong><p className="text-sm text-slate-500">{dateTime(item.changedAt)}</p>{item.note && <p className="text-sm">{item.note}</p>}</div>)}</div>
    <p className="mt-5 text-xs text-slate-500">Trang tự kiểm tra trạng thái mới mỗi 15 giây khi đơn đang được xử lý.</p>
  </div>}</section>
}
