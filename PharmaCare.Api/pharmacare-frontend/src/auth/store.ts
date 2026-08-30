import axios from 'axios'
import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { CurrentUser, TokenResponse } from '@/types/api'

interface AuthState {
  user: CurrentUser | null
  accessToken: string | null
  refreshToken: string | null
  permissions: string[]
  login: (email: string, password: string) => Promise<CurrentUser>
  register: (email: string, username: string, password: string, displayName: string, phone: string) => Promise<CurrentUser>
  logout: () => Promise<void>
  clearSession: () => void
  setTokens: (response: TokenResponse) => void
  setUser: (user: CurrentUser) => void
  hasPermission: (permission: string) => boolean
  isInternalUser: () => boolean
}

const authHttp = axios.create({ baseURL: '/api' })

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      permissions: [],

      login: async (email, password) => {
        const tokenResult = await authHttp.post<TokenResponse>('/auth/login', { email, password })
        const tokens = tokenResult.data
        const profileResult = await authHttp.get<CurrentUser>('/auth/me', {
          headers: { Authorization: `Bearer ${tokens.accessToken}` },
        })
        const user = profileResult.data
        set({ user, accessToken: tokens.accessToken, refreshToken: tokens.refreshToken,
          permissions: tokens.permissions })
        return user
      },

      register: async (email, username, password, displayName, phone) => {
        const tokenResult = await authHttp.post<TokenResponse>('/auth/register', { email, username, password, displayName, phone: phone || null })
        const tokens = tokenResult.data
        const profileResult = await authHttp.get<CurrentUser>('/auth/me', { headers: { Authorization: `Bearer ${tokens.accessToken}` } })
        const user = profileResult.data
        set({ user, accessToken: tokens.accessToken, refreshToken: tokens.refreshToken, permissions: tokens.permissions })
        return user
      },

      logout: async () => {
        const refreshToken = get().refreshToken
        get().clearSession()
        if (refreshToken) {
          await authHttp.post('/auth/revoke', { refreshToken }).catch(() => undefined)
        }
      },

      clearSession: () => set({ user: null, accessToken: null, refreshToken: null, permissions: [] }),
      setTokens: (tokens) => set({
        accessToken: tokens.accessToken,
        refreshToken: tokens.refreshToken,
        permissions: tokens.permissions,
        user: get().user ? { ...get().user!, roles: tokens.roles, permissions: tokens.permissions } : null,
      }),
      setUser: (user) => set({ user, permissions: user.permissions }),
      hasPermission: (permission) => get().permissions.includes(permission),
      isInternalUser: () => get().user?.roles.some((role) => role !== 'Customer') ?? false,
    }),
    {
      name: 'pharmacare-auth',
      partialize: ({ user, accessToken, refreshToken, permissions }) =>
        ({ user, accessToken, refreshToken, permissions }),
    },
  ),
)
