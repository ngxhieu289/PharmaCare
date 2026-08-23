import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/auth/store'

export default function InternalLayout() {
  const user = useAuthStore((state) => state.user)
  const logout = useAuthStore((state) => state.logout)
  const navigate = useNavigate()
  const links = [
    { to: '/internal', label: 'Bàn làm việc', permission: 'inventory.read' },
    { to: '/internal/pos', label: 'Bán hàng tại quầy', permission: 'orders.manage' },
    { to: '/internal/dashboard', label: 'Báo cáo tổng quan', permission: 'reports.read' },
    { to: '/internal/branches', label: 'Chi nhánh của tôi', permission: 'branches.read' },
    { to: '/internal/orders', label: 'Đơn hàng', permission: 'orders.read' },
    { to: '/internal/inventory', label: 'Kho thuốc', permission: 'inventory.read' },
    { to: '/internal/products', label: 'Sản phẩm', permission: 'products.manage' },
    { to: '/internal/prescriptions', label: 'Đơn thuốc', permission: 'prescriptions.read' },
    { to: '/internal/vouchers', label: 'Voucher', permission: 'vouchers.manage' },
    { to: '/internal/categories', label: 'Danh mục thuốc', permission: 'products.manage' },
    { to: '/internal/admin/users', label: 'Tài khoản & phân công', permission: 'users.manage' },
    { to: '/internal/admin/roles', label: 'Vai trò & quyền', permission: 'roles.manage' },
    { to: '/internal/admin/branches', label: 'Quản trị chi nhánh', permission: 'branches.manage' },
    { to: '/internal/audit', label: 'Nhật ký truy vết', permission: 'audit.read' },
  ].filter((link) => user?.permissions.includes(link.permission))
  async function signOut() { await logout(); navigate('/login') }
  return <div className="flex min-h-screen bg-slate-100">
    <aside className="flex w-64 flex-col bg-primary-900 text-white">
      <div className="border-b border-primary-800 p-6 text-xl font-bold">✚ PharmaCare</div>
      <nav className="flex-1 space-y-1 p-4">{links.map((link) =>
        <NavLink key={link.to} to={link.to} className={({ isActive }) => `block rounded-xl px-4 py-3 text-sm ${isActive ? 'bg-primary-600' : 'text-primary-100 hover:bg-primary-800'}`}>{link.label}</NavLink>)}</nav>
      <button onClick={signOut} className="m-4 rounded-xl border border-primary-700 p-3 text-left text-sm hover:bg-primary-800">Đăng xuất</button>
    </aside>
    <section className="min-w-0 flex-1"><header className="flex h-16 items-center justify-end border-b bg-white px-7 shadow-sm">
      <div className="text-right"><div className="text-sm font-semibold">{user?.displayName}</div><div className="text-xs text-slate-500">{user?.email}</div></div>
      <div className="ml-3 grid h-9 w-9 place-items-center rounded-full bg-primary-600 font-bold text-white">{user?.displayName.charAt(0).toUpperCase()}</div>
    </header><main className="p-7"><Outlet /></main></section>
  </div>
}
