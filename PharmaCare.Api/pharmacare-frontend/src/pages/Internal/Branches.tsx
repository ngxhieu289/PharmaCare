import { useEffect, useState } from 'react'
import client from '@/api/client'
import { useAuthStore } from '@/auth/store'
import type { BranchResponse } from '@/types/api'

export default function Branches() {
  const assigned=useAuthStore(s=>s.user?.branches??[]);const [items,setItems]=useState<BranchResponse[]>([]);const [error,setError]=useState('')
  useEffect(()=>{Promise.all(assigned.map(b=>client.get<BranchResponse>(`/branches/${b.id}`))).then(rows=>setItems(rows.map(r=>r.data))).catch(e=>setError(e.message))},[assigned])
  return <section><p className="text-sm text-slate-500">Phạm vi quản lý</p><h1 className="text-3xl font-bold">Chi nhánh của tôi</h1>{error&&<p className="mt-4 rounded-xl bg-red-50 p-3 text-red-700">{error}</p>}<div className="mt-6 grid gap-5 lg:grid-cols-2">{items.map(b=><article key={b.id} className="rounded-2xl bg-white p-6 shadow-sm"><div className="flex justify-between"><div><span className="text-sm font-semibold text-primary-700">{b.code}</span><h2 className="mt-1 text-xl font-bold">{b.name}</h2></div><span className={`h-fit rounded-full px-3 py-1 text-xs ${b.isActive?'bg-emerald-50 text-emerald-700':'bg-red-50 text-red-700'}`}>{b.isActive?'Đang hoạt động':'Ngừng hoạt động'}</span></div><dl className="mt-5 space-y-3 text-sm"><div><dt className="text-slate-500">Địa chỉ</dt><dd>{b.address}</dd></div><div><dt className="text-slate-500">Khu vực</dt><dd>{[b.ward,b.district,b.province].filter(Boolean).join(', ')}</dd></div><div><dt className="text-slate-500">Điện thoại</dt><dd>{b.phone||'Chưa cập nhật'}</dd></div></dl></article>)}</div><p className="mt-5 rounded-xl bg-blue-50 p-4 text-sm text-blue-800">Việc tạo, khóa hoặc thay đổi thông tin pháp lý của chi nhánh thuộc quyền Admin. Quản lý chi nhánh vận hành đơn hàng, kho, voucher và báo cáo trong phạm vi được phân công.</p></section>
}
