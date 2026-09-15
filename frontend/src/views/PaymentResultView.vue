<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Button from 'primevue/button'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

// Đọc kết quả từ query — hỗ trợ cả redirect từ trang giả lập và VNPay thật.
// VNPay thật trả về: ?status=success|failed&invoiceId=..&code=..
// Nếu chỉ có vnp_ResponseCode (một số cấu hình) thì suy ra trạng thái từ đó.
const invoiceId = computed(() => (route.query.invoiceId as string) || '')
const code = computed(() => (route.query.code as string) || (route.query.vnp_ResponseCode as string) || '')
const success = computed(() => {
  const status = (route.query.status as string) || ''
  if (status) return status === 'success'
  // Không có status → suy ra từ mã phản hồi VNPay ("00" = thành công)
  return code.value === '00'
})

// Khách thuê xem hoá đơn của tôi; nhân viên xem danh sách hoá đơn.
const invoicesPath = computed(() => (auth.isStaff ? '/invoices' : '/my-invoices'))

function goHome() { router.push(auth.isStaff ? '/' : '/home') }
</script>

<template>
  <div class="pr">
    <div class="card">
      <i class="pi" :class="success ? 'pi-check-circle ok' : 'pi-times-circle fail'" />
      <h1>{{ success ? 'Thanh toán thành công' : 'Thanh toán thất bại' }}</h1>
      <p v-if="invoiceId" class="text-muted">Hoá đơn: #{{ invoiceId }}</p>
      <p v-if="code" class="text-muted">Mã phản hồi: {{ code }}</p>
      <p class="text-muted">
        {{ success
          ? 'Cảm ơn bạn. Giao dịch VNPay đã được ghi nhận.'
          : 'Giao dịch chưa hoàn tất hoặc đã bị huỷ. Bạn có thể thử lại.' }}
      </p>
      <div class="actions">
        <Button label="Xem hoá đơn" icon="pi pi-receipt" @click="router.push(invoicesPath)" />
        <Button label="Trang chủ" icon="pi pi-home" text @click="goHome" />
      </div>
    </div>
  </div>
</template>

<style scoped>
.pr { min-height: 100vh; display: grid; place-items: center; background: var(--bg); padding: var(--sp-4); }
.card { background: var(--surface); border-radius: var(--radius-lg); box-shadow: var(--shadow-md); padding: var(--sp-8); text-align: center; max-width: 460px; }
.card .pi { font-size: 4rem; }
.card .ok { color: var(--success, #16a34a); }
.card .fail { color: var(--danger, #dc2626); }
.card h1 { margin: var(--sp-3) 0; }
.actions { display: flex; flex-direction: column; gap: var(--sp-2); margin-top: var(--sp-4); }
</style>
