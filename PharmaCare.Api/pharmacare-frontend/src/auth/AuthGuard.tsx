import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuthStore } from './store'

export function RequireAuth({ children }: { children: ReactNode }) {
  const token = useAuthStore((state) => state.accessToken)
  const location = useLocation()
  return token ? children : <Navigate to="/login" state={{ from: location }} replace />
}

export function RequirePermission({ permission, children }: { permission: string; children: ReactNode }) {
  const allowed = useAuthStore((state) => state.permissions.includes(permission))
  return allowed ? children : <Navigate to="/forbidden" replace />
}

export function RequirePortal({ portal, children }: { portal: 'customer' | 'internal'; children: ReactNode }) {
  const user = useAuthStore((state) => state.user)
  const internal = user?.roles.some((role) => role !== 'Customer') ?? false
  if (!user) return <Navigate to="/login" replace />
  if (portal === 'customer' && internal) return <Navigate to="/internal/dashboard" replace />
  if (portal === 'internal' && !internal) return <Navigate to="/" replace />
  return children
}
