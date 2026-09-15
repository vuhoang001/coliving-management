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
import { amenityApi, buildingApi } from '@/services'
import type { Amenity, Building } from '@/types'
import { extractError } from '@/services/api'
import { useAuthStore } from '@/stores/auth'

const toast = useToast()
const confirm = useConfirm()
const auth = useAuthStore()

const items = ref<Amenity[]>([])
const buildings = ref<Building[]>([])
const loading = ref(true)

const fBuilding = ref<number | null>(null)

const showDialog = ref(false)
const saving = ref(false)

interface Form {
  id?: number; name: string; buildingId?: number | null; description?: string
  capacity?: number; openTime?: string; closeTime?: string; isActive: boolean
}
const form = ref<Form>(blank())
function blank(): Form {
  return { name: '', buildingId: null, description: '', capacity: undefined, openTime: '08:00', closeTime: '22:00', isActive: true }
}

async function load() {
  loading.value = true
  try { items.value = await amenityApi.list(fBuilding.value ?? undefined) }
  catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { loading.value = false }
}
async function loadRefs() {
  try { buildings.value = await buildingApi.list() } catch { /* */ }
}
function applyFilter() { load() }
function clearFilter() { fBuilding.value = null; load() }

function openNew() { form.value = blank(); showDialog.value = true }
function openEdit(a: Amenity) {
  form.value = {
    id: a.id, name: a.name, buildingId: a.buildingId ?? null, description: a.description,
    capacity: a.capacity, openTime: a.openTime, closeTime: a.closeTime, isActive: a.isActive ?? true
  }
  showDialog.value = true
}

async function save() {
  if (!form.value.name.trim()) { toast.add({ severity: 'warn', summary: 'Nhập tên tiện ích', life: 2500 }); return }
  saving.value = true
  try {
    const payload = {
      name: form.value.name.trim(), buildingId: form.value.buildingId ?? undefined,
      description: form.value.description, capacity: form.value.capacity,
      openTime: form.value.openTime, closeTime: form.value.closeTime, isActive: form.value.isActive
    }
    if (form.value.id) await amenityApi.update(form.value.id, payload)
    else await amenityApi.create(payload)
    showDialog.value = false; await load()
    toast.add({ severity: 'success', summary: 'Đã lưu', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { saving.value = false }
}
function remove(a: Amenity) {
  confirm.require({
    message: `Xóa tiện ích "${a.name}"?`, header: 'Xác nhận', icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Hủy', acceptLabel: 'Xóa', acceptClass: 'p-button-danger',
    accept: async () => {
      try { await amenityApi.remove(a.id); await load(); toast.add({ severity: 'success', summary: 'Đã xóa', life: 2000 }) }
      catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
    }
  })
}
onMounted(async () => { await loadRefs(); await load() })
</script>

<template>
  <div class="head">
    <h1>Tiện ích</h1>
    <Button v-if="auth.isManager" label="Thêm tiện ích" icon="pi pi-plus" size="small" @click="openNew" />
  </div>

  <div class="filters">
    <Select v-model="fBuilding" :options="buildings" optionLabel="name" optionValue="id" placeholder="Toà nhà" showClear class="flt" @change="applyFilter" />
    <Button label="Xoá lọc" icon="pi pi-times" size="small" text @click="clearFilter" />
  </div>

  <DataTable :value="items" :loading="loading" stripedRows size="small" class="box" paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
    <Column field="id" header="ID" style="width: 60px" />
    <Column field="name" header="Tên" />
    <Column field="buildingName" header="Toà nhà" />
    <Column field="capacity" header="Sức chứa" style="width: 100px">
      <template #body="{ data }">{{ data.capacity ?? '—' }}</template>
    </Column>
    <Column field="openTime" header="Mở cửa" style="width: 90px">
      <template #body="{ data }">{{ data.openTime ?? '—' }}</template>
    </Column>
    <Column field="closeTime" header="Đóng cửa" style="width: 90px">
      <template #body="{ data }">{{ data.closeTime ?? '—' }}</template>
    </Column>
    <Column header="Trạng thái" style="width: 120px">
      <template #body="{ data }">
        <Tag :value="data.isActive ? 'Hoạt động' : 'Ngừng'" :severity="data.isActive ? 'success' : 'danger'" />
      </template>
    </Column>
    <Column field="description" header="Mô tả" />
    <Column v-if="auth.isManager" header="Thao tác" style="width: 110px">
      <template #body="{ data }">
        <Button icon="pi pi-pencil" text size="small" @click="openEdit(data)" />
        <Button icon="pi pi-trash" text size="small" severity="danger" @click="remove(data)" />
      </template>
    </Column>
    <template #empty><div class="empty"><i class="pi pi-th-large" /><p>Chưa có tiện ích nào.</p></div></template>
  </DataTable>

  <Dialog v-model:visible="showDialog" :header="form.id ? 'Sửa tiện ích' : 'Thêm tiện ích'" modal style="width: 560px">
    <div class="form">
      <label>Tên tiện ích</label>
      <InputText v-model="form.name" class="w-full" />
      <label>Toà nhà</label>
      <Select v-model="form.buildingId" :options="buildings" optionLabel="name" optionValue="id" placeholder="Chọn toà nhà (tuỳ chọn)" showClear class="w-full" />
      <label>Sức chứa</label>
      <InputNumber v-model="form.capacity" :min="0" class="w-full" inputClass="w-full" />
      <label>Giờ mở cửa</label>
      <InputText v-model="form.openTime" class="w-full" placeholder="08:00" />
      <label>Giờ đóng cửa</label>
      <InputText v-model="form.closeTime" class="w-full" placeholder="22:00" />
      <label>Mô tả</label>
      <Textarea v-model="form.description" rows="3" class="w-full" autoResize />
      <label>Kích hoạt</label>
      <ToggleSwitch v-model="form.isActive" />
    </div>
    <template #footer>
      <Button label="Hủy" text @click="showDialog = false" />
      <Button label="Lưu" icon="pi pi-check" :loading="saving" @click="save" />
    </template>
  </Dialog>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--sp-4); }
.filters { display: flex; gap: var(--sp-2); align-items: center; margin-bottom: var(--sp-3); flex-wrap: wrap; }
.flt { min-width: 170px; }
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
.empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); padding: var(--sp-8); color: var(--text-muted); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.form { display: flex; flex-direction: column; gap: var(--sp-1); }
.form label { font-weight: 600; font-size: 0.85rem; margin-top: var(--sp-2); }
</style>
