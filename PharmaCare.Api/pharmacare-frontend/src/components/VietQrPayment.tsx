import { useMemo, useState } from 'react'
import type { OrderResponse } from '@/types/api'
import { money } from '@/utils/format'

const BANK_NAME = 'MB Bank'

export default function VietQrPayment({ order }: { order: OrderResponse }) {
  const [imageFailed, setImageFailed] = useState(false)
  const paymentMethod = order.paymentMethod?.trim().toUpperCase()
  const bankId = import.meta.env.VITE_VIETQR_BANK_ID || '970422'
  const accountNo = import.meta.env.VITE_VIETQR_ACCOUNT_NO || ''
  const accountName = import.meta.env.VITE_VIETQR_ACCOUNT_NAME || ''
  const transferContent = order.code.replace(/[^a-zA-Z0-9]/g, '')
  const qrUrl = useMemo(() => {
    if (!accountNo || !accountName) return ''
    const params = new URLSearchParams({
      amount: String(Math.round(order.totalAmount)),
      addInfo: transferContent,
      accountName,
    })
    return `https://img.vietqr.io/image/${encodeURIComponent(bankId)}-${encodeURIComponent(accountNo)}-compact2.png?${params.toString()}`
  }, [accountName, accountNo, bankId, order.totalAmount, transferContent])

  if (paymentMethod !== 'VIETQR' || order.paymentStatus?.toUpperCase() === 'PAID') return null

  return (
    <section className="mt-5 rounded-2xl border border-primary-200 bg-primary-50 p-5 text-center">
      <h2 className="text-lg font-bold text-primary-900">Quét mã để chuyển khoản</h2>
      {qrUrl && !imageFailed ? (
        <img key={qrUrl} src={qrUrl} alt={`Mã VietQR thanh toán đơn ${order.code}`} referrerPolicy="no-referrer" onError={() => setImageFailed(true)} className="mx-auto mt-3 aspect-square w-full max-w-72 rounded-xl bg-white object-contain p-2" />
      ) : (
        <div className="mx-auto mt-3 max-w-72 rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
          {!accountNo || !accountName ? 'Frontend chưa nạp cấu hình tài khoản VietQR. Hãy khởi động lại Vite.' : 'Không tải được ảnh QR từ VietQR. Bạn vẫn có thể chuyển khoản bằng thông tin bên dưới.'}
        </div>
      )}
      <dl className="mx-auto mt-4 max-w-sm space-y-2 rounded-xl bg-white p-4 text-left text-sm">
        <div className="flex justify-between gap-4"><dt>Ngân hàng</dt><dd className="font-semibold">{BANK_NAME}</dd></div>
        <div className="flex justify-between gap-4"><dt>Số tài khoản</dt><dd className="font-semibold">{accountNo || 'Chưa cấu hình'}</dd></div>
        <div className="flex justify-between gap-4"><dt>Chủ tài khoản</dt><dd className="text-right font-semibold">{accountName || 'Chưa cấu hình'}</dd></div>
        <div className="flex justify-between gap-4 border-t pt-2"><dt>Số tiền</dt><dd className="font-bold text-primary-800">{money(order.totalAmount)}</dd></div>
        <div className="flex justify-between gap-4"><dt>Nội dung</dt><dd className="font-bold text-primary-800">{transferContent}</dd></div>
      </dl>
      {qrUrl && imageFailed && <a href={qrUrl} target="_blank" rel="noreferrer" className="mt-3 inline-block font-semibold text-primary-700 underline">Thử tải lại ảnh QR</a>}
      <p className="mt-3 text-xs text-slate-500">Sau khi chuyển khoản, nhân viên sẽ đối chiếu và xác nhận thanh toán.</p>
    </section>
  )
}
