import { useEffect, useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import client from '@/api/client'
import { useCartStore } from '@/cart/store'
import type { BranchResponse, PagedResponse, ProductAvailability, ProductResponse } from '@/types/api'
import { money } from '@/utils/format'

const fallback = (value: string | null | undefined) => value?.trim() || 'Đang cập nhật'

export default function ProductDetail() {
  const { id } = useParams()
  const add = useCartStore((state) => state.add)
  const branchId = useCartStore((state) => state.branchId)
  const setBranch = useCartStore((state) => state.setBranch)
  const branchRef = useRef<HTMLSelectElement>(null)
  const [product, setProduct] = useState<ProductResponse | null>(null)
  const [branches, setBranches] = useState<BranchResponse[]>([])
  const [stock, setStock] = useState<ProductAvailability | null>(null)
  const [error, setError] = useState('')
  const [selectedUnitId, setSelectedUnitId] = useState('')
  const [quantity, setQuantity] = useState(1)

  useEffect(() => {
    Promise.all([
      client.get<ProductResponse>(`/products/${id}`),
      client.get<PagedResponse<BranchResponse>>('/branches', { params: { pageSize: 100 } }),
    ]).then(([productResponse, branchResponse]) => {
      setProduct(productResponse.data)
      setSelectedUnitId(productResponse.data.saleUnits.find((unit) => unit.isDefault)?.id ?? productResponse.data.saleUnits[0]?.id ?? '')
      setBranches(branchResponse.data.items)
      if (!branchId && branchResponse.data.items[0]) setBranch(branchResponse.data.items[0].id)
    }).catch((reason) => setError(reason.message))
  }, [id, branchId, setBranch])

  useEffect(() => {
    if (id && branchId && selectedUnitId) {
      client.get<ProductAvailability>(`/products/${id}/availability`, { params: { branchId, saleUnitId: selectedUnitId } })
        .then((response) => setStock(response.data)).catch(() => setStock(null))
    }
  }, [id, branchId, selectedUnitId])

  if (error) return <p className="rounded-xl bg-red-50 p-4 text-red-600">{error}</p>
  if (!product) return <p>Đang tải thông tin sản phẩm…</p>
  const selectedUnit = product.saleUnits.find((unit) => unit.id === selectedUnitId)
  const detailRows = [
    ['Tên chính hãng', product.name], ['Số đăng ký', product.registrationNumber],
    ['Thành phần', product.composition ?? product.activeIngredient], ['Dạng bào chế', product.dosageForm],
    ['Quy cách', product.packaging], ['Danh mục', product.categoryName], ['Thương hiệu', product.brand],
    ['Nhà sản xuất', product.manufacturer], ['Nước sản xuất', product.countryOfOrigin], ['Hạn sử dụng', product.shelfLife],
  ]

  return <section className="pb-12">
    <Link className="text-sm font-medium text-primary-700" to="/">← Quay lại danh mục</Link>
    <div className="mt-5 grid gap-9 rounded-3xl bg-white p-5 shadow-sm md:p-8 lg:grid-cols-[46%_54%]">
      <div>
        <div className="grid min-h-[420px] place-items-center rounded-2xl bg-primary-50 p-6">
          {product.imageUrl ? <img src={product.imageUrl} alt={product.name} className="max-h-[390px] w-full object-contain" /> : <div className="text-center"><span className="text-6xl font-bold text-primary-300">{product.code}</span><p className="mt-4 text-sm text-slate-500">Hình ảnh sản phẩm đang cập nhật</p></div>}
        </div>
        {product.imageUrl && <div className="mt-4 h-20 w-20 rounded-xl border-2 border-primary-600 p-1"><img src={product.imageUrl} alt="Ảnh sản phẩm" className="h-full w-full object-contain" /></div>}
        <p className="mt-3 text-xs text-slate-500">Mẫu mã sản phẩm có thể thay đổi theo lô hàng.</p>
        <div className="mt-5 grid grid-cols-3 gap-3 border-t pt-4 text-xs text-slate-600"><span>✓ Sản phẩm chính hãng</span><span>⌂ Nhận tại nhà thuốc</span><span>🚚 Giao hàng tận nơi</span></div>
      </div>
      <div>
        <div className="flex flex-wrap items-center gap-2 text-sm"><span className="rounded bg-slate-100 px-2 py-1">{fallback(product.countryOfOrigin)}</span>{product.brand && <span>Thương hiệu: <strong className="text-primary-700">{product.brand}</strong></span>}</div>
        {product.rxFlag && <span className="mt-4 inline-block rounded-full bg-red-50 px-3 py-1 text-sm font-semibold text-red-700">Thuốc kê đơn · Cần tư vấn dược sĩ</span>}
        <h1 className="mt-3 text-3xl font-bold leading-tight text-slate-900">{product.name}</h1>
        <p className="mt-3 text-sm text-slate-500">Mã sản phẩm: {product.code} · {product.categoryName}</p>
        <p className="mt-5 text-4xl font-bold text-primary-700">{money(selectedUnit?.salePrice ?? product.unitPrice)} <span className="text-xl font-medium">/ {selectedUnit?.unitName ?? product.packaging}</span></p>
        <div className="mt-6 flex items-center gap-5"><span className="w-32 text-slate-600">Chọn đơn vị tính</span><div className="flex min-w-52 rounded-xl bg-slate-100 p-1">{product.saleUnits.map((unit) => <button key={unit.id} onClick={() => { setSelectedUnitId(unit.id); setQuantity(1) }} className={`flex-1 rounded-lg px-4 py-2 ${selectedUnitId === unit.id ? 'bg-white font-semibold text-primary-700 shadow-sm' : 'text-slate-500'}`}>{unit.unitName}</button>)}</div></div>
        <div className="mt-4 flex items-center gap-5"><span className="w-32 text-slate-600">Chọn số lượng</span><div className="flex overflow-hidden rounded-xl border"><button aria-label="Giảm số lượng" onClick={() => setQuantity((value) => Math.max(1, value - 1))} className="h-10 w-10 text-xl">−</button><span className="grid h-10 min-w-12 place-items-center border-x font-semibold">{quantity}</span><button aria-label="Tăng số lượng" onClick={() => setQuantity((value) => value + 1)} className="h-10 w-10 text-xl">+</button></div></div>
        <div className="mt-5"><label className="text-sm font-semibold">Nhà thuốc nhận hàng / kiểm tra tồn kho</label><select ref={branchRef} value={branchId ?? ''} onChange={(event) => setBranch(event.target.value)} className="mt-2 w-full rounded-xl border p-3">{branches.map((branch) => <option key={branch.id} value={branch.id}>{branch.name} — {branch.address}</option>)}</select>{stock && <p className={`mt-2 text-sm font-medium ${stock.status === 'OUT_OF_STOCK' ? 'text-red-600' : 'text-emerald-700'}`}>{stock.status === 'OUT_OF_STOCK' ? 'Chi nhánh này đang hết hàng' : `Còn ${stock.availableQuantity} ${stock.unitName ?? 'sản phẩm'} khả dụng`}</p>}</div>
        <div className="mt-6 grid gap-3 sm:grid-cols-2">{product.rxFlag ? <Link to={`/prescriptions?productId=${product.id}`} className="rounded-xl bg-primary-600 py-3 text-center font-semibold text-white">Tư vấn dược sĩ</Link> : <button disabled={stock?.status === 'OUT_OF_STOCK'} onClick={() => add(product, quantity, selectedUnit)} className="rounded-xl bg-primary-600 py-3 font-semibold text-white disabled:bg-slate-300">Chọn mua</button>}<button onClick={() => { branchRef.current?.scrollIntoView({ behavior: 'smooth', block: 'center' }); branchRef.current?.focus() }} className="rounded-xl bg-primary-50 py-3 font-semibold text-primary-700">Tìm nhà thuốc</button></div>
        <p className="mt-7 leading-7 text-slate-700">{fallback(product.indications)}</p>
        <dl className="mt-6 divide-y text-sm">{detailRows.map(([label, value]) => <div key={label} className="grid grid-cols-[140px_1fr] gap-4 py-3"><dt className="text-slate-600">{label}</dt><dd className="font-medium text-slate-800">{fallback(value)}</dd></div>)}</dl>
      </div>
    </div>
    <div className="mt-7 grid gap-6 lg:grid-cols-2">
      {[['Hướng dẫn sử dụng', product.usageInstructions], ['Chống chỉ định', product.contraindications], ['Tác dụng phụ', product.sideEffects], ['Bảo quản và cảnh báo', [product.storageTemp, product.warningText].filter(Boolean).join('. ')]].map(([title, content]) => <article key={title} className="rounded-2xl bg-white p-6 shadow-sm"><h2 className="text-xl font-bold text-slate-900">{title}</h2><p className="mt-3 whitespace-pre-line leading-7 text-slate-600">{fallback(content)}</p></article>)}
    </div>
    <p className="mt-5 rounded-xl bg-amber-50 p-4 text-sm text-amber-900">Thông tin sản phẩm chỉ có tính chất tham khảo. Đọc kỹ hướng dẫn sử dụng; với thuốc kê đơn hoặc khi có bệnh nền, hãy trao đổi với bác sĩ/dược sĩ trước khi dùng.</p>
  </section>
}
