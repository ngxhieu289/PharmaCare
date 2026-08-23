import { useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/auth/store'
import axios from 'axios'
import type { ApiError } from '@/types/api'

export default function Register() {
  const [form, setForm] = useState({ email: '', username: '', password: '', confirm: '', displayName: '', phone: '' })
  const [error, setError] = useState(''); const [busy, setBusy] = useState(false)
  const register = useAuthStore((state) => state.register); const user = useAuthStore((state) => state.user); const navigate = useNavigate()
  if (user) return <Navigate to="/" replace />
  async function submit(event: FormEvent) {
    event.preventDefault(); setError('')
    if (form.displayName.trim().length < 2) return setError('Họ và tên phải có ít nhất 2 ký tự.')
    if (!/^[a-zA-Z0-9._-]{3,50}$/.test(form.username)) return setError('Tên tài khoản phải có 3–50 ký tự, chỉ gồm chữ, số, dấu chấm, gạch dưới hoặc gạch ngang.')
    if (form.password.length < 8) return setError('Mật khẩu phải có ít nhất 8 ký tự.')
    if (form.phone && !/^(0|\+84)[0-9]{9,10}$/.test(form.phone.replace(/\s/g, ''))) return setError('Số điện thoại không đúng định dạng Việt Nam.')
    if (form.password !== form.confirm) return setError('Mật khẩu xác nhận không khớp.')
    setBusy(true)
    try { await register(form.email, form.username, form.password, form.displayName, form.phone); navigate('/') }
    catch (reason) { const data = axios.isAxiosError(reason) ? reason.response?.data as { error?: string; title?: string; errors?: Record<string, string[]> } : null; const validation = data?.errors ? Object.values(data.errors).flat()[0] : null; setError(validation ?? (data?.error === 'Email already exists' ? 'Email hoặc tên tài khoản đã tồn tại.' : data?.title) ?? (reason as ApiError).message ?? 'Không thể đăng ký.') }
    finally { setBusy(false) }
  }
  const field = (key: keyof typeof form, label: string, type = 'text') => <label className="block text-sm font-medium">{label}<input required type={type} value={form[key]} onChange={(event) => setForm({ ...form, [key]: event.target.value })} className="mt-2 w-full rounded-xl border border-slate-200 px-4 py-3" /></label>
  return <main className="min-h-screen bg-gradient-to-br from-primary-900 to-cyan-500 p-5 flex items-center justify-center"><section className="w-full max-w-lg rounded-3xl bg-white p-8 shadow-2xl">
    <h1 className="text-center text-3xl font-bold text-primary-900">Tạo tài khoản</h1><p className="mb-6 mt-2 text-center text-sm text-slate-500">Mua thuốc và theo dõi đơn hàng tại PharmaCare</p>
    {error && <p className="mb-4 rounded-xl bg-red-50 p-3 text-sm text-red-700">{error}</p>}<form onSubmit={submit} className="grid gap-4 sm:grid-cols-2">
      <div className="sm:col-span-2">{field('displayName', 'Họ và tên')}</div>{field('username', 'Tên tài khoản')}{field('phone', 'Số điện thoại', 'tel')}<div className="sm:col-span-2">{field('email', 'Email', 'email')}</div>{field('password', 'Mật khẩu (tối thiểu 8 ký tự)', 'password')}{field('confirm', 'Xác nhận mật khẩu', 'password')}
      <button disabled={busy} className="sm:col-span-2 rounded-xl bg-primary-600 py-3 font-semibold text-white">{busy ? 'Đang tạo…' : 'Đăng ký'}</button>
    </form><p className="mt-5 text-center text-sm">Đã có tài khoản? <Link className="font-semibold text-primary-700" to="/login">Đăng nhập</Link></p>
  </section></main>
}
