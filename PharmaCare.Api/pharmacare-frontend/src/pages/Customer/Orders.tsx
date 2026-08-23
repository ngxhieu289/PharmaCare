import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import client from '@/api/client'
import type { PagedResponse, OrderResponse } from '@/types/api'
import { dateTime, money, statusClass, statusLabel } from '@/utils/format'

export default function Orders() {
  const [orders, setOrders] = useState<OrderResponse[]>([]); const [status, setStatus] = useState(''); const [error, setError] = useState('')
  useEffect(() => { client.get<PagedResponse<OrderResponse>>('/orders', { params: { status: status || undefined, pageSize: 100 } }).then((response) => setOrders(response.data.items)).catch((reason) => setError(reason.message)) }, [status])
  return <section><div className="flex items-center justify-between"><h1 className="text-2xl font-bold">Đơn hàng của tôi</h1><select value={status} onChange={(e) => setStatus(e.target.value)} className="rounded-xl border p-2"><option value="">Tất cả trạng thái</option><option value="PENDING">Đang chờ</option><option value="CONFIRMED">Đã xác nhận</option><option value="COMPLETED">Hoàn tất</option><option value="CANCELLED">Đã hủy</option></select></div>{error && <p className="mt-5 text-red-600">{error}</p>}<div className="mt-6 space-y-4">{orders.length === 0 && <div className="rounded-2xl bg-white p-10 text-center text-slate-500">Không có đơn hàng.</div>}{orders.map((order) => <Link to={`/orders/${order.id}`} key={order.id} className="block rounded-2xl bg-white p-5 shadow-sm transition hover:shadow-md"><div className="flex flex-wrap items-start justify-between gap-3"><div><h2 className="font-bold text-primary-800">{order.code}</h2><p className="mt-1 text-sm text-slate-500">{order.branchCode} · {dateTime(order.createdAt)} · {order.items.length} dòng sản phẩm</p></div><div className="text-right"><span className={`rounded-full px-3 py-1 text-xs font-semibold ${statusClass(order.status)}`}>{statusLabel[order.status]}</span><p className="mt-2 font-bold">{money(order.totalAmount)}</p></div></div></Link>)}</div></section>
}
