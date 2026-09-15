<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputNumber from 'primevue/inputnumber'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import Select from 'primevue/select'
import DatePicker from 'primevue/datepicker'
import Tag from 'primevue/tag'
import { serviceRequestApi, serviceApi } from '@/services'
import type { ServiceRequest, ServiceCatalog } from '@/types'
import { extractError } from '@/services/api'
import { formatCurrency, formatDate, serviceRequestStatusLabel, serviceRequestStatusSeverity } from '@/composables/format'

const toast = useToast()

const items = ref<ServiceRequest[]>([])
const services = ref<ServiceCatalog[]>([])
const serviceOptions = ref<{ value: number; label: string }[]>([])
const loading = ref(true)

const showDialog = ref(false)
const saving = ref(false)

interface Form { serviceCatalogId: number | null; quantity: number; scheduledAt: Date | null; note: string; contactPhone: string; locationDetail: string }
const form = ref<Form>(blank())
function blank(): Form { return { serviceCatalogId: null, quantity: 1, scheduledAt: null, note: '', contactPhone: '', locationDetail: '' } }

async function load() {
  loading.value = true
  try {
    items.value = await serviceRequestApi.mine()
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    loading.value = false
  }
}

async function loadServices() {
  try {
    services.value = await serviceApi.list(true)
    serviceOptions.value = services.value.map((s) => ({ value: s.id, label: `${s.name} (${formatCurrency(s.price)})` }))
  } catch { /* ignore */ }
}

function canCancel(r: ServiceRequest): boolean {
  return r.status === 'Pending' || r.status === 'Scheduled'
}

function openNew() {
  form.value = blank()
  showDialog.value = true
}

async function save() {
  if (!form.value.serviceCatalogId) {
    toast.add({ severity: 'warn', summary: 'Chọn dịch vụ', life: 2500 })
    return
  }
  saving.value = true
  try {
    await serviceRequestApi.create({
      serviceCatalogId: form.value.serviceCatalogId,
      quantity: form.value.quantity,
      scheduledAt: form.value.scheduledAt ? form.value.scheduledAt.toISOString() : undefined,
      note: form.value.note || undefined,
      contactPhone: form.value.contactPhone || undefined,
      locationDetail: form.value.locationDetail || undefined
    } as any)
    showDialog.value = false
    await load()
    toast.add({ severity: 'success', summary: 'Đã gửi yêu cầu dịch vụ', life: 2500 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    saving.value = false
  }
}

async function cancel(r: ServiceRequest) {
  try {
    await serviceRequestApi.cancel(r.id)
    await load()
    toast.add({ severity: 'success', summary: 'Đã hủy yêu cầu', life: 2000 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  }
}

onMounted(async () => { await loadServices(); await load() })
</script>

<template>
  <div class="head">
    <h1>Dịch vụ của tôi</h1>
    <Button label="Yêu cầu dịch vụ" icon="pi pi-plus" size="small" @click="openNew" />
  </div>

  <DataTable :value="items" :loading="loading" stripedRows size="small" class="box" paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
    <Column field="id" header="ID" style="width: 60px" />
    <Column field="serviceName" header="Dịch vụ" />
    <Column field="quantity" header="SL" style="width: 70px" />
    <Column header="Lịch hẹn" style="width: 150px"><template #body="{ data }">{{ formatDate(data.scheduledAt) }}</template></Column>
    <Column header="Trạng thái" style="width: 140px">
      <template #body="{ data }"><Tag :value="serviceRequestStatusLabel[data.status]" :severity="serviceRequestStatusSeverity[data.status]" /></template>
    </Column>
    <Column header="Ngày tạo" style="width: 150px"><template #body="{ data }">{{ formatDate(data.createdAt) }}</template></Column>
    <Column header="Thao tác" style="width: 100px">
      <template #body="{ data }">
        <Button v-if="canCancel(data)" label="Hủy" icon="pi pi-times" text size="small" severity="danger" @click="cancel(data)" />
      </template>
    </Column>
    <template #empty><div class="empty"><i class="pi pi-wrench" /><p>Bạn chưa có yêu cầu dịch vụ nào.</p></div></template>
  </DataTable>

  <Dialog v-model:visible="showDialog" header="Yêu cầu dịch vụ" modal style="width: 520px">
    <div class="form">
      <label>Dịch vụ</label>
      <Select v-model="form.serviceCatalogId" :options="serviceOptions" optionLabel="label" optionValue="value" placeholder="Chọn dịch vụ" class="w-full" filter />
      <label>Số lượng</label>
      <InputNumber v-model="form.quantity" :min="1" class="w-full" inputClass="w-full" />
      <label>Lịch hẹn</label>
      <DatePicker v-model="form.scheduledAt" showTime hourFormat="24" dateFormat="dd/mm/yy" showIcon class="w-full" />
      <label>Số điện thoại liên hệ</label>
      <InputText v-model="form.contactPhone" class="w-full" />
      <label>Vị trí cụ thể</label>
      <InputText v-model="form.locationDetail" class="w-full" placeholder="Ví dụ: phòng 201, tầng 2..." />
      <label>Ghi chú</label>
      <Textarea v-model="form.note" rows="3" class="w-full" autoResize />
    </div>
    <template #footer>
      <Button label="Hủy" text @click="showDialog = false" />
      <Button label="Gửi" icon="pi pi-check" :loading="saving" @click="save" />
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
