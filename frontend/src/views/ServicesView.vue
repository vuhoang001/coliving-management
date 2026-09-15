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
import Tabs from 'primevue/tabs'
import TabList from 'primevue/tablist'
import Tab from 'primevue/tab'
import TabPanels from 'primevue/tabpanels'
import TabPanel from 'primevue/tabpanel'
import { serviceApi, serviceRequestApi } from '@/services'
import type { ServiceCatalog, ServiceRequest } from '@/types'
import { extractError } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { formatCurrency, formatDate, serviceRequestStatusLabel, serviceRequestStatusSeverity } from '@/composables/format'

const toast = useToast()
const confirm = useConfirm()
const auth = useAuthStore()

const activeTab = ref('catalog')

// ================= Tab 1: Danh mục dịch vụ =================
const services = ref<ServiceCatalog[]>([])
const svcLoading = ref(true)
const showSvcDialog = ref(false)
const svcSaving = ref(false)

interface SvcForm { id?: number; name: string; description?: string; unit?: string; price: number; isActive: boolean }
const svcForm = ref<SvcForm>(svcBlank())
function svcBlank(): SvcForm { return { name: '', description: '', unit: '', price: 0, isActive: true } }

async function loadServices() {
  svcLoading.value = true
  try { services.value = await serviceApi.list(false) }
  catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { svcLoading.value = false }
}
function openNewSvc() { svcForm.value = svcBlank(); showSvcDialog.value = true }
function openEditSvc(s: ServiceCatalog) {
  svcForm.value = { id: s.id, name: s.name, description: s.description, unit: s.unit, price: s.price, isActive: s.isActive }
  showSvcDialog.value = true
}
async function saveSvc() {
  if (!svcForm.value.name.trim()) { toast.add({ severity: 'warn', summary: 'Nhập tên dịch vụ', life: 2500 }); return }
  svcSaving.value = true
  try {
    const payload: Partial<ServiceCatalog> = {
      name: svcForm.value.name.trim(), description: svcForm.value.description,
      unit: svcForm.value.unit, price: svcForm.value.price, isActive: svcForm.value.isActive
    }
    if (svcForm.value.id) await serviceApi.update(svcForm.value.id, payload)
    else await serviceApi.create(payload)
    showSvcDialog.value = false; await loadServices()
    toast.add({ severity: 'success', summary: 'Đã lưu', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { svcSaving.value = false }
}
function removeSvc(s: ServiceCatalog) {
  confirm.require({
    message: `Xóa dịch vụ "${s.name}"?`, header: 'Xác nhận', icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Hủy', acceptLabel: 'Xóa', acceptClass: 'p-button-danger',
    accept: async () => {
      try { await serviceApi.remove(s.id); await loadServices(); toast.add({ severity: 'success', summary: 'Đã xóa', life: 2000 }) }
      catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
    }
  })
}

// ================= Tab 2: Yêu cầu dịch vụ =================
const requests = ref<ServiceRequest[]>([])
const reqLoading = ref(true)
const reqTotal = ref(0)
const reqPage = ref(1)
const reqPageSize = ref(10)
const fReqStatus = ref<string | null>(null)

const statusOptions = Object.entries(serviceRequestStatusLabel).map(([value, label]) => ({ value, label }))

const showStatusDialog = ref(false)
const statusSaving = ref(false)
const targetRequest = ref<ServiceRequest | null>(null)
const newStatus = ref<string | null>(null)

async function loadRequests() {
  reqLoading.value = true
  try {
    const res = await serviceRequestApi.list({ page: reqPage.value, pageSize: reqPageSize.value, status: fReqStatus.value ?? undefined })
    requests.value = res.items
    reqTotal.value = res.totalItems
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { reqLoading.value = false }
}
function onReqPage(e: { page: number; rows: number }) { reqPage.value = e.page + 1; reqPageSize.value = e.rows; loadRequests() }
function applyReqFilter() { reqPage.value = 1; loadRequests() }

function openStatus(r: ServiceRequest) { targetRequest.value = r; newStatus.value = r.status; showStatusDialog.value = true }
async function saveStatus() {
  if (!targetRequest.value || !newStatus.value) { toast.add({ severity: 'warn', summary: 'Chọn trạng thái', life: 2500 }); return }
  statusSaving.value = true
  try {
    await serviceRequestApi.setStatus(targetRequest.value.id, newStatus.value)
    showStatusDialog.value = false; await loadRequests()
    toast.add({ severity: 'success', summary: 'Đã cập nhật', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { statusSaving.value = false }
}

onMounted(async () => { await loadServices(); await loadRequests() })
</script>

<template>
  <div class="head">
    <h1>Dịch vụ</h1>
  </div>

  <Tabs v-model:value="activeTab">
    <TabList>
      <Tab value="catalog">Danh mục dịch vụ</Tab>
      <Tab value="requests">Yêu cầu dịch vụ</Tab>
    </TabList>
    <TabPanels>
      <!-- ============ Tab 1: Danh mục ============ -->
      <TabPanel value="catalog">
        <div class="sub-head">
          <Button v-if="auth.isManager" label="Thêm dịch vụ" icon="pi pi-plus" size="small" @click="openNewSvc" />
        </div>
        <DataTable :value="services" :loading="svcLoading" stripedRows size="small" class="box" paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
          <Column field="id" header="ID" style="width: 60px" />
          <Column field="name" header="Tên" />
          <Column field="description" header="Mô tả" />
          <Column field="unit" header="Đơn vị" style="width: 100px" />
          <Column header="Giá" style="width: 130px"><template #body="{ data }">{{ formatCurrency(data.price) }}</template></Column>
          <Column header="Trạng thái" style="width: 120px">
            <template #body="{ data }"><Tag :value="data.isActive ? 'Hoạt động' : 'Ngừng'" :severity="data.isActive ? 'success' : 'secondary'" /></template>
          </Column>
          <Column v-if="auth.isManager" header="Thao tác" style="width: 120px">
            <template #body="{ data }">
              <Button icon="pi pi-pencil" text size="small" @click="openEditSvc(data)" />
              <Button icon="pi pi-trash" text size="small" severity="danger" @click="removeSvc(data)" />
            </template>
          </Column>
          <template #empty><div class="empty"><i class="pi pi-wrench" /><p>Chưa có dịch vụ nào.</p></div></template>
        </DataTable>
      </TabPanel>

      <!-- ============ Tab 2: Yêu cầu ============ -->
      <TabPanel value="requests">
        <div class="filters">
          <Select v-model="fReqStatus" :options="statusOptions" optionLabel="label" optionValue="value" placeholder="Trạng thái" showClear class="flt" @change="applyReqFilter" />
          <Button label="Lọc" icon="pi pi-search" size="small" @click="applyReqFilter" />
        </div>
        <DataTable :value="requests" :loading="reqLoading" stripedRows size="small" class="box" lazy paginator
          :rows="reqPageSize" :totalRecords="reqTotal" :rowsPerPageOptions="[10, 20, 50]" @page="onReqPage">
          <Column field="id" header="ID" style="width: 60px" />
          <Column field="serviceName" header="Dịch vụ" />
          <Column field="roomName" header="Phòng" />
          <Column field="tenantName" header="Khách thuê" />
          <Column field="quantity" header="SL" style="width: 80px" />
          <Column header="Lịch hẹn" style="width: 160px"><template #body="{ data }">{{ formatDate(data.scheduledAt) }}</template></Column>
          <Column header="Trạng thái" style="width: 140px">
            <template #body="{ data }"><Tag :value="serviceRequestStatusLabel[data.status]" :severity="serviceRequestStatusSeverity[data.status]" /></template>
          </Column>
          <Column field="assignedToName" header="Phụ trách" />
          <Column v-if="auth.isStaff" header="Thao tác" style="width: 130px">
            <template #body="{ data }">
              <Button label="Cập nhật" icon="pi pi-sync" text size="small" @click="openStatus(data)" />
            </template>
          </Column>
          <template #empty><div class="empty"><i class="pi pi-inbox" /><p>Chưa có yêu cầu nào.</p></div></template>
        </DataTable>
      </TabPanel>
    </TabPanels>
  </Tabs>

  <!-- Dialog dịch vụ -->
  <Dialog v-model:visible="showSvcDialog" :header="svcForm.id ? 'Sửa dịch vụ' : 'Thêm dịch vụ'" modal style="width: 520px">
    <div class="form">
      <label>Tên dịch vụ</label>
      <InputText v-model="svcForm.name" class="w-full" />
      <label>Mô tả</label>
      <Textarea v-model="svcForm.description" rows="2" class="w-full" autoResize />
      <label>Đơn vị</label>
      <InputText v-model="svcForm.unit" class="w-full" />
      <label>Giá (VNĐ)</label>
      <InputNumber v-model="svcForm.price" :min="0" class="w-full" inputClass="w-full" />
      <div class="toggle-row">
        <ToggleSwitch v-model="svcForm.isActive" inputId="svc-active" />
        <label for="svc-active" class="inline-label">Hoạt động</label>
      </div>
    </div>
    <template #footer>
      <Button label="Hủy" text @click="showSvcDialog = false" />
      <Button label="Lưu" icon="pi pi-check" :loading="svcSaving" @click="saveSvc" />
    </template>
  </Dialog>

  <!-- Dialog cập nhật trạng thái yêu cầu -->
  <Dialog v-model:visible="showStatusDialog" header="Cập nhật trạng thái" modal style="width: 420px">
    <div class="form">
      <label>Trạng thái mới</label>
      <Select v-model="newStatus" :options="statusOptions" optionLabel="label" optionValue="value" class="w-full" />
    </div>
    <template #footer>
      <Button label="Hủy" text @click="showStatusDialog = false" />
      <Button label="Lưu" icon="pi pi-check" :loading="statusSaving" @click="saveStatus" />
    </template>
  </Dialog>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--sp-4); }
.sub-head { display: flex; justify-content: flex-end; margin-bottom: var(--sp-3); }
.filters { display: flex; gap: var(--sp-2); align-items: center; margin-bottom: var(--sp-3); flex-wrap: wrap; }
.flt { min-width: 170px; }
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
.empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); padding: var(--sp-8); color: var(--text-muted); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.form { display: flex; flex-direction: column; gap: var(--sp-1); }
.form label { font-weight: 600; font-size: 0.85rem; margin-top: var(--sp-2); }
.toggle-row { display: flex; align-items: center; gap: var(--sp-2); margin-top: var(--sp-3); }
.inline-label { margin-top: 0 !important; }
</style>
