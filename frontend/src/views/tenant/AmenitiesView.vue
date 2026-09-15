<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputNumber from 'primevue/inputnumber'
import Textarea from 'primevue/textarea'
import DatePicker from 'primevue/datepicker'
import Tag from 'primevue/tag'
import { amenityApi } from '@/services'
import type { Amenity, AmenityBooking } from '@/types'
import { extractError } from '@/services/api'
import { formatDate, formatTime, amenityBookingStatusLabel, amenityBookingStatusSeverity, label } from '@/composables/format'

const toast = useToast()

const amenities = ref<Amenity[]>([])
const loading = ref(true)

const myBookings = ref<AmenityBooking[]>([])
const loadingMine = ref(true)

// ----- Đặt lịch -----
const showBook = ref(false)
const booking = ref(false)
const bookTarget = ref<Amenity | null>(null)
const bookDate = ref<Date | null>(null)
const bookStart = ref<Date | null>(null)
const bookEnd = ref<Date | null>(null)
const partySize = ref<number>(1)
const bookNote = ref('')

// ----- Xem lịch -----
const showSchedule = ref(false)
const scheduleTarget = ref<Amenity | null>(null)
const scheduleDay = ref<Date | null>(new Date())
const scheduleList = ref<AmenityBooking[]>([])
const loadingSchedule = ref(false)

async function load() {
  loading.value = true
  try {
    amenities.value = await amenityApi.list()
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    loading.value = false
  }
}

async function loadMine() {
  loadingMine.value = true
  try {
    myBookings.value = await amenityApi.myBookings()
  } catch {
    myBookings.value = []
  } finally {
    loadingMine.value = false
  }
}

/** Ghép ngày (Date) với giờ (Date) thành ISO string. */
function combine(day: Date, time: Date): string {
  const d = new Date(day)
  d.setHours(time.getHours(), time.getMinutes(), 0, 0)
  return d.toISOString()
}

function openBook(a: Amenity) {
  bookTarget.value = a
  bookDate.value = new Date()
  bookStart.value = null
  bookEnd.value = null
  partySize.value = 1
  bookNote.value = ''
  showBook.value = true
}

async function submitBook() {
  if (!bookTarget.value) return
  if (!bookDate.value || !bookStart.value || !bookEnd.value) {
    toast.add({ severity: 'warn', summary: 'Chọn ngày và giờ bắt đầu/kết thúc', life: 2500 })
    return
  }
  booking.value = true
  try {
    await amenityApi.book({
      amenityId: bookTarget.value.id,
      startTime: combine(bookDate.value, bookStart.value),
      endTime: combine(bookDate.value, bookEnd.value),
      partySize: partySize.value,
      note: bookNote.value || undefined
    })
    showBook.value = false
    await loadMine()
    toast.add({ severity: 'success', summary: 'Đã đặt lịch tiện ích', life: 2500 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    booking.value = false
  }
}

function openSchedule(a: Amenity) {
  scheduleTarget.value = a
  scheduleDay.value = new Date()
  scheduleList.value = []
  showSchedule.value = true
  loadSchedule()
}

async function loadSchedule() {
  if (!scheduleTarget.value || !scheduleDay.value) return
  loadingSchedule.value = true
  try {
    scheduleList.value = await amenityApi.bookings(scheduleTarget.value.id, scheduleDay.value.toISOString())
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    loadingSchedule.value = false
  }
}

async function cancelMine(b: AmenityBooking) {
  try {
    await amenityApi.cancelBooking(b.id)
    await loadMine()
    toast.add({ severity: 'success', summary: 'Đã hủy lịch', life: 2000 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  }
}

onMounted(async () => { await load(); await loadMine() })
</script>

<template>
  <div class="head">
    <h1>Tiện ích</h1>
  </div>

  <div v-if="loading" class="empty"><i class="pi pi-spin pi-spinner" /><p>Đang tải...</p></div>
  <div v-else-if="!amenities.length" class="empty"><i class="pi pi-th-large" /><p>Chưa có tiện ích nào.</p></div>

  <div v-else class="grid">
    <div v-for="a in amenities" :key="a.id" class="card">
      <div class="body">
        <h3>{{ a.name }}</h3>
        <div class="meta"><i class="pi pi-building" /> {{ a.buildingName || '—' }}</div>
        <div class="meta"><i class="pi pi-users" /> Sức chứa: {{ a.capacity ?? '—' }}</div>
        <div class="meta"><i class="pi pi-clock" /> {{ a.openTime || '—' }} - {{ a.closeTime || '—' }}</div>
        <p v-if="a.description" class="desc">{{ a.description }}</p>
        <div class="actions">
          <Button label="Đặt lịch" icon="pi pi-calendar-plus" size="small" @click="openBook(a)" />
          <Button label="Xem lịch" icon="pi pi-calendar" size="small" severity="secondary" outlined @click="openSchedule(a)" />
        </div>
      </div>
    </div>
  </div>

  <h2 class="sectitle">Lịch đặt của tôi</h2>
  <DataTable :value="myBookings" :loading="loadingMine" stripedRows size="small" class="box" paginator :rows="5" :rowsPerPageOptions="[5, 10, 20]">
    <Column field="amenityName" header="Tiện ích" />
    <Column header="Bắt đầu" style="width: 160px"><template #body="{ data }">{{ formatDate(data.startTime) }}</template></Column>
    <Column header="Kết thúc" style="width: 110px"><template #body="{ data }">{{ formatTime(data.endTime) }}</template></Column>
    <Column field="partySize" header="Số người" style="width: 100px" />
    <Column field="status" header="Trạng thái" style="width: 120px">
      <template #body="{ data }"><Tag :value="label(amenityBookingStatusLabel, data.status)" :severity="amenityBookingStatusSeverity[data.status] || 'info'" /></template>
    </Column>
    <Column header="Thao tác" style="width: 90px">
      <template #body="{ data }">
        <Button label="Hủy" icon="pi pi-times" text size="small" severity="danger" @click="cancelMine(data)" />
      </template>
    </Column>
    <template #empty><div class="empty"><i class="pi pi-calendar" /><p>Bạn chưa đặt lịch tiện ích nào.</p></div></template>
  </DataTable>

  <!-- Dialog đặt lịch -->
  <Dialog v-model:visible="showBook" :header="bookTarget ? `Đặt lịch: ${bookTarget.name}` : 'Đặt lịch'" modal style="width: 480px">
    <div class="form">
      <label>Ngày</label>
      <DatePicker v-model="bookDate" dateFormat="dd/mm/yy" showIcon class="w-full" />
      <label>Giờ bắt đầu</label>
      <DatePicker v-model="bookStart" timeOnly hourFormat="24" showIcon iconDisplay="input" class="w-full" />
      <label>Giờ kết thúc</label>
      <DatePicker v-model="bookEnd" timeOnly hourFormat="24" showIcon iconDisplay="input" class="w-full" />
      <label>Số người</label>
      <InputNumber v-model="partySize" :min="1" class="w-full" inputClass="w-full" />
      <label>Ghi chú</label>
      <Textarea v-model="bookNote" rows="2" class="w-full" autoResize />
    </div>
    <template #footer>
      <Button label="Hủy" text @click="showBook = false" />
      <Button label="Đặt lịch" icon="pi pi-check" :loading="booking" @click="submitBook" />
    </template>
  </Dialog>

  <!-- Dialog xem lịch -->
  <Dialog v-model:visible="showSchedule" :header="scheduleTarget ? `Lịch: ${scheduleTarget.name}` : 'Lịch'" modal style="width: 560px">
    <div class="form">
      <label>Chọn ngày</label>
      <DatePicker v-model="scheduleDay" dateFormat="dd/mm/yy" showIcon class="w-full" @update:modelValue="loadSchedule" />
    </div>
    <DataTable :value="scheduleList" :loading="loadingSchedule" size="small" class="mini">
      <Column field="tenantName" header="Khách thuê" />
      <Column header="Bắt đầu" style="width: 100px"><template #body="{ data }">{{ formatTime(data.startTime) }}</template></Column>
      <Column header="Kết thúc" style="width: 100px"><template #body="{ data }">{{ formatTime(data.endTime) }}</template></Column>
      <Column field="partySize" header="Số người" style="width: 90px" />
      <template #empty><span class="muted">Không có lịch trong ngày này.</span></template>
    </DataTable>
    <template #footer>
      <Button label="Đóng" text @click="showSchedule = false" />
    </template>
  </Dialog>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--sp-3); }
.grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: var(--sp-3); }
.card { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); overflow: hidden; }
.body { padding: var(--sp-3); display: flex; flex-direction: column; gap: var(--sp-2); }
.body h3 { margin: 0; font-size: 1rem; }
.meta { font-size: 0.82rem; color: var(--text-muted); display: flex; align-items: center; gap: 6px; }
.desc { margin: 0; font-size: 0.82rem; color: var(--text-muted); }
.actions { display: flex; gap: var(--sp-2); margin-top: var(--sp-2); }
.sectitle { margin: var(--sp-5) 0 var(--sp-3); font-size: 1.1rem; }
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
.mini { border: 1px solid var(--border); border-radius: var(--radius-sm); margin-top: var(--sp-3); }
.muted { color: var(--text-muted); font-size: 0.85rem; }
.empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); padding: var(--sp-8); color: var(--text-muted); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.form { display: flex; flex-direction: column; gap: var(--sp-1); }
.form label { font-weight: 600; font-size: 0.85rem; margin-top: var(--sp-2); }
</style>
