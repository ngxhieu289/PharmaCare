import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import client from '@/api/client'
import PrintableInvoice from '@/components/PrintableInvoice'
import type { ApiError, OrderResponse } from '@/types/api'
import { dateTime, money, statusClass, statusLabel } from '@/utils/format'

export default function InternalOrderDetail() {
  const { id } = useParams()
  const [order, setOrder] = useState<OrderResponse | null>(null)
  const [note, setNote] = useState('')
  const [reference, setReference] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const load = useCallback(() => client.get<OrderResponse>(`/orders/${id}`).then((response) => setOrder(response.data)), [id])

  useEffect(() => { load().catch((reason) => setError(reason.message)) }, [load])

  async function action(path: string, body: object) {
    setBusy(true); setError('')
    try { await client.post(`/orders/${id}/${path}`, body); await load() }
    catch (reason) { setError((reason as ApiError).message) }
    finally { setBusy(false) }
  }

  if (!order) return <p>{error || 'Đang tải…'}</p>
  return <section className="order-page">
    <div className="print:hidden"><div className="flex flex-wrap items-center justify-between gap-3"><Link to="/internal/orders" className="text-sm font-semibold text-primary-700">← Danh sách đơn</Link><button type="button" onClick={() => window.print()} className="rounded-xl border border-primary-300 bg-white px-5 py-2 font-semibold text-primary-700">🖨 In hóa đơn</button></div>
      {order.status !== 'COMPLETED' && <p className="mt-3 rounded-xl bg-amber-50 p-3 text-sm text-amber-800">Hóa đơn có thể in để đối chiếu, nhưng trạng thái hiện tại là “{statusLabel[order.status]}”. Nên in bản giao khách sau khi hoàn tất đơn.</p>}
      {error && <p className="mt-4 rounded-xl bg-red-50 p-3 text-red-700">{error}</p>}
      <div className="mt-5 grid gap-6 xl:grid-cols-[1fr_380px]"><div className="space-y-5"><article className="rounded-2xl bg-white p-6 shadow-sm"><div className="flex justify-between"><div><h1 className="text-2xl font-bold">{order.code}</h1><p className="text-sm text-slate-500">{order.customerName} · {dateTime(order.createdAt)}</p><p className="text-sm text-slate-500">{order.branchName} · {order.branchAddress}</p></div><span className={`h-fit rounded-full px-3 py-1 text-sm ${statusClass(order.status)}`}>{statusLabel[order.status]}</span></div><div className="mt-5 divide-y">{order.items.map((item) => <div key={item.id} className="flex justify-between py-4"><div><strong>{item.productName}</strong><p className="text-sm text-slate-500">{item.quantity} {item.saleUnitName} · Lô {item.batchNumber} · HSD {new Date(item.expiryDate).toLocaleDateString('vi-VN')} · VAT {item.vatRate}%</p></div><strong>{money(item.lineTotal)}</strong></div>)}</div></article>
        <article className="rounded-2xl bg-white p-6 shadow-sm"><h2 className="font-bold">Lịch sử xử lý</h2>{order.statusHistory.map((history, index) => <div key={index} className="mt-4 border-l-2 border-primary-200 pl-4"><strong>{statusLabel[history.toStatus]}</strong><p className="text-xs text-slate-500">{dateTime(history.changedAt)} · {history.changedByName}</p>{history.note && <p>{history.note}</p>}</div>)}</article></div>
        <aside className="h-fit rounded-2xl bg-white p-6 shadow-sm"><h2 className="text-lg font-bold">Xử lý đơn</h2><dl className="mt-4 space-y-2 text-sm"><div className="flex justify-between"><dt>Trước VAT</dt><dd>{money(order.subtotalBeforeVat)}</dd></div><div className="flex justify-between"><dt>VAT</dt><dd>{money(order.totalVatAmount)}</dd></div><div className="flex justify-between"><dt>Phí giao</dt><dd>{money(order.shippingFee)}</dd></div><div className="flex justify-between"><dt>Giảm giá</dt><dd>-{money(order.discountAmount)}</dd></div><div className="flex justify-between border-t pt-2 text-lg"><dt>Tổng tiền</dt><dd className="font-bold">{money(order.totalAmount)}</dd></div><div className="flex justify-between"><dt>Thanh toán</dt><dd>{statusLabel[order.paymentStatus]}</dd></div><div className="flex justify-between"><dt>Nhận hàng</dt><dd>{order.pickupType === 'SHIPPING' ? 'Giao tận nơi' : 'Tại nhà thuốc'}</dd></div></dl>{order.shippingAddress && <p className="mt-4 rounded-xl bg-slate-50 p-3 text-sm">{order.recipientName} · {order.recipientPhone}<br />{order.shippingAddress}</p>}<textarea value={note} onChange={(event) => setNote(event.target.value)} placeholder="Ghi chú xử lý (khách sẽ thấy trong lịch sử trạng thái)" className="mt-5 w-full rounded-xl border p-3" />{order.status === 'PENDING' && <button disabled={busy} onClick={() => action('confirm', { note: note || 'Nhà thuốc đã xác nhận và đang chuẩn bị đơn.' })} className="mt-3 w-full rounded-xl bg-primary-600 py-3 font-semibold text-white">Xác nhận đơn</button>}{order.paymentStatus === 'UNPAID' && <><input value={reference} onChange={(event) => setReference(event.target.value)} placeholder="Mã giao dịch (nếu có)" className="mt-3 w-full rounded-xl border p-3" /><button disabled={busy} onClick={() => action('payments/confirm', { externalReference: reference || null, note: note || null })} className="mt-3 w-full rounded-xl border border-emerald-300 py-3 font-semibold text-emerald-700">Xác nhận đã thanh toán</button></>}{order.status === 'CONFIRMED' && <button disabled={busy} onClick={() => action('complete', { note: note || 'Đơn đã hoàn tất và sẵn sàng giao/đã giao cho khách.' })} className="mt-3 w-full rounded-xl bg-emerald-600 py-3 font-semibold text-white">Hoàn tất và xuất kho</button>}{(order.status === 'PENDING' || order.status === 'CONFIRMED') && <button disabled={busy} onClick={() => action('cancel', { note: note || 'Nhân viên hủy đơn' })} className="mt-3 w-full rounded-xl border border-red-200 py-3 font-semibold text-red-600">Hủy đơn</button>}</aside></div>
    </div>
    <PrintableInvoice order={order} />
  </section>
}
