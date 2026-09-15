<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import Textarea from 'primevue/textarea'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import DatePicker from 'primevue/datepicker'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import { bookingApi, roomApi, userApi } from '@/services'
import type { Booking, Room, User } from '@/types'
import { extractError } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { bookingStatusLabel, bookingStatusSeverity, formatDate, formatDay, sourceChannelLabel, label } from '@/composables/format'

const toast = useToast()
const confirm = useConfirm()
const auth = useAuthStore()

const items = ref<Booking[]>([])
const loading = ref(true)
const totalRecords = ref(0)
const page = ref(1)
const pageSize = ref(10)

const fStatus = ref<string | null>(null)
const statusOptions = Object.entries(bookingStatusLabel).map(([value, label]) => ({ value, label }))

// ---- Tạo đặt phòng ----
const rooms = ref<Room[]>([])
const tenants = ref<User[]>([])
const showDialog = ref(false)
const saving = ref(false)

interface Form {
  roomId?: number; tenantId?: number; checkInDate: Date | null; checkOutDate: Date | null; note?: string
  numberOfOccupants?: number; purpose?: string; sourceChannel?: string; vehiclePlate?: string
}
const form = ref<Form>(blank())
function blank(): Form {
  return {
    roomId: undefined, tenantId: undefined, checkInDate: null, checkOutDate: null, note: '',
    numberOfOccupants: 1, purpose: '', sourceChannel: 'Website', vehiclePlate: ''
  }
}
const sourceChannelOptions = Object.entries(sourceChannelLabel).map(([value, label]) => ({ value, label }))

// ---- Hủy đặt phòng ----
const showCancel = ref(false)
const cancelling = ref(false)
const cancelReason = ref('')
const cancelTarget = ref<Booking | null>(null)

async function load() {
  loading.value = true
  try {
    const res = await bookingApi.list({ page: page.value, pageSize: pageSize.value, status: fStatus.value ?? undefined })
    items.value = res.items
    totalRecords.value = res.totalItems
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { loading.value = false }
}
async function loadRefs() {
  try {
    const [r, t] = await Promise.all([
      roomApi.list({ page: 1, pageSize: 100 }),
      userApi.list({ page: 1, pageSize: 100, role: 'Tenant' })
    ])
    rooms.value = r.items
    tenants.value = t.items
  } catch { /* */ }
}
function onPage(e: { page: number; rows: number }) { page.value = e.page + 1; pageSize.value = e.rows; load() }
function applyFilter() { page.value = 1; load() }

function openNew() { form.value = blank(); showDialog.value = true }
async function save() {
  if (!form.value.roomId || !form.value.tenantId || !form.value.checkInDate || !form.value.checkOutDate) {
    toast.add({ severity: 'warn', summary: 'Chọn phòng, khách thuê & ngày', life: 2500 }); return
  }
  saving.value = true
  try {
    await bookingApi.create({
      roomId: form.value.roomId,
      tenantId: form.value.tenantId,
      checkInDate: form.value.checkInDate.toISOString(),
      checkOutDate: form.value.checkOutDate.toISOString(),
      note: form.value.note || undefined,
      numberOfOccupants: form.value.numberOfOccupants,
      purpose: form.value.purpose || undefined,
      sourceChannel: form.value.sourceChannel,
      vehiclePlate: form.value.vehiclePlate || undefined
    } as any)
    showDialog.value = false; await load()
    toast.add({ severity: 'success', summary: 'Đã tạo đặt phòng', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { saving.value = false }
}

async function confirmBooking(b: Booking) {
  confirm.require({
    message: `Xác nhận đặt phòng #${b.id}?`, header: 'Xác nhận', icon: 'pi pi-check-circle',
    rejectLabel: 'Hủy', acceptLabel: 'Xác nhận',
    accept: async () => {
      try { await bookingApi.confirm(b.id); await load(); toast.add({ severity: 'success', summary: 'Đã xác nhận', life: 2000 }) }
      catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
    }
  })
}

function openCancel(b: Booking) { cancelTarget.value = b; cancelReason.value = ''; showCancel.value = true }
async function doCancel() {
  if (!cancelTarget.value) return
  cancelling.value = true
  try {
    await bookingApi.cancel(cancelTarget.value.id, cancelReason.value || undefined)
    showCancel.value = false; await load()
    toast.add({ severity: 'success', summary: 'Đã hủy', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { cancelling.value = false }
}

onMounted(async () => { if (auth.isStaff) await loadRefs(); await load() })
</script>

<template>
  <div class="head">
    <h1>Đặt phòng</h1>
    <Button v-if="auth.isStaff" label="Tạo đặt phòng" icon="pi pi-plus" size="small" @click="openNew" />
  </div>

  <div class="filters">
    <Select v-model="fStatus" :options="statusOptions" optionLabel="label" optionValue="value" placeholder="Trạng thái" showClear class="flt" @change="applyFilter" />
    <Button label="Lọc" icon="pi pi-search" size="small" @click="applyFilter" />
  </div>

  <DataTable :value="items" :loading="loading" stripedRows size="small" class="box" lazy paginator
    :rows="pageSize" :totalRecords="totalRecords" :rowsPerPageOptions="[10, 20, 50]" @page="onPage">
    <Column field="id" header="ID" style="width: 60px" />
    <Column field="roomName" header="Phòng" />
    <Column field="buildingName" header="Toà nhà" />
    <Column field="tenantName" header="Khách thuê" />
    <Column header="Nhận phòng" style="width: 120px"><template #body="{ data }">{{ formatDay(data.checkInDate) }}</template></Column>
    <Column header="Trả phòng" style="width: 120px"><template #body="{ data }">{{ formatDay(data.checkOutDate) }}</template></Column>
    <Column header="Số người" style="width: 90px"><template #body="{ data }">{{ data.numberOfOccupants ?? '—' }}</template></Column>
    <Column header="Nguồn" style="width: 110px"><template #body="{ data }">{{ label(sourceChannelLabel, data.sourceChannel) }}</template></Column>
    <Column header="Trạng thái" style="width: 140px">
      <template #body="{ data }"><Tag :value="bookingStatusLabel[data.status]" :severity="bookingStatusSeverity[data.status]" /></template>
    </Column>
    <Column header="Ngày tạo" style="width: 150px"><template #body="{ data }">{{ formatDate(data.createdAt) }}</template></Column>
    <Column v-if="auth.isStaff" header="Thao tác" style="width: 200px">
      <template #body="{ data }">
        <Button v-if="data.status === 'Pending'" label="Xác nhận" icon="pi pi-check" text size="small" @click="confirmBooking(data)" />
        <Button v-if="data.status === 'Pending' || data.status === 'Confirmed'" label="Hủy" icon="pi pi-times" text size="small" severity="danger" @click="openCancel(data)" />
      </template>
    </Column>
    <template #empty><div class="empty"><i class="pi pi-calendar" /><p>Chưa có đặt phòng nào.</p></div></template>
  </DataTable>

  <Dialog v-model:visible="showDialog" header="Tạo đặt phòng" modal style="width: 620px">
    <div class="form grid2">
      <div class="span2"><label>Phòng</label>
        <Select v-model="form.roomId" :options="rooms" optionLabel="name" optionValue="id" placeholder="Chọn phòng" class="w-full" filter />
      </div>
      <div class="span2"><label>Khách thuê</label>
        <Select v-model="form.tenantId" :options="tenants" optionLabel="fullName" optionValue="id" placeholder="Chọn khách thuê" class="w-full" filter />
      </div>
      <div><label>Ngày nhận phòng</label><DatePicker v-model="form.checkInDate" dateFormat="dd/mm/yy" showIcon class="w-full" /></div>
      <div><label>Ngày trả phòng</label><DatePicker v-model="form.checkOutDate" dateFormat="dd/mm/yy" showIcon class="w-full" /></div>
      <div><label>Số người ở</label><InputNumber v-model="form.numberOfOccupants" :min="1" class="w-full" inputClass="w-full" /></div>
      <div><label>Nguồn khách</label><Select v-model="form.sourceChannel" :options="sourceChannelOptions" optionLabel="label" optionValue="value" class="w-full" /></div>
      <div><label>Biển số xe gửi</label><InputText v-model="form.vehiclePlate" class="w-full" /></div>
      <div><label>Mục đích thuê</label><InputText v-model="form.purpose" class="w-full" /></div>
      <div class="span2"><label>Ghi chú</label><Textarea v-model="form.note" rows="2" class="w-full" autoResize /></div>
    </div>
    <template #footer>
      <Button label="Hủy" text @click="showDialog = false" />
      <Button label="Tạo" icon="pi pi-check" :loading="saving" @click="save" />
    </template>
  </Dialog>

  <Dialog v-model:visible="showCancel" header="Hủy đặt phòng" modal style="width: 460px">
    <div class="form">
      <label>Lý do hủy</label>
      <Textarea v-model="cancelReason" rows="3" class="w-full" autoResize placeholder="Nhập lý do (không bắt buộc)" />
    </div>
    <template #footer>
      <Button label="Đóng" text @click="showCancel = false" />
      <Button label="Xác nhận hủy" icon="pi pi-times" severity="danger" :loading="cancelling" @click="doCancel" />
    </template>
  </Dialog>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--sp-3); }
.filters { display: flex; gap: var(--sp-2); align-items: center; margin-bottom: var(--sp-3); flex-wrap: wrap; }
.flt { min-width: 170px; }
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
.empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); padding: var(--sp-8); color: var(--text-muted); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.form { display: flex; flex-direction: column; gap: var(--sp-1); }
.form label { font-weight: 600; font-size: 0.85rem; margin-top: var(--sp-2); }
.grid2 { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sp-3); align-items: start; }
.grid2 label { margin-top: 0; display: block; margin-bottom: 4px; }
.span2 { grid-column: 1 / -1; }
@media (max-width: 640px) { .grid2 { grid-template-columns: 1fr; } }
</style>
