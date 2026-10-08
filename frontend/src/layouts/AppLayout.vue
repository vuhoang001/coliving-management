<script setup lang="ts">
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import Menu from 'primevue/menu'
import NotificationBell from '@/components/NotificationBell.vue'
import { useAuthStore } from '@/stores/auth'
import { useNotificationStore } from '@/stores/notification'
import { roleLabel } from '@/composables/format'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()
const notif = useNotificationStore()
const toast = useToast()

interface NavItem { label: string; icon: string; name: string }

// Menu nhân viên (Staff/Manager/Admin).
const staffNav: NavItem[] = [
  { label: 'Bảng điều khiển', icon: 'pi pi-chart-line', name: 'dashboard' },
  { label: 'Toà nhà', icon: 'pi pi-building', name: 'buildings' },
  { label: 'Căn hộ', icon: 'pi pi-th-large', name: 'apartments' },
  { label: 'Phòng', icon: 'pi pi-home', name: 'rooms' },
  { label: 'Tài sản', icon: 'pi pi-box', name: 'assets' },
  { label: 'Đặt phòng', icon: 'pi pi-calendar-plus', name: 'bookings' },
  { label: 'Hợp đồng', icon: 'pi pi-file-edit', name: 'contracts' },
  { label: 'Nhận/Trả phòng', icon: 'pi pi-sign-in', name: 'checks' },
  { label: 'Sự cố', icon: 'pi pi-exclamation-triangle', name: 'incidents' },
  { label: 'Tiện ích', icon: 'pi pi-star', name: 'amenities-admin' },
  { label: 'Dịch vụ', icon: 'pi pi-wrench', name: 'services' },
  { label: 'Hoá đơn', icon: 'pi pi-receipt', name: 'invoices' }
]
const managerOnlyNav: NavItem[] = [
  { label: 'Người dùng', icon: 'pi pi-users', name: 'users' },
  { label: 'Nhật ký', icon: 'pi pi-history', name: 'audit-logs' }
]
// Menu khách thuê (Tenant).
const tenantNav: NavItem[] = [
  { label: 'Phòng trống', icon: 'pi pi-home', name: 'tenant-home' },
  { label: 'Đặt phòng của tôi', icon: 'pi pi-calendar-plus', name: 'my-bookings' },
  { label: 'Hợp đồng của tôi', icon: 'pi pi-file-edit', name: 'my-contracts' },
  { label: 'Hoá đơn của tôi', icon: 'pi pi-receipt', name: 'my-invoices' },
  { label: 'Sự cố của tôi', icon: 'pi pi-exclamation-triangle', name: 'my-incidents' },
  { label: 'Tiện ích', icon: 'pi pi-star', name: 'amenities' },
  { label: 'Dịch vụ', icon: 'pi pi-wrench', name: 'my-services' }
]
const commonNav: NavItem[] = [
  { label: 'Thông báo', icon: 'pi pi-bell', name: 'notifications' }
]

const nav = computed<NavItem[]>(() => {
  if (auth.isStaff) {
    return [...staffNav, ...(auth.isManager ? managerOnlyNav : []), ...commonNav]
  }
  return [...tenantNav, ...commonNav]
})

const sidebarOpen = ref(false)
function go(name: string) { router.push({ name }); sidebarOpen.value = false }
watch(() => route.name, () => { sidebarOpen.value = false })

// Menu người dùng (góc phải).
const userMenu = ref()
const userMenuItems = [
  { label: 'Hồ sơ', icon: 'pi pi-user', command: () => router.push({ name: 'profile' }) },
  { separator: true },
  { label: 'Đăng xuất', icon: 'pi pi-sign-out', command: () => doLogout() }
]
function toggleUserMenu(e: Event) { userMenu.value.toggle(e) }

async function doLogout() {
  await notif.disconnect()
  await auth.logout()
  router.push({ name: 'login' })
}

onMounted(async () => {
  // Đăng ký toast realtime cho SignalR.
  notif.onToast((n) => {
    toast.add({ severity: 'info', summary: n.title, detail: n.message, life: 4000 })
  })
  await notif.fetch()
  await notif.connect()
})
onBeforeUnmount(() => { notif.disconnect() })

const initials = computed(() => {
  const name = auth.user?.fullName || auth.user?.email || '?'
  return name.trim().charAt(0).toUpperCase()
})
</script>

<template>
  <div class="app-shell">
    <div v-if="sidebarOpen" class="overlay" @click="sidebarOpen = false" />

    <aside class="sidebar" :class="{ open: sidebarOpen }">
      <div class="brand"><i class="pi pi-building" /> Co<b>living</b></div>
      <nav>
        <button
          v-for="item in nav"
          :key="item.name"
          class="nav-item"
          :class="{ active: route.name === item.name }"
          @click="go(item.name)"
        >
          <i :class="item.icon" /> <span>{{ item.label }}</span>
        </button>
      </nav>
    </aside>

    <div class="app-main">
      <header class="app-header">
        <button class="burger" aria-label="Chức năng" @click="sidebarOpen = !sidebarOpen"><i class="pi pi-bars" /></button>
        <span class="app-title">Coliving</span>
        <div class="header-right">
          <NotificationBell />
          <button class="user-btn" @click="toggleUserMenu">
            <span class="avatar">{{ initials }}</span>
            <span class="user-meta">
              <strong>{{ auth.user?.fullName }}</strong>
              <small>{{ roleLabel[auth.user?.role || ''] || auth.user?.role }}</small>
            </span>
            <i class="pi pi-angle-down" />
          </button>
          <Menu ref="userMenu" :model="userMenuItems" :popup="true" />
        </div>
      </header>
      <div class="app-content">
        <router-view />
      </div>
    </div>
  </div>
</template>

<style scoped>
.app-shell { display: flex; min-height: 100vh; }

.sidebar {
  width: 244px; background: var(--surface); color: var(--text-2);
  display: flex; flex-direction: column; padding: var(--sp-4) var(--sp-2);
  flex-shrink: 0; border-right: 1px solid var(--border);
  position: sticky; top: 0; align-self: flex-start; height: 100vh; overflow-y: auto;
}
.brand { display: flex; align-items: center; gap: 8px; font-size: 1.2rem; font-weight: 700; letter-spacing: -0.02em; padding: var(--sp-2) var(--sp-3) var(--sp-5); color: var(--text); }
.brand .pi { color: var(--brand); }
.nav-item {
  display: flex; align-items: center; gap: 12px; width: 100%;
  background: none; border: none; color: var(--text-2); padding: 10px var(--sp-3);
  cursor: pointer; font-size: 0.9rem; font-family: inherit; text-align: left;
  border-radius: var(--radius); margin-bottom: 2px; transition: all var(--ease);
}
.nav-item i { font-size: 1rem; }
.nav-item:hover { background: var(--surface-2); color: var(--text); }
.nav-item.active { background: var(--brand-50); color: var(--brand); font-weight: 600; }

.app-main { flex: 1; display: flex; flex-direction: column; background: var(--bg); min-width: 0; }
.app-header {
  background: var(--surface); padding: var(--sp-3) var(--sp-5);
  border-bottom: 1px solid var(--border); display: flex; align-items: center; gap: var(--sp-3);
  position: sticky; top: 0; z-index: 50;
}
.app-title { font-weight: 700; font-size: 1.1rem; }
.header-right { margin-left: auto; display: flex; align-items: center; gap: var(--sp-2); }
.burger { display: none; background: none; border: none; cursor: pointer; font-size: 1.3rem; color: var(--text); padding: 4px; }
.user-btn { display: flex; align-items: center; gap: 8px; background: none; border: none; cursor: pointer; padding: 4px 8px; border-radius: var(--radius); font-family: inherit; transition: background var(--ease); }
.user-btn:hover { background: var(--surface-2); }
.avatar { width: 34px; height: 34px; border-radius: 50%; background: var(--brand); color: #fff; display: grid; place-items: center; font-weight: 700; }
.user-meta { display: flex; flex-direction: column; line-height: 1.1; text-align: left; }
.user-meta strong { font-size: 0.85rem; }
.user-meta small { font-size: 0.72rem; color: var(--text-muted); }
.app-content { padding: var(--sp-5); }

.overlay { display: none; }

@media (max-width: 900px) {
  .burger { display: inline-flex; }
  .sidebar { position: fixed; top: 0; left: 0; bottom: 0; z-index: 200; transform: translateX(-100%); transition: transform var(--ease); box-shadow: var(--shadow-md); }
  .sidebar.open { transform: translateX(0); }
  .overlay { display: block; position: fixed; inset: 0; z-index: 150; background: rgba(0, 0, 0, 0.45); }
  .app-content { padding: var(--sp-4); }
  .user-meta { display: none; }
}
</style>
