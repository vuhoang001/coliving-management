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
import ToggleSwitch from 'primevue/toggleswitch'
import { buildingApi } from '@/services'
import type { Building } from '@/types'
import { extractError } from '@/services/api'
import { useAuthStore } from '@/stores/auth'

const toast = useToast()
const confirm = useConfirm()
const auth = useAuthStore()

const items = ref<Building[]>([])
const loading = ref(true)
const showDialog = ref(false)
const saving = ref(false)

interface Form {
  id?: number; name: string; address?: string; description?: string; floors?: number
  district?: string; ward?: string; contactPhone?: string; contactEmail?: string
  yearBuilt?: number; totalFloorArea?: number; parkingSlots?: number; hasElevator?: boolean; notes?: string
}
const form = ref<Form>(blank())
function blank(): Form {
  return {
    name: '', address: '', description: '', floors: undefined,
    district: '', ward: '', contactPhone: '', contactEmail: '',
    yearBuilt: undefined, totalFloorArea: undefined, parkingSlots: 0, hasElevator: true, notes: ''
  }
}

async function load() {
  loading.value = true
  try { items.value = await buildingApi.list() }
  catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { loading.value = false }
}
function openNew() { form.value = blank(); showDialog.value = true }
function openEdit(b: Building) {
  form.value = {
    id: b.id, name: b.name, address: b.address, description: b.description, floors: b.floors,
    district: b.district, ward: b.ward, contactPhone: b.contactPhone, contactEmail: b.contactEmail,
    yearBuilt: b.yearBuilt, totalFloorArea: b.totalFloorArea, parkingSlots: b.parkingSlots,
    hasElevator: b.hasElevator ?? true, notes: b.notes
  }
  showDialog.value = true
}

async function save() {
  if (!form.value.name.trim()) { toast.add({ severity: 'warn', summary: 'Nhập tên toà nhà', life: 2500 }); return }
  saving.value = true
  try {
    const payload = {
      name: form.value.name.trim(), address: form.value.address, description: form.value.description, floors: form.value.floors,
      district: form.value.district, ward: form.value.ward, contactPhone: form.value.contactPhone, contactEmail: form.value.contactEmail,
      yearBuilt: form.value.yearBuilt, totalFloorArea: form.value.totalFloorArea, parkingSlots: form.value.parkingSlots,
      hasElevator: form.value.hasElevator, notes: form.value.notes
    }
    if (form.value.id) await buildingApi.update(form.value.id, payload)
    else await buildingApi.create(payload)
    showDialog.value = false; await load()
    toast.add({ severity: 'success', summary: 'Đã lưu', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { saving.value = false }
}
function remove(b: Building) {
  confirm.require({
    message: `Xóa toà nhà "${b.name}"?`, header: 'Xác nhận', icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Hủy', acceptLabel: 'Xóa', acceptClass: 'p-button-danger',
    accept: async () => {
      try { await buildingApi.remove(b.id); await load(); toast.add({ severity: 'success', summary: 'Đã xóa', life: 2000 }) }
      catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
    }
  })
}
onMounted(load)
</script>

<template>
  <div class="head">
    <h1>Toà nhà</h1>
    <Button v-if="auth.isManager" label="Thêm toà nhà" icon="pi pi-plus" size="small" @click="openNew" />
  </div>

  <DataTable :value="items" :loading="loading" stripedRows size="small" class="box" paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
    <Column field="id" header="ID" style="width: 60px" />
    <Column field="name" header="Tên" />
    <Column field="address" header="Địa chỉ" />
    <Column field="district" header="Quận/Huyện" style="width: 130px"><template #body="{ data }">{{ data.district || '—' }}</template></Column>
    <Column field="floors" header="Số tầng" style="width: 100px" />
    <Column header="Thang máy" style="width: 100px"><template #body="{ data }"><i :class="data.hasElevator ? 'pi pi-check' : 'pi pi-minus'" /></template></Column>
    <Column header="Căn hộ" style="width: 90px"><template #body="{ data }">{{ data.apartmentCount ?? '—' }}</template></Column>
    <Column header="Phòng" style="width: 90px"><template #body="{ data }">{{ data.roomCount ?? '—' }}</template></Column>
    <Column v-if="auth.isManager" header="Thao tác" style="width: 120px">
      <template #body="{ data }">
        <Button icon="pi pi-pencil" text size="small" @click="openEdit(data)" />
        <Button icon="pi pi-trash" text size="small" severity="danger" @click="remove(data)" />
      </template>
    </Column>
    <template #empty><div class="empty"><i class="pi pi-building" /><p>Chưa có toà nhà nào.</p></div></template>
  </DataTable>

  <Dialog v-model:visible="showDialog" :header="form.id ? 'Sửa toà nhà' : 'Thêm toà nhà'" modal style="width: 680px">
    <div class="form grid2">
      <div class="span2"><label>Tên toà nhà</label><InputText v-model="form.name" class="w-full" /></div>
      <div class="span2"><label>Địa chỉ</label><InputText v-model="form.address" class="w-full" /></div>
      <div><label>Quận / Huyện</label><InputText v-model="form.district" class="w-full" /></div>
      <div><label>Phường / Xã</label><InputText v-model="form.ward" class="w-full" /></div>
      <div><label>Số điện thoại liên hệ</label><InputText v-model="form.contactPhone" class="w-full" /></div>
      <div><label>Email liên hệ</label><InputText v-model="form.contactEmail" class="w-full" /></div>
      <div><label>Số tầng</label><InputNumber v-model="form.floors" :min="0" class="w-full" inputClass="w-full" /></div>
      <div><label>Năm xây dựng</label><InputNumber v-model="form.yearBuilt" :min="1900" :max="2100" :useGrouping="false" class="w-full" inputClass="w-full" /></div>
      <div><label>Tổng diện tích sàn (m²)</label><InputNumber v-model="form.totalFloorArea" :min="0" class="w-full" inputClass="w-full" /></div>
      <div><label>Số chỗ đỗ xe</label><InputNumber v-model="form.parkingSlots" :min="0" class="w-full" inputClass="w-full" /></div>
      <div class="toggle"><label>Có thang máy</label><ToggleSwitch v-model="form.hasElevator" /></div>
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
.head { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--sp-4); }
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
