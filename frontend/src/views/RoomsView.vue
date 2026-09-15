<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import Textarea from 'primevue/textarea'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import ToggleSwitch from 'primevue/toggleswitch'
import FileUpload from 'primevue/fileupload'
import { roomApi, buildingApi, apartmentApi, uploadApi } from '@/services'
import type { Room, Building, Apartment } from '@/types'
import { extractError } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { formatCurrency, roomStatusLabel, roomStatusSeverity, roomTypeLabel, label } from '@/composables/format'

const toast = useToast()
const confirm = useConfirm()
const auth = useAuthStore()

const items = ref<Room[]>([])
const buildings = ref<Building[]>([])
const apartments = ref<Apartment[]>([])
const loading = ref(true)
const totalRecords = ref(0)
const page = ref(1)
const pageSize = ref(10)

const fBuilding = ref<number | null>(null)
const fStatus = ref<string | null>(null)
const fKeyword = ref('')

const statusOptions = Object.entries(roomStatusLabel).map(([value, label]) => ({ value, label }))
const typeOptions = ['Single', 'Double', 'Shared', 'Studio', 'Suite'].map(v => ({ value: v, label: roomTypeLabel[v] ?? v }))

const showDialog = ref(false)
const saving = ref(false)
const uploading = ref(false)

interface Form {
  id?: number; apartmentId?: number; name: string; code?: string; type?: string
  area?: number; capacity?: number; price: number; status: string; description?: string; photoUrl?: string
  deposit?: number; hasWindow?: boolean; hasPrivateBathroom?: boolean; hasAirConditioner?: boolean
  electricityUnitPrice?: number; waterUnitPrice?: number; internetFee?: number; notes?: string
}
const form = ref<Form>(blank())
function blank(): Form {
  return {
    name: '', code: '', type: 'Single', area: undefined, capacity: 1, price: 0, status: 'Available', description: '', photoUrl: '',
    deposit: 0, hasWindow: true, hasPrivateBathroom: false, hasAirConditioner: true,
    electricityUnitPrice: 3500, waterUnitPrice: 15000, internetFee: 100000, notes: ''
  }
}

async function load() {
  loading.value = true
  try {
    const res = await roomApi.list({
      page: page.value, pageSize: pageSize.value,
      buildingId: fBuilding.value ?? undefined, status: fStatus.value ?? undefined, keyword: fKeyword.value || undefined
    })
    items.value = res.items
    totalRecords.value = res.totalItems
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { loading.value = false }
}
async function loadRefs() {
  try { buildings.value = await buildingApi.list(); apartments.value = await apartmentApi.list() } catch { /* */ }
}
function onPage(e: { page: number; rows: number }) { page.value = e.page + 1; pageSize.value = e.rows; load() }
function applyFilter() { page.value = 1; load() }

function openNew() { form.value = blank(); showDialog.value = true }
function openEdit(r: Room) {
  form.value = {
    id: r.id, apartmentId: r.apartmentId, name: r.name, code: r.code, type: r.type, area: r.area,
    capacity: r.capacity, price: r.price, status: r.status, description: r.description, photoUrl: r.photoUrl,
    deposit: r.deposit ?? 0, hasWindow: r.hasWindow ?? true, hasPrivateBathroom: r.hasPrivateBathroom ?? false,
    hasAirConditioner: r.hasAirConditioner ?? true, electricityUnitPrice: r.electricityUnitPrice ?? 3500,
    waterUnitPrice: r.waterUnitPrice ?? 15000, internetFee: r.internetFee ?? 100000, notes: r.notes
  }
  showDialog.value = true
}
async function onUpload(e: { files: File | File[] }) {
  const file = Array.isArray(e.files) ? e.files[0] : e.files
  if (!file) return
  uploading.value = true
  try { const { url } = await uploadApi.image(file, 'rooms'); form.value.photoUrl = url }
  catch (err) { toast.add({ severity: 'error', summary: 'Lỗi tải ảnh', detail: extractError(err), life: 3000 }) }
  finally { uploading.value = false }
}
async function save() {
  if (!form.value.name.trim() || !form.value.apartmentId) { toast.add({ severity: 'warn', summary: 'Nhập tên & chọn căn hộ', life: 2500 }); return }
  saving.value = true
  try {
    const payload = { ...form.value, name: form.value.name.trim() } as any
    if (form.value.id) await roomApi.update(form.value.id, payload)
    else await roomApi.create(payload)
    showDialog.value = false; await load()
    toast.add({ severity: 'success', summary: 'Đã lưu', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { saving.value = false }
}
async function changeStatus(r: Room, value: string) {
  try { await roomApi.setStatus(r.id, value); await load(); toast.add({ severity: 'success', summary: 'Đã đổi trạng thái', life: 2000 }) }
  catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
}
function remove(r: Room) {
  confirm.require({
    message: `Xóa phòng "${r.name}"?`, header: 'Xác nhận', icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Hủy', acceptLabel: 'Xóa', acceptClass: 'p-button-danger',
    accept: async () => {
      try { await roomApi.remove(r.id); await load(); toast.add({ severity: 'success', summary: 'Đã xóa', life: 2000 }) }
      catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
    }
  })
}
onMounted(async () => { await loadRefs(); await load() })
</script>

<template>
  <div class="head">
    <h1>Phòng</h1>
    <Button v-if="auth.isManager" label="Thêm phòng" icon="pi pi-plus" size="small" @click="openNew" />
  </div>

  <div class="filters">
    <Select v-model="fBuilding" :options="buildings" optionLabel="name" optionValue="id" placeholder="Toà nhà" showClear class="flt" @change="applyFilter" />
    <Select v-model="fStatus" :options="statusOptions" optionLabel="label" optionValue="value" placeholder="Trạng thái" showClear class="flt" @change="applyFilter" />
    <span class="p-input-icon-left search">
      <InputText v-model="fKeyword" placeholder="Tìm tên/mã phòng" @keyup.enter="applyFilter" />
    </span>
    <Button label="Lọc" icon="pi pi-search" size="small" @click="applyFilter" />
  </div>

  <DataTable :value="items" :loading="loading" stripedRows size="small" class="box" lazy paginator
    :rows="pageSize" :totalRecords="totalRecords" :rowsPerPageOptions="[10, 20, 50]" @page="onPage">
    <Column header="Ảnh" style="width: 70px">
      <template #body="{ data }"><img :src="data.photoUrl || 'https://placehold.co/48?text=P'" class="thumb" alt="" /></template>
    </Column>
    <Column field="name" header="Phòng" />
    <Column field="buildingName" header="Toà nhà" />
    <Column field="apartmentName" header="Căn hộ" />
    <Column header="Loại" style="width: 90px"><template #body="{ data }">{{ label(roomTypeLabel, data.type) }}</template></Column>
    <Column header="Giá / tháng" style="width: 130px"><template #body="{ data }">{{ formatCurrency(data.price) }}</template></Column>
    <Column header="Điều hoà" style="width: 80px"><template #body="{ data }"><i :class="data.hasAirConditioner ? 'pi pi-check' : 'pi pi-minus'" /></template></Column>
    <Column header="Trạng thái" style="width: 130px">
      <template #body="{ data }"><Tag :value="roomStatusLabel[data.status]" :severity="roomStatusSeverity[data.status]" /></template>
    </Column>
    <Column v-if="auth.isManager" header="Thao tác" style="width: 160px">
      <template #body="{ data }">
        <Button icon="pi pi-pencil" text size="small" @click="openEdit(data)" />
        <Button icon="pi pi-sync" text size="small" v-tooltip.top="'Đổi trạng thái'" @click="changeStatus(data, data.status === 'Available' ? 'Maintenance' : 'Available')" />
        <Button icon="pi pi-trash" text size="small" severity="danger" @click="remove(data)" />
      </template>
    </Column>
    <template #empty><div class="empty"><i class="pi pi-home" /><p>Chưa có phòng nào.</p></div></template>
  </DataTable>

  <Dialog v-model:visible="showDialog" :header="form.id ? 'Sửa phòng' : 'Thêm phòng'" modal style="width: 620px">
    <div class="form grid2">
      <div><label>Căn hộ</label><Select v-model="form.apartmentId" :options="apartments" optionLabel="name" optionValue="id" placeholder="Chọn căn hộ" class="w-full" filter /></div>
      <div><label>Tên phòng</label><InputText v-model="form.name" class="w-full" /></div>
      <div><label>Mã phòng</label><InputText v-model="form.code" class="w-full" /></div>
      <div><label>Loại</label><Select v-model="form.type" :options="typeOptions" optionLabel="label" optionValue="value" class="w-full" /></div>
      <div><label>Diện tích (m²)</label><InputNumber v-model="form.area" :min="0" class="w-full" inputClass="w-full" /></div>
      <div><label>Sức chứa</label><InputNumber v-model="form.capacity" :min="1" class="w-full" inputClass="w-full" /></div>
      <div><label>Giá / tháng (VNĐ)</label><InputNumber v-model="form.price" :min="0" class="w-full" inputClass="w-full" /></div>
      <div><label>Trạng thái</label><Select v-model="form.status" :options="statusOptions" optionLabel="label" optionValue="value" class="w-full" /></div>
      <div><label>Tiền cọc (VNĐ)</label><InputNumber v-model="form.deposit" :min="0" class="w-full" inputClass="w-full" /></div>
      <div><label>Đơn giá điện (VNĐ/kWh)</label><InputNumber v-model="form.electricityUnitPrice" :min="0" class="w-full" inputClass="w-full" /></div>
      <div><label>Đơn giá nước (VNĐ/m³)</label><InputNumber v-model="form.waterUnitPrice" :min="0" class="w-full" inputClass="w-full" /></div>
      <div><label>Phí Internet (VNĐ/tháng)</label><InputNumber v-model="form.internetFee" :min="0" class="w-full" inputClass="w-full" /></div>
      <div class="toggle"><label>Có cửa sổ</label><ToggleSwitch v-model="form.hasWindow" /></div>
      <div class="toggle"><label>WC riêng</label><ToggleSwitch v-model="form.hasPrivateBathroom" /></div>
      <div class="toggle"><label>Có điều hoà</label><ToggleSwitch v-model="form.hasAirConditioner" /></div>
      <div class="span2"><label>Ảnh phòng</label>
        <div class="img-row">
          <img :src="form.photoUrl || 'https://placehold.co/64?text=P'" class="preview" alt="" />
          <FileUpload mode="basic" customUpload auto accept="image/*" :maxFileSize="5000000" chooseLabel="Tải ảnh" chooseIcon="pi pi-upload" :disabled="uploading" @uploader="onUpload" />
        </div>
      </div>
      <div class="span2"><label>Mô tả</label><Textarea v-model="form.description" rows="2" class="w-full" autoResize /></div>
      <div class="span2"><label>Ghi chú</label><Textarea v-model="form.notes" rows="2" class="w-full" autoResize /></div>
    </div>
    <template #footer>
      <Button label="Hủy" text @click="showDialog = false" />
      <Button label="Lưu" icon="pi pi-check" :loading="saving" @click="save" />
    </template>
  </Dialog>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--sp-3); }
.filters { display: flex; gap: var(--sp-2); align-items: center; margin-bottom: var(--sp-3); flex-wrap: wrap; }
.flt { min-width: 170px; }
.search :deep(input) { min-width: 220px; }
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
.thumb { width: 48px; height: 48px; object-fit: cover; border-radius: var(--radius-sm); border: 1px solid var(--border); }
.empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); padding: var(--sp-8); color: var(--text-muted); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.form label { font-weight: 600; font-size: 0.85rem; display: block; margin-bottom: 4px; }
.grid2 { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sp-3); align-items: start; }
.span2 { grid-column: 1 / -1; }
.toggle { display: flex; align-items: center; justify-content: space-between; gap: var(--sp-3); padding-top: var(--sp-4); }
.toggle label { margin-bottom: 0; }
@media (max-width: 640px) { .grid2 { grid-template-columns: 1fr; } }
.img-row { display: flex; gap: var(--sp-3); align-items: center; }
.preview { width: 64px; height: 64px; object-fit: cover; border-radius: var(--radius-sm); border: 1px solid var(--border); }
</style>
