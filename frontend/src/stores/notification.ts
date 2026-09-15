import { defineStore } from 'pinia'
import { ref } from 'vue'
import * as signalR from '@microsoft/signalr'
import type { AppNotification } from '@/types'
import { notificationApi } from '@/services'
import { useAuthStore } from './auth'

// URL hub suy ra từ baseURL API: '/api' → '/hubs/notifications'.
function hubUrl() {
  const base = import.meta.env.VITE_API_BASE_URL || '/api'
  return base.replace(/\/api\/?$/, '') + '/hubs/notifications'
}

// Toast callback đăng ký từ layout để đẩy thông báo realtime.
type ToastFn = (n: AppNotification) => void

export const useNotificationStore = defineStore('notification', () => {
  const items = ref<AppNotification[]>([])
  const unreadCount = ref(0)
  const connected = ref(false)
  let connection: signalR.HubConnection | null = null
  let toastFn: ToastFn | null = null

  function onToast(fn: ToastFn) { toastFn = fn }

  async function fetch() {
    const auth = useAuthStore()
    if (!auth.isAuthenticated) { items.value = []; unreadCount.value = 0; return }
    try {
      const res = await notificationApi.mine()
      items.value = res.items
      unreadCount.value = res.unreadCount
    } catch { /* bỏ qua lỗi mạng */ }
  }

  async function connect() {
    const auth = useAuthStore()
    if (!auth.isAuthenticated || connection) return
    connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl(), { accessTokenFactory: () => localStorage.getItem('token') || '' })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connection.on('notification', (n: AppNotification) => {
      if (!items.value.some((x) => x.id === n.id)) {
        items.value.unshift(n)
        if (!n.isRead) unreadCount.value++
        toastFn?.(n)
      }
      // Đồng bộ lại danh sách để chắc chắn số liệu đúng.
      fetch().catch(() => {})
    })

    try {
      await connection.start()
      connected.value = true
    } catch {
      connection = null
    }
  }

  async function disconnect() {
    connected.value = false
    if (connection) {
      const c = connection
      connection = null
      await c.stop().catch(() => {})
    }
  }

  async function markRead(id: number) {
    const n = items.value.find((x) => x.id === id)
    if (n && !n.isRead) { n.isRead = true; unreadCount.value = Math.max(0, unreadCount.value - 1) }
    await notificationApi.read(id).catch(() => {})
  }

  async function markAllRead() {
    items.value.forEach((n) => (n.isRead = true))
    unreadCount.value = 0
    await notificationApi.readAll().catch(() => {})
  }

  return { items, unreadCount, connected, onToast, fetch, connect, disconnect, markRead, markAllRead }
})
