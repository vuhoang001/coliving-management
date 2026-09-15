<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import Textarea from 'primevue/textarea'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import DatePicker from 'primevue/datepicker'
import InputNumber from 'primevue/inputnumber'
import ToggleSwitch from 'primevue/toggleswitch'
import { contractApi, bookingApi } from '@/services'
import type { Contract, Booking } from '@/types'
import { extractError } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import {
  contractStatusLabel, contractStatusSeverity, formatDate, formatDay, formatCurrency,
  contractTypeLabel, paymentCycleLabel, label
} from '@/composables/format'

const toast = useToast()
const auth = useAuthStore()

const items = ref<Contract[]>([])
const loading = ref(true)

const fStatus = ref<string | null>(null)
const statusOptions = Object.entries(contractStatusLabel).map(([value, label]) => ({ value, label }))

// ---- Tạo hợp đồng từ đặt phòng ----
const bookings = ref<Booking[]>([])
const showDialog = ref(false)
const saving = ref(false)

interface Form {
  bookingId?: number; startDate: Date | null; endDate: Date | null; terms?: string
  contractType?: string; paymentCycle?: string; noticePeriodDays?: number; lateFeePercent?: number
  utilitiesIncluded?: boolean; maxOccupants?: number; depositPaid?: boolean; renewalTerms?: string
}
const form = ref<Form>(blank())
function blank(): Form {
  return {
    bookingId: undefined, startDate: null, endDate: null, terms: '',
    contractType: 'FixedTerm', paymentCycle: 'Monthly', noticePeriodDays: 30, lateFeePercent: 0,
    utilitiesIncluded: false, maxOccupants: 1, depositPaid: false, renewalTerms: ''
  }
}

const bookingOptions = ref<{ value: number; label: string }[]>([])
const contractTypeOptions = Object.entries(contractTypeLabel).map(([value, label]) => ({ value, label }))
const paymentCycleOptions = Object.entries(paymentCycleLabel).map(([value, label]) => ({ value, label }))

// ---- Xem hợp đồng ----
const showView = ref(false)
const viewItem = ref<Contract | null>(null)

async function load() {
  loading.value = true
  try { items.value = await contractApi.list(fStatus.value ?? undefined) }
  catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { loading.value = false }
}
function applyFilter() { load() }

async function openNew() {
  form.value = blank()
  try {
    const res = await bookingApi.list({ page: 1, pageSize: 100, status: 'Confirmed' })
    bookings.value = res.items
    bookingOptions.value = res.items.map((b) => ({ value: b.id, label: `${b.roomName ?? '—'} - ${b.tenantName ?? '—'}` }))
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  showDialog.value = true
}
async function save() {
  if (!form.value.bookingId) { toast.add({ severity: 'warn', summary: 'Chọn đặt phòng', life: 2500 }); return }
  saving.value = true
  try {
    await contractApi.fromBooking(form.value.bookingId, {
      startDate: form.value.startDate ? form.value.startDate.toISOString() : undefined,
      endDate: form.value.endDate ? form.value.endDate.toISOString() : undefined,
      terms: form.value.terms || undefined,
      contractType: form.value.contractType,
      paymentCycle: form.value.paymentCycle,
      noticePeriodDays: form.value.noticePeriodDays,
      lateFeePercent: form.value.lateFeePercent,
      utilitiesIncluded: form.value.utilitiesIncluded,
      maxOccupants: form.value.maxOccupants,
      depositPaid: form.value.depositPaid,
      renewalTerms: form.value.renewalTerms || undefined
    } as any)
    showDialog.value = false; await load()
    toast.add({ severity: 'success', summary: 'Đã tạo hợp đồng', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { saving.value = false }
}

function openView(c: Contract) { viewItem.value = c; showView.value = true }

// ---- Chấm dứt hợp đồng (Dialog thay cho window.prompt) ----
const showTerminate = ref(false)
const terminating = ref(false)
const terminateReason = ref('')
const terminateTarget = ref<Contract | null>(null)

function openTerminate(c: Contract) {
  terminateTarget.value = c
  terminateReason.value = ''
  showTerminate.value = true
}
async function doTerminate() {
  if (!terminateTarget.value) return
  terminating.value = true
  try {
    await contractApi.terminate(terminateTarget.value.id, terminateReason.value.trim() || undefined)
    showTerminate.value = false; await load()
    toast.add({ severity: 'success', summary: 'Đã chấm dứt', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { terminating.value = false }
}

onMounted(load)
</script>

<template>
  <div class="head">
    <h1>Hợp đồng</h1>
    <Button v-if="auth.isStaff" label="Tạo hợp đồng từ đặt phòng" icon="pi pi-plus" size="small" @click="openNew" />
  </div>

  <div class="filters">
    <Select v-model="fStatus" :options="statusOptions" optionLabel="label" optionValue="value" placeholder="Trạng thái" showClear class="flt" @change="applyFilter" />
    <Button label="Lọc" icon="pi pi-search" size="small" @click="applyFilter" />
  </div>

  <DataTable :value="items" :loading="loading" stripedRows size="small" class="box" paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
    <Column field="id" header="ID" style="width: 60px" />
    <Column field="roomName" header="Phòng" />
    <Column field="tenantName" header="Khách thuê" />
    <Column header="Ngày bắt đầu" style="width: 120px"><template #body="{ data }">{{ formatDay(data.startDate) }}</template></Column>
    <Column header="Ngày kết thúc" style="width: 120px"><template #body="{ data }">{{ formatDay(data.endDate) }}</template></Column>
    <Column header="Loại" style="width: 110px"><template #body="{ data }">{{ label(contractTypeLabel, data.contractType) }}</template></Column>
    <Column header="Chu kỳ" style="width: 110px"><template #body="{ data }">{{ label(paymentCycleLabel, data.paymentCycle) }}</template></Column>
    <Column header="Trạng thái" style="width: 140px">
      <template #body="{ data }"><Tag :value="contractStatusLabel[data.status]" :severity="contractStatusSeverity[data.status]" /></template>
    </Column>
    <Column header="Ngày ký" style="width: 150px"><template #body="{ data }">{{ formatDate(data.signedAt) }}</template></Column>
    <Column header="Thao tác" style="width: 180px">
      <template #body="{ data }">
        <Button label="Xem" icon="pi pi-eye" text size="small" @click="openView(data)" />
        <Button v-if="auth.isManager && data.status === 'Active'" label="Chấm dứt" icon="pi pi-ban" text size="small" severity="danger" @click="openTerminate(data)" />
      </template>
    </Column>
    <template #empty><div class="empty"><i class="pi pi-file" /><p>Chưa có hợp đồng nào.</p></div></template>
  </DataTable>

  <Dialog v-model:visible="showDialog" header="Tạo hợp đồng từ đặt phòng" modal style="width: 680px">
    <div class="form grid2">
      <div class="span2"><label>Đặt phòng (đã xác nhận)</label>
        <Select v-model="form.bookingId" :options="bookingOptions" optionLabel="label" optionValue="value" placeholder="Chọn đặt phòng" class="w-full" filter />
      </div>
      <div><label>Ngày bắt đầu</label><DatePicker v-model="form.startDate" dateFormat="dd/mm/yy" showIcon class="w-full" /></div>
      <div><label>Ngày kết thúc</label><DatePicker v-model="form.endDate" dateFormat="dd/mm/yy" showIcon class="w-full" /></div>
      <div><label>Loại hợp đồng</label><Select v-model="form.contractType" :options="contractTypeOptions" optionLabel="label" optionValue="value" class="w-full" /></div>
      <div><label>Chu kỳ thanh toán</label><Select v-model="form.paymentCycle" :options="paymentCycleOptions" optionLabel="label" optionValue="value" class="w-full" /></div>
      <div><label>Số người tối đa</label><InputNumber v-model="form.maxOccupants" :min="1" class="w-full" inputClass="w-full" /></div>
      <div><label>Báo trước (ngày)</label><InputNumber v-model="form.noticePeriodDays" :min="0" class="w-full" inputClass="w-full" /></div>
      <div><label>Phí trễ hạn (%)</label><InputNumber v-model="form.lateFeePercent" :min="0" :maxFractionDigits="2" class="w-full" inputClass="w-full" /></div>
      <div class="toggle"><label>Bao gồm tiện ích</label><ToggleSwitch v-model="form.utilitiesIncluded" /></div>
      <div class="toggle"><label>Đã đóng cọc</label><ToggleSwitch v-model="form.depositPaid" /></div>
      <div class="span2"><label>Điều khoản</label><Textarea v-model="form.terms" rows="3" class="w-full" autoResize /></div>
      <div class="span2"><label>Điều khoản gia hạn</label><Textarea v-model="form.renewalTerms" rows="2" class="w-full" autoResize /></div>
    </div>
    <template #footer>
      <Button label="Hủy" text @click="showDialog = false" />
      <Button label="Tạo" icon="pi pi-check" :loading="saving" @click="save" />
    </template>
  </Dialog>

  <Dialog v-model:visible="showView" :header="viewItem ? `Hợp đồng #${viewItem.id}` : 'Hợp đồng'" modal style="width: 640px">
    <div v-if="viewItem" class="view">
      <div class="row">
        <span class="k">Trạng thái</span>
        <Tag :value="contractStatusLabel[viewItem.status]" :severity="contractStatusSeverity[viewItem.status]" />
      </div>
      <div class="row"><span class="k">Phòng</span><span>{{ viewItem.roomName ?? '—' }}</span></div>
      <div class="row"><span class="k">Khách thuê</span><span>{{ viewItem.tenantName ?? '—' }}</span></div>
      <div class="row"><span class="k">Ngày bắt đầu</span><span>{{ formatDay(viewItem.startDate) }}</span></div>
      <div class="row"><span class="k">Ngày kết thúc</span><span>{{ formatDay(viewItem.endDate) }}</span></div>
      <div class="row"><span class="k">Ngày ký</span><span>{{ formatDate(viewItem.signedAt) }}</span></div>
      <div v-if="viewItem.contractNumber" class="row"><span class="k">Số hợp đồng</span><span>{{ viewItem.contractNumber }}</span></div>
      <div class="row"><span class="k">Loại hợp đồng</span><span>{{ label(contractTypeLabel, viewItem.contractType) }}</span></div>
      <div class="row"><span class="k">Chu kỳ thanh toán</span><span>{{ label(paymentCycleLabel, viewItem.paymentCycle) }}</span></div>
      <div v-if="viewItem.monthlyRent != null" class="row"><span class="k">Tiền thuê / tháng</span><span>{{ formatCurrency(viewItem.monthlyRent) }}</span></div>
      <div v-if="viewItem.deposit != null" class="row"><span class="k">Tiền cọc</span><span>{{ formatCurrency(viewItem.deposit) }}</span></div>
      <div class="row"><span class="k">Đã đóng cọc</span><span>{{ viewItem.depositPaid ? 'Rồi' : 'Chưa' }}</span></div>
      <div v-if="viewItem.maxOccupants != null" class="row"><span class="k">Số người tối đa</span><span>{{ viewItem.maxOccupants }}</span></div>
      <div v-if="viewItem.noticePeriodDays != null" class="row"><span class="k">Báo trước (ngày)</span><span>{{ viewItem.noticePeriodDays }}</span></div>
      <div v-if="viewItem.lateFeePercent != null" class="row"><span class="k">Phí trễ hạn (%)</span><span>{{ viewItem.lateFeePercent }}</span></div>
      <div class="row"><span class="k">Bao gồm tiện ích</span><span>{{ viewItem.utilitiesIncluded ? 'Có' : 'Không' }}</span></div>
      <div v-if="viewItem.signature" class="row"><span class="k">Chữ ký</span><span>{{ viewItem.signature }}</span></div>
      <div class="terms">
        <span class="k">Điều khoản</span>
        <pre>{{ viewItem.terms || '—' }}</pre>
      </div>
      <div v-if="viewItem.renewalTerms" class="terms">
        <span class="k">Điều khoản gia hạn</span>
        <pre>{{ viewItem.renewalTerms }}</pre>
      </div>
    </div>
    <template #footer>
      <Button label="Đóng" text @click="showView = false" />
    </template>
  </Dialog>

  <!-- Dialog chấm dứt hợp đồng (thay cho window.prompt) -->
  <Dialog v-model:visible="showTerminate" header="Chấm dứt hợp đồng" modal style="width: 480px">
    <div class="form">
      <p class="hint">Nhập lý do chấm dứt hợp đồng {{ terminateTarget ? '#' + terminateTarget.id : '' }} (không bắt buộc).</p>
      <label>Lý do chấm dứt</label>
      <Textarea v-model="terminateReason" rows="3" class="w-full" autoResize placeholder="Ví dụ: khách thuê chuyển đi sớm..." />
    </div>
    <template #footer>
      <Button label="Huỷ" text @click="showTerminate = false" />
      <Button label="Chấm dứt" icon="pi pi-ban" severity="danger" :loading="terminating" @click="doTerminate" />
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
.toggle { display: flex; align-items: center; justify-content: space-between; gap: var(--sp-3); padding-top: var(--sp-4); }
.toggle label { margin-bottom: 0; }
.hint { color: var(--text-muted); font-size: 0.85rem; margin: 0 0 var(--sp-2); }
@media (max-width: 640px) { .grid2 { grid-template-columns: 1fr; } }
.view { display: flex; flex-direction: column; gap: var(--sp-2); }
.view .row { display: flex; gap: var(--sp-3); align-items: center; }
.view .k { font-weight: 600; font-size: 0.85rem; min-width: 120px; color: var(--text-muted); }
.terms { display: flex; flex-direction: column; gap: var(--sp-1); }
.terms pre { white-space: pre-wrap; word-break: break-word; background: var(--surface-hover, #f8f9fa); border: 1px solid var(--border); border-radius: var(--radius-sm); padding: var(--sp-3); margin: 0; font-family: inherit; font-size: 0.9rem; }
</style>
