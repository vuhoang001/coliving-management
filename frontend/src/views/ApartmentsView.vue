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
import ToggleSwitch from 'primevue/toggleswitch'
import { apartmentApi, buildingApi } from '@/services'
import type { Apartment, Building } from '@/types'
import { extractError } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { furnishingLabel, directionLabel, label } from '@/composables/format'

const toast = useToast()
const confirm = useConfirm()
const auth = useAuthStore()

const items = ref<Apartment[]>([])
const buildings = ref<Building[]>([])
const loading = ref(true)
const filterBuilding = ref<number | null>(null)
const showDialog = ref(false)
const saving = ref(false)

interface Form {
  id?: number; buildingId?: number; name: string; floor?: number; description?: string
  direction?: string; furnishing?: string; hasBalcony?: boolean; maintenanceFee?: number; notes?: string
}
const form = ref<Form>(blank())
function blank(): Form {
  return {
    buildingId: filterBuilding.value ?? undefined, name: '', floor: undefined, description: '',
    direction: undefined, furnishing: 'Basic', hasBalcony: false, maintenanceFee: 0, notes: ''
  }
}
const directionOptions = Object.keys(directionLabel).map(v => ({ value: v, label: directionLabel[v] }))
const furnishingOptions = Object.entries(furnishingLabel).map(([value, label]) => ({ value, label }))

async function load() {
  loading.value = true
  try { items.value = await apartmentApi.list(filterBuilding.value ?? undefined) }
  catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { loading.value = false }
}
async function loadBuildings() { try { buildings.value = await buildingApi.list() } catch { /* */ } }

function openNew() { form.value = blank(); showDialog.value = true }
function openEdit(a: Apartment) {
  form.value = {
    id: a.id, buildingId: a.buildingId, name: a.name, floor: a.floor, description: a.description,
    direction: a.direction, furnishing: a.furnishing ?? 'Basic', hasBalcony: a.hasBalcony ?? false,
    maintenanceFee: a.maintenanceFee ?? 0, notes: a.notes
  }
  showDialog.value = true
}

async function save() {
  if (!form.value.name.trim() || !form.value.buildingId) { toast.add({ severity: 'warn', summary: 'Nhập tên & chọn toà nhà', life: 2500 }); return }
  saving.value = true
  try {
    const payload = {
      buildingId: form.value.buildingId, name: form.value.name.trim(), floor: form.value.floor, description: form.value.description,
      direction: form.value.direction, furnishing: form.value.furnishing, hasBalcony: form.value.hasBalcony,
      maintenanceFee: form.value.maintenanceFee, notes: form.value.notes
    }
    if (form.value.id) await apartmentApi.update(form.value.id, payload)
    else await apartmentApi.create(payload)
    showDialog.value = false; await load()
    toast.add({ severity: 'success', summary: 'Đã lưu', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { saving.value = false }
}
function remove(a: Apartment) {
  confirm.require({
    message: `Xóa căn hộ "${a.name}"?`, header: 'Xác nhận', icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Hủy', acceptLabel: 'Xóa', acceptClass: 'p-button-danger',
    accept: async () => {
      try { await apartmentApi.remove(a.id); await load(); toast.add({ severity: 'success', summary: 'Đã xóa', life: 2000 }) }
      catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
    }
  })
}
onMounted(async () => { await loadBuildings(); await load() })
</script>

<template>
  <div class="head">
    <h1>Căn hộ</h1>
    <div class="actions">
      <Select v-model="filterBuilding" :options="buildings" optionLabel="name" optionValue="id" placeholder="Tất cả toà nhà" showClear class="flt" @change="load" />
      <Button v-if="auth.isManager" label="Thêm căn hộ" icon="pi pi-plus" size="small" @click="openNew" />
    </div>
  </div>

  <DataTable :value="items" :loading="loading" stripedRows size="small" class="box" paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
    <Column field="id" header="ID" style="width: 60px" />
    <Column field="name" header="Tên căn hộ" />
    <Column field="buildingName" header="Toà nhà" />
    <Column field="floor" header="Tầng" style="width: 90px" />
    <Column header="Hướng" style="width: 110px"><template #body="{ data }">{{ data.direction || '—' }}</template></Column>
    <Column header="Nội thất" style="width: 120px"><template #body="{ data }">{{ label(furnishingLabel, data.furnishing) }}</template></Column>
    <Column header="Số phòng" style="width: 100px"><template #body="{ data }">{{ data.roomCount ?? '—' }}</template></Column>
    <Column v-if="auth.isManager" header="Thao tác" style="width: 120px">
      <template #body="{ data }">
        <Button icon="pi pi-pencil" text size="small" @click="openEdit(data)" />
        <Button icon="pi pi-trash" text size="small" severity="danger" @click="remove(data)" />
      </template>
    </Column>
    <template #empty><div class="empty"><i class="pi pi-th-large" /><p>Chưa có căn hộ nào.</p></div></template>
  </DataTable>

  <Dialog v-model:visible="showDialog" :header="form.id ? 'Sửa căn hộ' : 'Thêm căn hộ'" modal style="width: 620px">
    <div class="form grid2">
      <div class="span2"><label>Toà nhà</label>
        <Select v-model="form.buildingId" :options="buildings" optionLabel="name" optionValue="id" placeholder="Chọn toà nhà" class="w-full" />
      </div>
      <div><label>Tên căn hộ</label><InputText v-model="form.name" class="w-full" /></div>
      <div><label>Tầng</label><InputNumber v-model="form.floor" :min="0" class="w-full" inputClass="w-full" /></div>
      <div><label>Hướng</label><Select v-model="form.direction" :options="directionOptions" optionLabel="label" optionValue="value" placeholder="Chọn hướng" showClear class="w-full" /></div>
      <div><label>Nội thất</label><Select v-model="form.furnishing" :options="furnishingOptions" optionLabel="label" optionValue="value" class="w-full" /></div>
      <div><label>Phí bảo trì (VNĐ)</label><InputNumber v-model="form.maintenanceFee" :min="0" class="w-full" inputClass="w-full" /></div>
      <div class="toggle"><label>Có ban công</label><ToggleSwitch v-model="form.hasBalcony" /></div>
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
.head { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--sp-4); gap: var(--sp-3); flex-wrap: wrap; }
.actions { display: flex; gap: var(--sp-2); align-items: center; }
.flt { min-width: 200px; }
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
@media (max-width: 640px) { .grid2 { grid-template-columns: 1fr; } }
</style>
