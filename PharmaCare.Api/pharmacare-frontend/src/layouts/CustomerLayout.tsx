import { Link, Outlet, useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/auth/store'
import { useCartStore } from '@/cart/store'

export default function CustomerLayout() {
  const user = useAuthStore((state) => state.user)
  const logout = useAuthStore((state) => state.logout)
  const navigate = useNavigate()
  const internal = user?.roles.some((role) => role !== 'Customer') ?? false
  const cartCount = useCartStore((state) => state.items.reduce((sum, item) => sum + item.quantity, 0))
  async function signOut() { await logout(); navigate('/login') }
  return <div className="min-h-screen">
    <header className="sticky top-0 z-10 border-b border-primary-100 bg-white/95 shadow-sm backdrop-blur">
      <div className="mx-auto flex max-w-7xl items-center justify-between px-5 py-4">
        <Link to="/" className="flex items-center gap-2 text-xl font-bold text-primary-800"><span className="text-primary-600">✚</span> PharmaCare</Link>
        <nav className="flex items-center gap-4 text-sm"><Link to="/">Sản phẩm</Link>{user && !internal && <><Link to="/prescriptions">Đơn thuốc</Link><Link to="/orders">Đơn hàng</Link><Link to="/profile">Hồ sơ</Link></>}{internal ? <Link to="/internal" className="rounded-lg bg-primary-600 px-3 py-2 text-white">Về trang nhân viên</Link> : <Link to="/cart" className="rounded-lg bg-primary-600 px-3 py-2 text-white">Giỏ hàng ({cartCount})</Link>}{user ? <><span className="hidden text-slate-500 lg:inline">{user.displayName}</span><button onClick={signOut} className="rounded-lg bg-primary-50 px-3 py-2 text-primary-700">Đăng xuất</button></> : <><Link className="font-semibold text-primary-700" to="/login">Đăng nhập</Link><Link className="hidden rounded-lg border border-primary-200 px-3 py-2 text-primary-700 sm:inline" to="/register">Đăng ký</Link></>}</nav>
      </div>
    </header>
    <main className="mx-auto max-w-7xl p-5"><Outlet /></main>
  </div>
}
