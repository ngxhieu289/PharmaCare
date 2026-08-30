import { useState, type FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/auth/store'
import type { ApiError } from '@/types/api'

export default function Login() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const login = useAuthStore((state) => state.login)
  const currentUser = useAuthStore((state) => state.user)
  const navigate = useNavigate()
  const location = useLocation()

  if (currentUser) {
    const target = currentUser.roles.some((role) => role !== 'Customer') ? '/internal' : '/'
    return <Navigate to={target} replace />
  }

  async function submit(event: FormEvent) {
    event.preventDefault(); setError(''); setSubmitting(true)
    try {
      const user = await login(email, password)
      const fallback = user.roles.some((role) => role !== 'Customer') ? '/internal' : '/'
      const from = (location.state as { from?: { pathname?: string; search?: string } } | null)?.from
      const requested = from?.pathname ? `${from.pathname}${from.search ?? ''}` : undefined
      navigate(requested ?? fallback, { replace: true })
    } catch (reason) {
      setError((reason as ApiError).message || 'Đăng nhập thất bại.')
    } finally { setSubmitting(false) }
  }

  return <main className="min-h-screen bg-gradient-to-br from-primary-900 via-primary-700 to-cyan-500 p-5 flex items-center justify-center">
    <section className="w-full max-w-md rounded-3xl bg-white/95 p-8 shadow-2xl">
      <div className="mb-8 text-center">
        <div className="mx-auto mb-4 grid h-14 w-14 place-items-center rounded-2xl bg-primary-600 text-2xl text-white">✚</div>
        <h1 className="text-3xl font-bold text-primary-900">PharmaCare</h1>
        <p className="mt-2 text-sm text-slate-500">Đăng nhập vào hệ thống nhà thuốc</p>
      </div>
      {error && <div className="mb-4 rounded-xl bg-red-50 p-3 text-sm text-red-700">{error}</div>}
      <form className="space-y-5" onSubmit={submit}>
        <label className="block text-sm font-medium">Email hoặc tên tài khoản
          <input required type="text" autoComplete="username" value={email} onChange={(event) => setEmail(event.target.value)}
            className="mt-2 w-full rounded-xl border border-slate-200 px-4 py-3" placeholder="name@example.com hoặc nguyenvana" />
        </label>
        <label className="block text-sm font-medium">Mật khẩu
          <input required type="password" autoComplete="current-password" value={password} onChange={(event) => setPassword(event.target.value)}
            className="mt-2 w-full rounded-xl border border-slate-200 px-4 py-3" placeholder="••••••••" />
        </label>
        <button disabled={submitting} className="w-full rounded-xl bg-primary-600 py-3 font-semibold text-white hover:bg-primary-700 disabled:opacity-60">
          {submitting ? 'Đang đăng nhập…' : 'Đăng nhập'}
        </button>
      </form>
      <p className="mt-5 text-center text-sm">Chưa có tài khoản? <Link className="font-semibold text-primary-700" to="/register">Đăng ký</Link></p>
    </section>
  </main>
}
