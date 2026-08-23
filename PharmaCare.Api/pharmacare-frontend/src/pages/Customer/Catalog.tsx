import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import client from '@/api/client'
import type { CategoryResponse, PagedResponse, ProductResponse } from '@/types/api'
import { useCartStore } from '@/cart/store'
import { money } from '@/utils/format'

export default function Catalog() {
  const [products, setProducts] = useState<ProductResponse[]>([])
  const [search, setSearch] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [symptom, setSymptom] = useState('')
  const [rxFilter, setRxFilter] = useState('')
  const [priceRange, setPriceRange] = useState('')
  const [sort, setSort] = useState('name')
  const [categories, setCategories] = useState<CategoryResponse[]>([])
  const [page, setPage] = useState(1); const [totalPages, setTotalPages] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [selectedUnits, setSelectedUnits] = useState<Record<string, string>>({})
  const add = useCartStore((state) => state.add)
  useEffect(() => {
    const controller = new AbortController()
    const timer = window.setTimeout(() => {
      setLoading(true); setError('')
      client.get<PagedResponse<ProductResponse>>('/products', {
        params: { search: search || undefined, symptom: symptom || undefined, categoryId: categoryId || undefined, rxFlag: rxFilter || undefined, minPrice: priceRange === '100-300' ? 100000 : priceRange === '300+' ? 300000 : undefined, maxPrice: priceRange === '0-100' ? 100000 : priceRange === '100-300' ? 300000 : undefined, sort, page, pageSize: 12 }, signal: controller.signal,
      }).then(({ data }) => { setProducts(data.items); setTotalPages(data.totalPages) }).catch((reason) => {
        if (!axiosCanceled(reason)) setError(reason.message ?? 'Không tải được sản phẩm.')
      }).finally(() => setLoading(false))
    }, 250)
    return () => { window.clearTimeout(timer); controller.abort() }
  }, [search, symptom, categoryId, rxFilter, priceRange, sort, page])
  useEffect(() => { client.get<PagedResponse<CategoryResponse>>('/categories', { params: { pageSize: 100 } }).then((response) => setCategories(response.data.items)).catch(() => undefined) }, [])
  return <section>
    <div className="mb-8 rounded-3xl bg-gradient-to-r from-primary-800 to-primary-500 p-8 text-white">
      <p className="text-sm font-semibold uppercase tracking-wider text-primary-100">Nhà thuốc trực tuyến</p><h1 className="mt-2 text-3xl font-bold">Chăm sóc sức khỏe, dễ dàng hơn</h1>
      <div className="mt-6 flex max-w-3xl flex-col gap-3 sm:flex-row"><input value={search} onChange={(event) => { setSearch(event.target.value); setPage(1) }} placeholder="Tìm tên thuốc, hoạt chất, triệu chứng…" className="w-full rounded-xl border-0 px-5 py-3 text-slate-800" /></div>
    </div>
    <div className="grid gap-6 lg:grid-cols-[270px_1fr]"><aside className="h-fit rounded-2xl bg-white p-5 shadow-sm"><h2 className="border-b pb-4 text-lg font-bold">☰ Bộ lọc nâng cao</h2><label className="mt-5 block text-sm font-semibold">Loại sản phẩm<select value={categoryId} onChange={(e) => { setCategoryId(e.target.value); setPage(1) }} className="mt-2 w-full rounded-xl border p-3"><option value="">Tất cả</option>{categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select></label><label className="mt-5 block text-sm font-semibold">Triệu chứng<select value={symptom} onChange={(e) => { setSymptom(e.target.value); setPage(1) }} className="mt-2 w-full rounded-xl border p-3"><option value="">Tất cả</option>{['Đau đầu','Hạ sốt','Ho','Đau họng','Sổ mũi','Dị ứng','Đau bụng','Tiêu chảy','Trào ngược','Đau cơ','Mất nước'].map((value) => <option key={value}>{value}</option>)}</select></label><label className="mt-5 block text-sm font-semibold">Yêu cầu đơn thuốc<select value={rxFilter} onChange={(e) => { setRxFilter(e.target.value); setPage(1) }} className="mt-2 w-full rounded-xl border p-3"><option value="">Tất cả</option><option value="false">Không kê đơn</option><option value="true">Thuốc kê đơn</option></select></label><label className="mt-5 block text-sm font-semibold">Giá bán<select value={priceRange} onChange={(e) => { setPriceRange(e.target.value); setPage(1) }} className="mt-2 w-full rounded-xl border p-3"><option value="">Tất cả mức giá</option><option value="0-100">Dưới 100.000đ</option><option value="100-300">100.000đ – 300.000đ</option><option value="300+">Trên 300.000đ</option></select></label><button onClick={() => { setCategoryId(''); setSymptom(''); setRxFilter(''); setPriceRange(''); setSearch(''); setPage(1) }} className="mt-5 w-full rounded-xl bg-slate-100 py-3 text-sm font-semibold">Xóa bộ lọc</button></aside><div><div className="mb-4 flex items-center justify-between"><h2 className="text-xl font-bold">Danh sách sản phẩm</h2><select value={sort} onChange={(e) => { setSort(e.target.value); setPage(1) }} className="rounded-xl border bg-white p-2 text-sm"><option value="name">Tên sản phẩm</option><option value="price_asc">Giá thấp</option><option value="price_desc">Giá cao</option></select></div>
    {loading && <p>Đang tải sản phẩm…</p>}{error && <p className="text-red-600">{error}</p>}
    {!loading && !error && <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">{products.map((product) => <article key={product.id} className="overflow-hidden rounded-2xl bg-white shadow-sm ring-1 ring-slate-100 transition hover:-translate-y-1 hover:shadow-lg">
      <Link to={`/products/${product.id}`} className="grid h-44 place-items-center bg-primary-50">{product.imageUrl ? <img className="h-full w-full object-cover" src={product.imageUrl} alt={product.name} /> : <span className="text-3xl font-bold text-primary-300">{product.code}</span>}</Link>
      <div className="p-5">{product.rxFlag && <span className="rounded-full bg-red-50 px-2 py-1 text-xs font-semibold text-red-600">Thuốc kê đơn</span>}<h2 className="mt-3 min-h-12 font-semibold">{product.name}</h2><p className="mt-1 text-sm text-slate-500">{product.packaging}</p>
        <div className="mt-3 flex rounded-lg bg-slate-100 p-1">{product.saleUnits.map((unit) => <button key={unit.id} onClick={() => setSelectedUnits((current) => ({ ...current, [product.id]: unit.id }))} className={`flex-1 rounded-md px-2 py-1 text-xs ${(selectedUnits[product.id] ?? product.saleUnits.find((item) => item.isDefault)?.id) === unit.id ? 'bg-white font-semibold text-primary-700 shadow-sm' : 'text-slate-500'}`}>{unit.unitName}</button>)}</div>{(() => { const unit = product.saleUnits.find((item) => item.id === selectedUnits[product.id]) ?? product.saleUnits.find((item) => item.isDefault) ?? product.saleUnits[0]; return unit ? <div className="mt-4"><strong className="text-lg text-primary-700">{money(unit.salePrice)} <span className="text-sm font-normal">/ {unit.unitName}</span></strong>{product.rxFlag ? <Link to={`/prescriptions?productId=${product.id}`} className="mt-3 block w-full rounded-xl border border-primary-600 px-3 py-2 text-center text-sm font-semibold text-primary-700">Tư vấn dược sĩ</Link> : <button onClick={() => add(product, 1, unit)} className="mt-3 w-full rounded-xl bg-primary-600 px-3 py-2 text-sm font-semibold text-white hover:bg-primary-700">Chọn mua</button>}</div> : null })()}</div>
    </article>)}</div>}{!loading && totalPages > 1 && <div className="mt-8 flex items-center justify-center gap-4"><button disabled={page === 1} onClick={() => setPage((value) => value - 1)} className="rounded-lg border bg-white px-4 py-2 disabled:opacity-40">Trang trước</button><span className="text-sm">Trang {page}/{totalPages}</span><button disabled={page === totalPages} onClick={() => setPage((value) => value + 1)} className="rounded-lg border bg-white px-4 py-2 disabled:opacity-40">Trang sau</button></div>}</div></div>
  </section>
}

function axiosCanceled(reason: unknown) { return typeof reason === 'object' && reason !== null && 'code' in reason && reason.code === 'ERR_CANCELED' }
