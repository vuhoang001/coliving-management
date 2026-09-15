import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { User, Role } from '@/types'
import { authApi } from '@/services'

const STAFF_ROLES: Role[] = ['Staff', 'Manager', 'Admin']
const MANAGER_ROLES: Role[] = ['Manager', 'Admin']

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(JSON.parse(localStorage.getItem('user') || 'null'))
  const token = ref<string | null>(localStorage.getItem('token'))
  const refreshToken = ref<string | null>(localStorage.getItem('refreshToken'))

  const isAuthenticated = computed(() => !!token.value)
  const role = computed<Role | undefined>(() => user.value?.role)
  // Nhân viên trở lên (Staff/Manager/Admin) — thấy khu quản trị.
  const isStaff = computed(() => !!user.value && STAFF_ROLES.includes(user.value.role))
  // Quản lý trở lên (Manager/Admin) — quyền CRUD cao nhất + quản lý người dùng.
  const isManager = computed(() => !!user.value && MANAGER_ROLES.includes(user.value.role))
  const isTenant = computed(() => user.value?.role === 'Tenant')

  function persist() {
    token.value ? localStorage.setItem('token', token.value) : localStorage.removeItem('token')
    refreshToken.value ? localStorage.setItem('refreshToken', refreshToken.value) : localStorage.removeItem('refreshToken')
    user.value ? localStorage.setItem('user', JSON.stringify(user.value)) : localStorage.removeItem('user')
  }

  async function login(email: string, password: string) {
    const res = await authApi.login({ email, password })
    token.value = res.token
    refreshToken.value = res.refreshToken
    user.value = res.user
    persist()
  }

  async function register(data: { email: string; password: string; fullName: string; phone?: string }) {
    const res = await authApi.register(data)
    token.value = res.token
    refreshToken.value = res.refreshToken
    user.value = res.user
    persist()
  }

  function updateUser(u: User) {
    user.value = u
    persist()
  }

  async function logout() {
    const rt = refreshToken.value
    token.value = null
    user.value = null
    refreshToken.value = null
    persist()
    if (rt) { try { await authApi.logout(rt) } catch { /* ignore */ } }
  }

  return {
    user, token, refreshToken, isAuthenticated, role, isStaff, isManager, isTenant,
    login, register, logout, updateUser
  }
})
