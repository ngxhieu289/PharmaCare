import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { RequireAuth, RequirePermission, RequirePortal } from '@/auth/AuthGuard'
import CustomerLayout from '@/layouts/CustomerLayout'
import InternalLayout from '@/layouts/InternalLayout'
import Catalog from '@/pages/Customer/Catalog'
import Dashboard from '@/pages/Internal/Dashboard'
import Login from '@/pages/Login'
import Register from '@/pages/Register'
import ProductDetail from '@/pages/Customer/ProductDetail'
import Cart from '@/pages/Customer/Cart'
import Checkout from '@/pages/Customer/Checkout'
import Prescriptions from '@/pages/Customer/Prescriptions'
import Orders from '@/pages/Customer/Orders'
import OrderDetail from '@/pages/Customer/OrderDetail'
import Profile from '@/pages/Customer/Profile'
import OrderSuccess from '@/pages/Customer/OrderSuccess'
import InternalHome from '@/pages/Internal/Home'
import InternalPrescriptions from '@/pages/Internal/Prescriptions'
import InternalOrders from '@/pages/Internal/Orders'
import InternalOrderDetail from '@/pages/Internal/OrderDetail'
import Pos from '@/pages/Internal/Pos'
import Inventory from '@/pages/Internal/Inventory'
import Products from '@/pages/Internal/Products'
import Vouchers from '@/pages/Internal/Vouchers'
import Branches from '@/pages/Internal/Branches'
import AdminUsers from '@/pages/Internal/AdminUsers'
import AdminRoles from '@/pages/Internal/AdminRoles'
import AdminBranches from '@/pages/Internal/AdminBranches'
import Categories from '@/pages/Internal/Categories'
import AuditLogs from '@/pages/Internal/AuditLogs'

export default function App() {
  return <BrowserRouter><Routes>
    <Route path="/login" element={<Login />} />
    <Route path="/register" element={<Register />} />
    <Route path="/forbidden" element={<div className="p-16 text-center"><h1 className="text-3xl font-bold text-red-600">403</h1><p>Bạn không có quyền truy cập.</p></div>} />
    <Route path="/" element={<CustomerLayout />}>
      <Route index element={<Catalog />} />
      <Route path="products/:id" element={<ProductDetail />} />
      <Route path="cart" element={<Cart />} />
      <Route path="checkout" element={<Checkout />} />
      <Route path="order-success" element={<OrderSuccess />} />
      <Route path="prescriptions" element={<RequireAuth><RequirePortal portal="customer"><Prescriptions /></RequirePortal></RequireAuth>} />
      <Route path="orders" element={<RequireAuth><RequirePortal portal="customer"><Orders /></RequirePortal></RequireAuth>} />
      <Route path="orders/:id" element={<RequireAuth><RequirePortal portal="customer"><OrderDetail /></RequirePortal></RequireAuth>} />
      <Route path="profile" element={<RequireAuth><RequirePortal portal="customer"><Profile /></RequirePortal></RequireAuth>} />
    </Route>
    <Route path="/internal" element={<RequireAuth><RequirePortal portal="internal"><InternalLayout /></RequirePortal></RequireAuth>}>
      <Route index element={<InternalHome />} />
      <Route path="dashboard" element={<RequirePermission permission="reports.read"><Dashboard /></RequirePermission>} />
      <Route path="prescriptions" element={<RequirePermission permission="prescriptions.read"><InternalPrescriptions /></RequirePermission>} />
      <Route path="orders" element={<RequirePermission permission="orders.read"><InternalOrders /></RequirePermission>} />
      <Route path="orders/:id" element={<RequirePermission permission="orders.read"><InternalOrderDetail /></RequirePermission>} />
      <Route path="pos" element={<RequirePermission permission="orders.manage"><Pos /></RequirePermission>} />
      <Route path="inventory" element={<RequirePermission permission="inventory.read"><Inventory /></RequirePermission>} />
      <Route path="products" element={<RequirePermission permission="products.manage"><Products /></RequirePermission>} />
      <Route path="vouchers" element={<RequirePermission permission="vouchers.manage"><Vouchers /></RequirePermission>} />
      <Route path="branches" element={<RequirePermission permission="branches.read"><Branches /></RequirePermission>} />
      <Route path="admin/users" element={<RequirePermission permission="users.manage"><AdminUsers /></RequirePermission>} />
      <Route path="admin/roles" element={<RequirePermission permission="roles.manage"><AdminRoles /></RequirePermission>} />
      <Route path="admin/branches" element={<RequirePermission permission="branches.manage"><AdminBranches /></RequirePermission>} />
      <Route path="categories" element={<RequirePermission permission="products.manage"><Categories /></RequirePermission>} />
      <Route path="audit" element={<RequirePermission permission="audit.read"><AuditLogs /></RequirePermission>} />
    </Route>
    <Route path="*" element={<Navigate to="/" replace />} />
  </Routes></BrowserRouter>
}
