import axios from 'axios'

// Base URL: mặc định gọi qua proxy '/api' (Vite dev) hoặc biến môi trường khi build.
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  headers: { 'Content-Type': 'application/json' }
})

// Interceptor: gắn JWT vào mỗi request
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token && config.headers) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Refresh token dùng chung để nhiều request 401 đồng thời chỉ refresh 1 lần.
let refreshing: Promise<string | null> | null = null
async function tryRefresh(): Promise<string | null> {
  const rt = localStorage.getItem('refreshToken')
  if (!rt) return null
  try {
    // Dùng axios "trần" để tránh vòng lặp interceptor.
    const res = await axios.post(`${api.defaults.baseURL}/auth/refresh`, { refreshToken: rt })
    localStorage.setItem('token', res.data.token)
    localStorage.setItem('refreshToken', res.data.refreshToken)
    if (res.data.user) localStorage.setItem('user', JSON.stringify(res.data.user))
    return res.data.token as string
  } catch {
    return null
  }
}

function clearSession() {
  localStorage.removeItem('token')
  localStorage.removeItem('refreshToken')
  localStorage.removeItem('user')
}

// Interceptor: 401 -> thử refresh 1 lần rồi phát lại request; thất bại -> đăng xuất.
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config
    const status = error.response?.status
    if (status === 401 && original && !original._retry && localStorage.getItem('refreshToken')) {
      original._retry = true
      refreshing = refreshing ?? tryRefresh()
      const newToken = await refreshing
      refreshing = null
      if (newToken) {
        original.headers = original.headers ?? {}
        original.headers.Authorization = `Bearer ${newToken}`
        return api(original)
      }
    }
    if (status === 401) {
      clearSession()
      if (!location.pathname.startsWith('/login')) location.href = '/login'
    }
    return Promise.reject(error)
  }
)

/** Trích thông điệp lỗi thân thiện từ response của backend { error, statusCode }. */
export function extractError(err: unknown): string {
  const anyErr = err as any
  return anyErr?.response?.data?.error || anyErr?.response?.data?.message || anyErr?.message || 'Đã có lỗi xảy ra.'
}

export default api
