<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import Textarea from 'primevue/textarea'
import Tag from 'primevue/tag'
import { bookingApi } from '@/services'
import type { Booking } from '@/types'
import { extractError } from '@/services/api'
import { formatDate, formatDay, bookingStatusLabel, bookingStatusSeverity } from '@/composables/format'

const toast = useToast()

const items = ref<Booking[]>([])
const loading = ref(true)

const showCancel = ref(false)
const canceling = ref(false)
const target = ref<Booking | null>(null)
const reason = ref('')

async function load() {
  loading.value = true
  try {
    items.value = await bookingApi.mine()
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    loading.value = false
  }
}

function canCancel(b: Booking): boolean {
  return b.status === 'Pending' || b.status === 'Confirmed'
}

function openCancel(b: Booking) {
  target.value = b
  reason.value = ''
  showCancel.value = true
}

async function submitCancel() {
  if (!target.value) return
  canceling.value = true
  try {
    await bookingApi.cancel(target.value.id, reason.value || undefined)
    showCancel.value = false
    await load()
    toast.add({ severity: 'success', summary: 'Đã hủy đặt phòng', life: 2000 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    canceling.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="head">
    <h1>Đặt phòng của tôi</h1>
  </div>

  <DataTable :value="items" :loading="loading" stripedRows size="small" class="box" paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
    <Column field="id" header="ID" style="width: 60px" />
    <Column field="roomName" header="Phòng" />
    <Column field="buildingName" header="Toà nhà" />
    <Column header="Nhận phòng" style="width: 120px"><template #body="{ data }">{{ formatDay(data.checkInDate) }}</template></Column>
    <Column header="Trả phòng" style="width: 120px"><template #body="{ data }">{{ formatDay(data.checkOutDate) }}</template></Column>
    <Column header="Trạng thái" style="width: 140px">
      <template #body="{ data }"><Tag :value="bookingStatusLabel[data.status]" :severity="bookingStatusSeverity[data.status]" /></template>
    </Column>
    <Column header="Ngày tạo" style="width: 150px"><template #body="{ data }">{{ formatDate(data.createdAt) }}</template></Column>
    <Column header="Thao tác" style="width: 100px">
      <template #body="{ data }">
        <Button v-if="canCancel(data)" label="Hủy" icon="pi pi-times" text size="small" severity="danger" @click="openCancel(data)" />
      </template>
    </Column>
    <template #empty><div class="empty"><i class="pi pi-calendar" /><p>Bạn chưa đặt phòng nào.</p></div></template>
  </DataTable>

  <Dialog v-model:visible="showCancel" header="Hủy đặt phòng" modal style="width: 440px">
    <div class="form">
      <label>Lý do hủy (không bắt buộc)</label>
      <Textarea v-model="reason" rows="3" class="w-full" autoResize />
    </div>
    <template #footer>
      <Button label="Đóng" text @click="showCancel = false" />
      <Button label="Xác nhận hủy" icon="pi pi-check" severity="danger" :loading="canceling" @click="submitCancel" />
    </template>
  </Dialog>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--sp-4); }
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
.empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); padding: var(--sp-8); color: var(--text-muted); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.form { display: flex; flex-direction: column; gap: var(--sp-1); }
.form label { font-weight: 600; font-size: 0.85rem; margin-top: var(--sp-2); }
</style>
