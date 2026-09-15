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
import DatePicker from 'primevue/datepicker'
import { assetApi, buildingApi } from '@/services'
import type { AssetFilter } from '@/services'
import type { Asset, Building } from '@/types'
import { extractError } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { formatCurrency, assetStatusLabel, assetStatusSeverity, assetCategoryLabel, label } from '@/composables/format'

const toast = useToast()
const confirm = useConfirm()
const auth = useAuthStore()

const categoryOptions = ['Furniture', 'Appliance', 'Electronics', 'Plumbing', 'Other'].map(v => ({ value: v, label: assetCategoryLabel[v] ?? v }))
const statusOptions = Object.entries(assetStatusLabel).map(([value, lbl]) => ({ value, label: lbl }))
  .filter(o => ['InUse', 'Available', 'Broken', 'Maintenance', 'Disposed'].includes(o.value))

const items = ref<Asset[]>([])
const buildings = ref<Building[]>([])
const loading = ref(true)
const totalRecords = ref(0)
const page = ref(1)
const pageSize = ref(10)

const fCategory = ref<string | null>(null)
const fStatus = ref<string | null>(null)
const fBuilding = ref<number | null>(null)
const fRoom = ref<number | null>(null)
const fKeyword = ref('')

const showDialog = ref(false)
const saving = ref(false)

interface Form {
  id?: number; name: string; code?: string; category?: string; status: string
  buildingId?: number; roomId?: number; quantity?: number; purchasePrice?: number
  purchaseDate?: Date | null; note?: string
  brand?: string; model?: string; serialNumber?: string; supplier?: string; warrantyUntil?: Date | null
}
const form = ref<Form>(blank())
function blank(): Form {
  return {
    name: '', code: '', category: 'Furniture', status: 'Available', buildingId: undefined, roomId: undefined,
    quantity: 1, purchasePrice: 0, purchaseDate: null, note: '',
    brand: '', model: '', serialNumber: '', supplier: '', warrantyUntil: null
  }
}

async function load() {
  loading.value = true
  try {
    const filter: AssetFilter = {
      page: page.value, pageSize: pageSize.value,
      category: fCategory.value ?? undefined, status: fStatus.value ?? undefined,
      buildingId: fBuilding.value ?? undefined, roomId: fRoom.value ?? undefined,
      keyword: fKeyword.value || undefined
    }
    const res = await assetApi.list(filter)
    items.value = res.items
    totalRecords.value = res.totalItems
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { loading.value = false }
}
async function loadRefs() {
  try { buildings.value = await buildingApi.list() } catch { /* */ }
}
function onPage(e: { page: number; rows: number }) { page.value = e.page + 1; pageSize.value = e.rows; load() }
function applyFilter() { page.value = 1; load() }

function openNew() { form.value = blank(); showDialog.value = true }
function openEdit(a: Asset) {
  form.value = {
    id: a.id, name: a.name, code: a.code, category: a.category, status: a.status || 'Available',
    buildingId: a.buildingId, roomId: a.roomId, quantity: a.quantity, purchasePrice: a.purchasePrice,
    purchaseDate: a.purchaseDate ? new Date(a.purchaseDate) : null, note: a.note,
    brand: a.brand, model: a.model, serialNumber: a.serialNumber, supplier: a.supplier,
    warrantyUntil: a.warrantyUntil ? new Date(a.warrantyUntil) : null
  }
  showDialog.value = true
}
async function save() {
  if (!form.value.name.trim()) { toast.add({ severity: 'warn', summary: 'Nhập tên tài sản', life: 2500 }); return }
  saving.value = true
  try {
    const payload: Partial<Asset> = {
      name: form.value.name.trim(), code: form.value.code, category: form.value.category, status: form.value.status,
      buildingId: form.value.buildingId, roomId: form.value.roomId, quantity: form.value.quantity,
      purchasePrice: form.value.purchasePrice,
      purchaseDate: form.value.purchaseDate ? form.value.purchaseDate.toISOString() : undefined,
      note: form.value.note,
      brand: form.value.brand, model: form.value.model, serialNumber: form.value.serialNumber,
      supplier: form.value.supplier,
      warrantyUntil: form.value.warrantyUntil ? form.value.warrantyUntil.toISOString() : undefined
    }
    if (form.value.id) await assetApi.update(form.value.id, payload)
    else await assetApi.create(payload)
    showDialog.value = false; await load()
    toast.add({ severity: 'success', summary: 'Đã lưu', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { saving.value = false }
}
function remove(a: Asset) {
  confirm.require({
    message: `Xóa tài sản "${a.name}"?`, header: 'Xác nhận', icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Hủy', acceptLabel: 'Xóa', acceptClass: 'p-button-danger',
    accept: async () => {
      try { await assetApi.remove(a.id); await load(); toast.add({ severity: 'success', summary: 'Đã xóa', life: 2000 }) }
      catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
    }
  })
}
onMounted(async () => { await loadRefs(); await load() })
</script>

<template>
  <div class="head">
    <h1>Tài sản</h1>
    <Button v-if="auth.isStaff" label="Thêm tài sản" icon="pi pi-plus" size="small" @click="openNew" />
  </div>

  <div class="filters">
    <Select v-model="fCategory" :options="categoryOptions" optionLabel="label" optionValue="value" placeholder="Danh mục" showClear class="flt" @change="applyFilter" />
    <Select v-model="fStatus" :options="statusOptions" optionLabel="label" optionValue="value" placeholder="Trạng thái" showClear class="flt" @change="applyFilter" />
    <Select v-model="fBuilding" :options="buildings" optionLabel="name" optionValue="id" placeholder="Toà nhà" showClear class="flt" @change="applyFilter" />
    <InputNumber v-model="fRoom" placeholder="ID phòng" :useGrouping="false" showButtons class="flt-room" inputClass="w-full" />
    <span class="p-input-icon-left search">
      <InputText v-model="fKeyword" placeholder="Tìm tên/mã tài sản" @keyup.enter="applyFilter" />
    </span>
    <Button label="Lọc" icon="pi pi-search" size="small" @click="applyFilter" />
  </div>

  <DataTable :value="items" :loading="loading" stripedRows size="small" class="box" lazy paginator
    :rows="pageSize" :totalRecords="totalRecords" :rowsPerPageOptions="[10, 20, 50]" @page="onPage">
    <Column field="id" header="ID" style="width: 60px" />
    <Column field="name" header="Tên" />
    <Column field="code" header="Mã" style="width: 110px" />
    <Column header="Danh mục" style="width: 120px"><template #body="{ data }">{{ label(assetCategoryLabel, data.category) }}</template></Column>
    <Column field="brand" header="Hãng" style="width: 110px"><template #body="{ data }">{{ data.brand || '—' }}</template></Column>
    <Column field="buildingName" header="Toà nhà" />
    <Column field="roomName" header="Phòng" />
    <Column field="quantity" header="Số lượng" style="width: 100px" />
    <Column header="Trạng thái" style="width: 130px">
      <template #body="{ data }"><Tag :value="label(assetStatusLabel, data.status)" :severity="assetStatusSeverity[data.status]" /></template>
    </Column>
    <Column header="Giá mua" style="width: 130px">
      <template #body="{ data }">{{ data.purchasePrice != null ? formatCurrency(data.purchasePrice) : '—' }}</template>
    </Column>
    <Column v-if="auth.isStaff" header="Thao tác" style="width: 120px">
      <template #body="{ data }">
        <Button icon="pi pi-pencil" text size="small" @click="openEdit(data)" />
        <Button v-if="auth.isManager" icon="pi pi-trash" text size="small" severity="danger" @click="remove(data)" />
      </template>
    </Column>
    <template #empty><div class="empty"><i class="pi pi-box" /><p>Chưa có tài sản nào.</p></div></template>
  </DataTable>

  <Dialog v-model:visible="showDialog" :header="form.id ? 'Sửa tài sản' : 'Thêm tài sản'" modal style="width: 620px">
    <div class="form grid2">
      <div><label>Tên tài sản</label><InputText v-model="form.name" class="w-full" /></div>
      <div><label>Mã</label><InputText v-model="form.code" class="w-full" /></div>
      <div><label>Danh mục</label><Select v-model="form.category" :options="categoryOptions" optionLabel="label" optionValue="value" class="w-full" /></div>
      <div><label>Trạng thái</label><Select v-model="form.status" :options="statusOptions" optionLabel="label" optionValue="value" class="w-full" /></div>
      <div><label>Toà nhà</label><Select v-model="form.buildingId" :options="buildings" optionLabel="name" optionValue="id" placeholder="Chọn toà nhà" showClear class="w-full" filter /></div>
      <div><label>ID phòng (tuỳ chọn)</label><InputNumber v-model="form.roomId" :useGrouping="false" :min="0" class="w-full" inputClass="w-full" /></div>
      <div><label>Số lượng</label><InputNumber v-model="form.quantity" :min="0" class="w-full" inputClass="w-full" /></div>
      <div><label>Giá mua (VNĐ)</label><InputNumber v-model="form.purchasePrice" :min="0" class="w-full" inputClass="w-full" /></div>
      <div><label>Ngày mua</label><DatePicker v-model="form.purchaseDate" dateFormat="dd/mm/yy" showButtonBar class="w-full" /></div>
      <div><label>Bảo hành đến</label><DatePicker v-model="form.warrantyUntil" dateFormat="dd/mm/yy" showButtonBar class="w-full" /></div>
      <div><label>Hãng</label><InputText v-model="form.brand" class="w-full" /></div>
      <div><label>Model</label><InputText v-model="form.model" class="w-full" /></div>
      <div><label>Số serial</label><InputText v-model="form.serialNumber" class="w-full" /></div>
      <div><label>Nhà cung cấp</label><InputText v-model="form.supplier" class="w-full" /></div>
      <div class="span2"><label>Ghi chú</label><Textarea v-model="form.note" rows="2" class="w-full" autoResize /></div>
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
.flt-room { width: 130px; }
.search :deep(input) { min-width: 220px; }
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
.empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); padding: var(--sp-8); color: var(--text-muted); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.form label { font-weight: 600; font-size: 0.85rem; display: block; margin-bottom: 4px; }
.grid2 { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sp-3); }
.span2 { grid-column: 1 / -1; }
</style>
