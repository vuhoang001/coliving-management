<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputNumber from 'primevue/inputnumber'
import Textarea from 'primevue/textarea'
import Select from 'primevue/select'
import FileUpload from 'primevue/fileupload'
import { bookingApi, checkApi, uploadApi } from '@/services'
import type { Booking, CheckRecord } from '@/types'
import { extractError } from '@/services/api'
import { formatDate } from '@/composables/format'

const toast = useToast()

const bookings = ref<Booking[]>([])
const selectedBookingId = ref<number | null>(null)
const records = ref<CheckRecord[]>([])
const loadingRecords = ref(false)

const bookingOptions = computed(() =>
  bookings.value.map((b) => ({
    id: b.id,
    label: `#${b.id} - ${b.roomName ?? '—'} - ${b.tenantName ?? '—'}`
  }))
)

const typeLabel: Record<string, string> = { CheckIn: 'Nhận phòng', CheckOut: 'Trả phòng' }

// Dialog
const showDialog = ref(false)
const dialogMode = ref<'in' | 'out'>('in')
const saving = ref(false)
const uploading = ref(false)

interface Form {
  electricityMeter?: number
  waterMeter?: number
  conditionNote?: string
  photoUrl?: string
}
const form = ref<Form>(blank())
function blank(): Form { return { electricityMeter: undefined, waterMeter: undefined, conditionNote: '', photoUrl: '' } }

async function loadBookings() {
  try {
    const res = await bookingApi.list({ page: 1, pageSize: 100 })
    bookings.value = res.items
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
}

async function loadRecords() {
  if (!selectedBookingId.value) { records.value = []; return }
  loadingRecords.value = true
  try {
    records.value = await checkApi.byBooking(selectedBookingId.value)
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { loadingRecords.value = false }
}

function onBookingChange() { loadRecords() }

function openDialog(mode: 'in' | 'out') {
  if (!selectedBookingId.value) {
    toast.add({ severity: 'warn', summary: 'Chọn đơn đặt phòng trước', life: 2500 }); return
  }
  dialogMode.value = mode
  form.value = blank()
  showDialog.value = true
}

async function onUpload(e: { files: File | File[] }) {
  const file = Array.isArray(e.files) ? e.files[0] : e.files
  if (!file) return
  uploading.value = true
  try { const { url } = await uploadApi.image(file, 'checks'); form.value.photoUrl = url }
  catch (err) { toast.add({ severity: 'error', summary: 'Lỗi tải ảnh', detail: extractError(err), life: 3000 }) }
  finally { uploading.value = false }
}

async function submit() {
  if (!selectedBookingId.value) {
    toast.add({ severity: 'warn', summary: 'Chọn đơn đặt phòng trước', life: 2500 }); return
  }
  saving.value = true
  try {
    const payload = {
      bookingId: selectedBookingId.value,
      electricityMeter: form.value.electricityMeter,
      waterMeter: form.value.waterMeter,
      conditionNote: form.value.conditionNote || undefined,
      photoUrl: form.value.photoUrl || undefined
    }
    if (dialogMode.value === 'in') await checkApi.checkIn(payload)
    else await checkApi.checkOut(payload)
    showDialog.value = false
    await loadRecords()
    toast.add({ severity: 'success', summary: 'Đã ghi nhận', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { saving.value = false }
}

onMounted(loadBookings)
</script>

<template>
  <div class="head">
    <h1>Nhận / Trả phòng</h1>
  </div>

  <div class="filters">
    <Select v-model="selectedBookingId" :options="bookingOptions" optionLabel="label" optionValue="id"
      placeholder="Chọn đơn đặt phòng" showClear filter class="booking-select" @change="onBookingChange" />
    <Button label="Ghi nhận phòng (Check-in)" icon="pi pi-sign-in" size="small" @click="openDialog('in')" />
    <Button label="Ghi trả phòng (Check-out)" icon="pi pi-sign-out" size="small" severity="secondary" @click="openDialog('out')" />
  </div>

  <DataTable :value="records" :loading="loadingRecords" stripedRows size="small" class="box">
    <Column header="Loại" style="width: 130px">
      <template #body="{ data }">{{ typeLabel[data.type] ?? data.type }}</template>
    </Column>
    <Column field="electricityMeter" header="Chỉ số điện" style="width: 120px" />
    <Column field="waterMeter" header="Chỉ số nước" style="width: 120px" />
    <Column field="conditionNote" header="Ghi chú tình trạng" />
    <Column header="Thời gian" style="width: 150px">
      <template #body="{ data }">{{ formatDate(data.createdAt) }}</template>
    </Column>
    <Column header="Ảnh" style="width: 70px">
      <template #body="{ data }">
        <img v-if="data.photoUrl" :src="data.photoUrl" class="thumb" alt="" />
        <span v-else>—</span>
      </template>
    </Column>
    <template #empty>
      <div class="empty">
        <i class="pi pi-clipboard" />
        <p>{{ selectedBookingId ? 'Chưa có bản ghi nào.' : 'Chọn đơn đặt phòng để xem bản ghi.' }}</p>
      </div>
    </template>
  </DataTable>

  <Dialog v-model:visible="showDialog"
    :header="dialogMode === 'in' ? 'Ghi nhận phòng (Check-in)' : 'Ghi trả phòng (Check-out)'"
    modal style="width: 560px">
    <div class="form grid2">
      <div><label>Chỉ số điện</label><InputNumber v-model="form.electricityMeter" :min="0" class="w-full" inputClass="w-full" /></div>
      <div><label>Chỉ số nước</label><InputNumber v-model="form.waterMeter" :min="0" class="w-full" inputClass="w-full" /></div>
      <div class="span2"><label>Ghi chú tình trạng</label><Textarea v-model="form.conditionNote" rows="2" class="w-full" autoResize /></div>
      <div class="span2"><label>Ảnh</label>
        <div class="img-row">
          <img :src="form.photoUrl || 'https://placehold.co/64?text=P'" class="preview" alt="" />
          <FileUpload mode="basic" customUpload auto accept="image/*" :maxFileSize="5000000" chooseLabel="Tải ảnh" chooseIcon="pi pi-upload" :disabled="uploading" @uploader="onUpload" />
        </div>
      </div>
    </div>
    <template #footer>
      <Button label="Hủy" text @click="showDialog = false" />
      <Button label="Lưu" icon="pi pi-check" :loading="saving" @click="submit" />
    </template>
  </Dialog>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--sp-3); }
.filters { display: flex; gap: var(--sp-2); align-items: center; margin-bottom: var(--sp-3); flex-wrap: wrap; }
.booking-select { min-width: 320px; }
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
.thumb { width: 48px; height: 48px; object-fit: cover; border-radius: var(--radius-sm); border: 1px solid var(--border); }
.empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); padding: var(--sp-8); color: var(--text-muted); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.form label { font-weight: 600; font-size: 0.85rem; display: block; margin-bottom: 4px; }
.grid2 { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sp-3); }
.span2 { grid-column: 1 / -1; }
.img-row { display: flex; gap: var(--sp-3); align-items: center; }
.preview { width: 64px; height: 64px; object-fit: cover; border-radius: var(--radius-sm); border: 1px solid var(--border); }
</style>
