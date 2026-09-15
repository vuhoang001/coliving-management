<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import DatePicker from 'primevue/datepicker'
import Textarea from 'primevue/textarea'
import { roomApi, bookingApi } from '@/services'
import type { Room } from '@/types'
import { extractError } from '@/services/api'
import { formatCurrency, roomTypeLabel, label } from '@/composables/format'

const toast = useToast()

const items = ref<Room[]>([])
const loading = ref(true)

const fFrom = ref<Date | null>(null)
const fTo = ref<Date | null>(null)

const showDialog = ref(false)
const saving = ref(false)
const selected = ref<Room | null>(null)
const checkInDate = ref<Date | null>(null)
const checkOutDate = ref<Date | null>(null)
const note = ref('')

async function load() {
  loading.value = true
  try {
    items.value = await roomApi.available(
      fFrom.value ? fFrom.value.toISOString() : undefined,
      fTo.value ? fTo.value.toISOString() : undefined
    )
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    loading.value = false
  }
}

function openBooking(r: Room) {
  selected.value = r
  checkInDate.value = null
  checkOutDate.value = null
  note.value = ''
  showDialog.value = true
}

async function submitBooking() {
  if (!selected.value) return
  if (!checkInDate.value || !checkOutDate.value) {
    toast.add({ severity: 'warn', summary: 'Chọn ngày nhận và trả phòng', life: 2500 })
    return
  }
  saving.value = true
  try {
    await bookingApi.create({
      roomId: selected.value.id,
      checkInDate: checkInDate.value.toISOString(),
      checkOutDate: checkOutDate.value.toISOString(),
      note: note.value || undefined
    })
    showDialog.value = false
    toast.add({ severity: 'success', summary: 'Đã gửi yêu cầu đặt phòng', life: 2500 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="head">
    <h1>Phòng trống</h1>
  </div>

  <div class="filters">
    <DatePicker v-model="fFrom" placeholder="Từ ngày" dateFormat="dd/mm/yy" showIcon class="flt" />
    <DatePicker v-model="fTo" placeholder="Đến ngày" dateFormat="dd/mm/yy" showIcon class="flt" />
    <Button label="Tìm" icon="pi pi-search" size="small" @click="load" />
  </div>

  <div v-if="loading" class="empty"><i class="pi pi-spin pi-spinner" /><p>Đang tải...</p></div>
  <div v-else-if="!items.length" class="empty"><i class="pi pi-home" /><p>Không có phòng trống nào.</p></div>

  <div v-else class="grid">
    <div v-for="r in items" :key="r.id" class="card">
      <img :src="r.photoUrl || 'https://placehold.co/400x220?text=Phong'" class="cover" alt="" />
      <div class="body">
        <h3>{{ r.name }}</h3>
        <div class="meta"><i class="pi pi-building" /> {{ r.buildingName || '—' }} · {{ r.apartmentName || '—' }}</div>
        <div class="meta"><i class="pi pi-tag" /> {{ label(roomTypeLabel, r.type) }} · {{ r.area ? r.area + ' m²' : '—' }} · {{ r.capacity ?? '—' }} người</div>
        <div class="price">{{ formatCurrency(r.price) }} <span>/tháng</span></div>
        <Button label="Đặt phòng" icon="pi pi-calendar-plus" size="small" class="w-full" @click="openBooking(r)" />
      </div>
    </div>
  </div>

  <Dialog v-model:visible="showDialog" :header="selected ? `Đặt phòng: ${selected.name}` : 'Đặt phòng'" modal style="width: 480px">
    <div class="form">
      <label>Ngày nhận phòng</label>
      <DatePicker v-model="checkInDate" dateFormat="dd/mm/yy" showIcon class="w-full" />
      <label>Ngày trả phòng</label>
      <DatePicker v-model="checkOutDate" dateFormat="dd/mm/yy" showIcon class="w-full" />
      <label>Ghi chú</label>
      <Textarea v-model="note" rows="3" class="w-full" autoResize />
    </div>
    <template #footer>
      <Button label="Hủy" text @click="showDialog = false" />
      <Button label="Đặt phòng" icon="pi pi-check" :loading="saving" @click="submitBooking" />
    </template>
  </Dialog>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--sp-3); }
.filters { display: flex; gap: var(--sp-2); align-items: center; margin-bottom: var(--sp-3); flex-wrap: wrap; }
.flt { min-width: 170px; }
.grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: var(--sp-3); }
.card { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); overflow: hidden; display: flex; flex-direction: column; }
.cover { width: 100%; height: 160px; object-fit: cover; background: #f3f4f6; }
.body { padding: var(--sp-3); display: flex; flex-direction: column; gap: var(--sp-2); }
.body h3 { margin: 0; font-size: 1rem; }
.meta { font-size: 0.82rem; color: var(--text-muted); display: flex; align-items: center; gap: 6px; }
.price { font-weight: 700; color: var(--primary, #2563eb); font-size: 1.05rem; }
.price span { font-weight: 400; font-size: 0.8rem; color: var(--text-muted); }
.empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); padding: var(--sp-8); color: var(--text-muted); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.form { display: flex; flex-direction: column; gap: var(--sp-1); }
.form label { font-weight: 600; font-size: 0.85rem; margin-top: var(--sp-2); }
</style>
