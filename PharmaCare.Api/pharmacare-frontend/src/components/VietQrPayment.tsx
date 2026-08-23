import type { OrderResponse } from '@/types/api'
import { money } from '@/utils/format'

export default function VietQrPayment({ order }: { order: OrderResponse }) {
  if (order.paymentMethod !== 'VIETQR' || order.paymentStatus === 'PAID') return null
  const bankId = import.meta.env.VITE_VIETQR_BANK_ID || '970422'
  const accountNo = import.meta.env.VITE_VIETQR_ACCOUNT_NO || '0000000000'
  const accountName = import.meta.env.VITE_VIETQR_ACCOUNT_NAME || 'PHARMACARE DEMO'
  const demo = !import.meta.env.VITE_VIETQR_ACCOUNT_NO
  const transferContent = order.code.replace(/[^a-zA-Z0-9 ]/g, '')
  const params = new URLSearchParams({ amount: String(Math.round(order.totalAmount)), addInfo: transferContent, accountName })
  const url = `https://img.vietqr.io/image/${encodeURIComponent(bankId)}-${encodeURIComponent(accountNo)}-compact2.png?${params}`
  return <section className="mt-5 rounded-2xl border border-primary-100 bg-primary-50 p-5 text-center"><h2 className="text-lg font-bold text-primary-900">Quét mã để chuyển khoản</h2><img src={url} alt={`Mã VietQR thanh toán đơn ${order.code}`} className="mx-auto mt-3 w-full max-w-72 rounded-xl bg-white" /><p className="mt-3 font-bold text-primary-800">{money(order.totalAmount)}</p><p className="text-sm text-slate-600">Nội dung: <strong>{transferContent}</strong></p>{demo && <p className="mt-3 rounded-lg bg-amber-100 p-2 text-xs text-amber-800">QR thử nghiệm. Hãy cấu hình tài khoản ngân hàng thật trong biến môi trường VITE_VIETQR_* trước khi nhận thanh toán.</p>}<p className="mt-3 text-xs text-slate-500">Sau khi chuyển khoản, nhân viên sẽ đối chiếu và xác nhận thanh toán.</p></section>
}
