import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const routes = [
  { path: '/login', name: 'login', component: () => import('@/views/LoginView.vue'), meta: { public: true } },
  { path: '/register', name: 'register', component: () => import('@/views/RegisterView.vue'), meta: { public: true } },
  { path: '/payment/result', name: 'payment-result', component: () => import('@/views/PaymentResultView.vue'), meta: { public: true } },
  { path: '/payment/mock', name: 'payment-mock', component: () => import('@/views/PaymentMockView.vue') },
  {
    path: '/',
    component: () => import('@/layouts/AppLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      // ----- Chung / khu nhân viên -----
      { path: '', name: 'dashboard', component: () => import('@/views/DashboardView.vue'), meta: { staff: true } },
      { path: 'buildings', name: 'buildings', component: () => import('@/views/BuildingsView.vue'), meta: { staff: true } },
      { path: 'apartments', name: 'apartments', component: () => import('@/views/ApartmentsView.vue'), meta: { staff: true } },
      { path: 'rooms', name: 'rooms', component: () => import('@/views/RoomsView.vue'), meta: { staff: true } },
      { path: 'assets', name: 'assets', component: () => import('@/views/AssetsView.vue'), meta: { staff: true } },
      { path: 'bookings', name: 'bookings', component: () => import('@/views/BookingsView.vue'), meta: { staff: true } },
      { path: 'contracts', name: 'contracts', component: () => import('@/views/ContractsView.vue'), meta: { staff: true } },
      { path: 'checks', name: 'checks', component: () => import('@/views/CheckInOutView.vue'), meta: { staff: true } },
      { path: 'incidents', name: 'incidents', component: () => import('@/views/IncidentsView.vue'), meta: { staff: true } },
      { path: 'services', name: 'services', component: () => import('@/views/ServicesView.vue'), meta: { staff: true } },
      { path: 'invoices', name: 'invoices', component: () => import('@/views/InvoicesView.vue'), meta: { staff: true } },
      { path: 'amenities-admin', name: 'amenities-admin', component: () => import('@/views/AmenitiesAdminView.vue'), meta: { staff: true } },
      { path: 'users', name: 'users', component: () => import('@/views/UsersView.vue'), meta: { manager: true } },

      // ----- Khu khách thuê (Tenant) -----
      { path: 'home', name: 'tenant-home', component: () => import('@/views/tenant/AvailableRoomsView.vue') },
      { path: 'my-bookings', name: 'my-bookings', component: () => import('@/views/tenant/MyBookingsView.vue') },
      { path: 'my-contracts', name: 'my-contracts', component: () => import('@/views/tenant/MyContractsView.vue') },
      { path: 'my-invoices', name: 'my-invoices', component: () => import('@/views/tenant/MyInvoicesView.vue') },
      { path: 'my-incidents', name: 'my-incidents', component: () => import('@/views/tenant/MyIncidentsView.vue') },
      { path: 'my-services', name: 'my-services', component: () => import('@/views/tenant/MyServicesView.vue') },
      { path: 'amenities', name: 'amenities', component: () => import('@/views/tenant/AmenitiesView.vue') },

      // ----- Chung cho mọi vai trò -----
      { path: 'notifications', name: 'notifications', component: () => import('@/views/NotificationsView.vue') },
      { path: 'profile', name: 'profile', component: () => import('@/views/ProfileView.vue') }
    ]
  },
  { path: '/:pathMatch(.*)*', name: 'not-found', component: () => import('@/views/NotFoundView.vue'), meta: { public: true } }
]

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: () => ({ top: 0 })
})

router.beforeEach((to) => {
  const auth = useAuthStore()

  if (to.meta.public) {
    // Đã đăng nhập mà vào /login → về trang chủ theo vai trò.
    if (to.name === 'login' && auth.isAuthenticated) {
      return { path: auth.isStaff ? '/' : '/home' }
    }
    return true
  }

  if (to.meta.requiresAuth !== false && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  // Route chỉ dành cho nhân viên (staff+) → chặn Tenant.
  if (to.meta.staff && !auth.isStaff) {
    return { name: 'tenant-home' }
  }
  // Route chỉ dành cho quản lý (Manager/Admin).
  if (to.meta.manager && !auth.isManager) {
    return auth.isStaff ? { name: 'dashboard' } : { name: 'tenant-home' }
  }

  return true
})

export default router
