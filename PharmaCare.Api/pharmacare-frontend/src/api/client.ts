import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { useAuthStore } from '@/auth/store'
import type { ApiError, TokenResponse } from '@/types/api'

interface RetryRequest extends InternalAxiosRequestConfig { _retry?: boolean }

const client = axios.create({ baseURL: '/api' })
let refreshPromise: Promise<string> | null = null

function isPublicGet(config: { method?: string; url?: string }) {
  if ((config.method ?? 'get').toLowerCase() !== 'get') return false
  const path = config.url ?? ''
  return path.startsWith('/products') || path.startsWith('/categories') || path.startsWith('/branches')
}

client.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken
  if (token && !isPublicGet(config)) config.headers.Authorization = `Bearer ${token}`
  return config
})

async function rotateToken(): Promise<string> {
  const refreshToken = useAuthStore.getState().refreshToken
  if (!refreshToken) throw new Error('Phiên đăng nhập đã hết hạn.')
  const response = await axios.post<TokenResponse>('/api/auth/refresh', { refreshToken })
  useAuthStore.getState().setTokens(response.data)
  return response.data.accessToken
}

client.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const request = error.config as RetryRequest | undefined
    const isAuthAction = request?.url?.endsWith('/auth/login') || request?.url?.endsWith('/auth/refresh')

    if (error.response?.status === 401 && request && !request._retry && !isAuthAction && !isPublicGet(request)) {
      request._retry = true
      refreshPromise ??= rotateToken().finally(() => { refreshPromise = null })
      try {
        const accessToken = await refreshPromise
        request.headers.Authorization = `Bearer ${accessToken}`
        return client(request)
      } catch {
        useAuthStore.getState().clearSession()
        if (window.location.pathname !== '/login') window.location.assign('/login')
      }
    }

    const payload = error.response?.data as { message?: string; error?: string } | undefined
    const normalized: ApiError = {
      status: error.response?.status,
      message: payload?.message ?? payload?.error ?? error.message,
      data: error.response?.data,
    }
    return Promise.reject(normalized)
  },
)

export default client
