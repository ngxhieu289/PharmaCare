import type { OrderResponse } from '@/types/api'
import { dateTime, money, statusLabel } from '@/utils/format'

const paymentNames: Record<string, string> = {
  COD: 'Thanh toán khi nhận hàng',
  VIETQR: 'Chuyển khoản VietQR',
  CASH_POS: 'Tiền mặt tại quầy',
}

export default function PrintableInvoice({ order }: { order: OrderResponse }) {
  return (
    <article id="printable-invoice" className="print-invoice hidden bg-white text-black print:block">
      <header className="border-b-2 border-black pb-4 text-center">
        <h1 className="text-2xl font-bold">NHÀ THUỐC PHARMACARE</h1>
        <p className="mt-1 font-semibold">{order.branchName} ({order.branchCode})</p>
        <p>{order.branchAddress}</p>
        {order.branchPhone && <p>Điện thoại: {order.branchPhone}</p>}
        <h2 className="mt-4 text-xl font-bold">HÓA ĐƠN BÁN HÀNG</h2>
        <p className="text-sm">Mã hóa đơn: <strong>{order.code}</strong> · Ngày: {dateTime(order.createdAt)}</p>
      </header>

      <section className="my-4 grid grid-cols-2 gap-x-8 gap-y-1 text-sm">
        <p>Khách hàng: <strong>{order.customerName}</strong></p>
        <p>Người nhận: <strong>{order.recipientName || order.customerName}</strong></p>
        <p>Số điện thoại: <strong>{order.recipientPhone || 'Không cung cấp'}</strong></p>
        <p>Hình thức: <strong>{order.orderType === 'POS' ? 'Mua tại quầy' : order.pickupType === 'SHIPPING' ? 'Giao tận nơi' : 'Nhận tại nhà thuốc'}</strong></p>
        {order.shippingAddress && <p className="col-span-2">Địa chỉ giao: <strong>{order.shippingAddress}</strong></p>}
        {order.prescriptionId && <p className="col-span-2">Đơn thuốc liên kết: <strong>{order.prescriptionId}</strong></p>}
      </section>

      <table className="w-full border-collapse text-xs">
        <thead><tr><th className="border border-black p-2">STT</th><th className="border border-black p-2 text-left">Sản phẩm</th><th className="border border-black p-2">Đơn vị</th><th className="border border-black p-2">SL</th><th className="border border-black p-2 text-right">Đơn giá</th><th className="border border-black p-2">VAT</th><th className="border border-black p-2 text-right">Thành tiền</th></tr></thead>
        <tbody>{order.items.map((item, index) => <tr key={item.id}><td className="border border-black p-2 text-center">{index + 1}</td><td className="border border-black p-2"><strong>{item.productName}</strong><br /><span>{item.productCode} · Lô {item.batchNumber} · HSD {new Date(item.expiryDate).toLocaleDateString('vi-VN')}</span></td><td className="border border-black p-2 text-center">{item.saleUnitName}</td><td className="border border-black p-2 text-center">{item.quantity}</td><td className="border border-black p-2 text-right">{money(item.unitPrice)}</td><td className="border border-black p-2 text-center">{item.vatRate}%<br />{money(item.vatAmount)}</td><td className="border border-black p-2 text-right font-semibold">{money(item.lineTotal)}</td></tr>)}</tbody>
      </table>

      <section className="ml-auto mt-4 w-80 space-y-1 text-sm">
        <div className="flex justify-between"><span>Tiền trước VAT</span><strong>{money(order.subtotalBeforeVat)}</strong></div>
        <div className="flex justify-between"><span>Thuế VAT</span><strong>{money(order.totalVatAmount)}</strong></div>
        <div className="flex justify-between"><span>Tiền hàng sau VAT</span><strong>{money(order.subtotalBeforeVat + order.totalVatAmount)}</strong></div>
        <div className="flex justify-between"><span>Phí giao hàng</span><strong>{money(order.shippingFee)}</strong></div>
        <div className="flex justify-between"><span>Giảm giá {order.voucherCode ? `(${order.voucherCode})` : ''}</span><strong>-{money(order.discountAmount)}</strong></div>
        <div className="flex justify-between border-t-2 border-black pt-2 text-lg"><span>TỔNG CỘNG</span><strong>{money(order.totalAmount)}</strong></div>
      </section>

      <section className="mt-5 text-sm">
        <p>Thanh toán: <strong>{paymentNames[order.paymentMethod] || order.paymentMethod}</strong> · {statusLabel[order.paymentStatus] || order.paymentStatus}</p>
        <p>Trạng thái đơn: <strong>{statusLabel[order.status] || order.status}</strong></p>
        {order.payments.length > 0 && <p>Mã giao dịch: <strong>{order.payments.at(-1)?.externalReference || 'Đã xác nhận tại nhà thuốc'}</strong></p>}
      </section>

      <footer className="mt-10 grid grid-cols-2 text-center text-sm"><div><strong>Khách hàng</strong><p className="mt-16">(Ký và ghi rõ họ tên)</p></div><div><strong>Dược sĩ/Nhân viên bán hàng</strong><p className="mt-16">(Ký và ghi rõ họ tên)</p></div></footer>
      <p className="mt-10 border-t pt-3 text-center text-xs">Cảm ơn quý khách đã sử dụng dịch vụ của PharmaCare. Vui lòng dùng thuốc theo hướng dẫn của dược sĩ/bác sĩ.</p>
    </article>
  )
}
