<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import { incidentApi, buildingApi, userApi } from '@/services'
import type { Incident, Building, User } from '@/types'
import { extractError } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import {
  formatDate, formatDay, formatCurrency,
  incidentPriorityLabel, incidentPrioritySeverity,
  incidentStatusLabel, incidentStatusSeverity,
  incidentCategoryLabel, label
} from '@/composables/format'

const toast = useToast()
const auth = useAuthStore()

const items = ref<Incident[]>([])
const buildings = ref<Building[]>([])
const staff = ref<User[]>([])
const loading = ref(true)
const totalRecords = ref(0)
const page = ref(1)
const pageSize = ref(10)

const fStatus = ref<string | null>(null)
const fPriority = ref<string | null>(null)
const fBuilding = ref<number | null>(null)
const fKeyword = ref('')

const statusOptions = Object.entries(incidentStatusLabel).map(([value, label]) => ({ value, label }))
const priorityOptions = Object.entries(incidentPriorityLabel).map(([value, label]) => ({ value, label }))

// Chi tiết
const showDetail = ref(false)
const detail = ref<Incident | null>(null)

// Phân công
const showAssign = ref(false)
const assignTarget = ref<Incident | null>(null)
const assignStaffId = ref<number | null>(null)
const assigning = ref(false)

// Cập nhật trạng thái
const showStatus = ref(false)
const statusTarget = ref<Incident | null>(null)
const statusValue = ref<string | null>(null)
const statusNote = ref('')
const savingStatus = ref(false)

const staffOptions = computed(() =>
  staff.value.filter((u) => ['Staff', 'Manager', 'Admin'].includes(u.role))
)

async function load() {
  loading.value = true
  try {
    const res = await incidentApi.list({
      page: page.value, pageSize: pageSize.value,
      status: fStatus.value ?? undefined,
      priority: fPriority.value ?? undefined,
      buildingId: fBuilding.value ?? undefined,
      keyword: fKeyword.value || undefined
    })
    items.value = res.items
    totalRecords.value = res.totalItems
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { loading.value = false }
}
async function loadRefs() {
  try { buildings.value = await buildingApi.list() } catch { /* */ }
  try { const res = await userApi.list({ page: 1, pageSize: 100 }); staff.value = res.items } catch { /* */ }
}
function onPage(e: { page: number; rows: number }) { page.value = e.page + 1; pageSize.value = e.rows; load() }
function applyFilter() { page.value = 1; load() }
function clearFilter() {
  fStatus.value = null; fPriority.value = null; fBuilding.value = null; fKeyword.value = ''
  applyFilter()
}

function openDetail(r: Incident) { detail.value = r; showDetail.value = true }

function openAssign(r: Incident) {
  assignTarget.value = r
  assignStaffId.value = r.assignedToId ?? null
  showAssign.value = true
}
async function submitAssign() {
  if (!assignTarget.value || !assignStaffId.value) {
    toast.add({ severity: 'warn', summary: 'Chọn nhân viên', life: 2500 }); return
  }
  assigning.value = true
  try {
    await incidentApi.assign(assignTarget.value.id, assignStaffId.value)
    showAssign.value = false; await load()
    toast.add({ severity: 'success', summary: 'Đã phân công', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { assigning.value = false }
}

function openStatus(r: Incident) {
  statusTarget.value = r
  statusValue.value = r.status
  statusNote.value = r.resolutionNote ?? ''
  showStatus.value = true
}
async function submitStatus() {
  if (!statusTarget.value || !statusValue.value) {
    toast.add({ severity: 'warn', summary: 'Chọn trạng thái', life: 2500 }); return
  }
  savingStatus.value = true
  try {
    await incidentApi.setStatus(statusTarget.value.id, {
      status: statusValue.value,
      resolutionNote: statusNote.value || undefined
    })
    showStatus.value = false; await load()
    toast.add({ severity: 'success', summary: 'Đã cập nhật trạng thái', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { savingStatus.value = false }
}

onMounted(async () => { await loadRefs(); await load() })
</script>

<template>
  <div class="head">
    <h1>Sự cố</h1>
  </div>

  <div class="filters">
    <Select v-model="fStatus" :options="statusOptions" optionLabel="label" optionValue="value" placeholder="Trạng thái" showClear class="flt" @change="applyFilter" />
    <Select v-model="fPriority" :options="priorityOptions" optionLabel="label" optionValue="value" placeholder="Mức độ" showClear class="flt" @change="applyFilter" />
    <Select v-model="fBuilding" :options="buildings" optionLabel="name" optionValue="id" placeholder="Toà nhà" showClear class="flt" @change="applyFilter" />
    <span class="p-input-icon-left search">
      <InputText v-model="fKeyword" placeholder="Tìm tiêu đề" @keyup.enter="applyFilter" />
    </span>
    <Button label="Lọc" icon="pi pi-search" size="small" @click="applyFilter" />
    <Button label="Xóa lọc" icon="pi pi-times" size="small" text @click="clearFilter" />
  </div>

  <DataTable :value="items" :loading="loading" stripedRows size="small" class="box" lazy paginator
    :rows="pageSize" :totalRecords="totalRecords" :rowsPerPageOptions="[10, 20, 50]" @page="onPage">
    <Column field="id" header="ID" style="width: 60px" />
    <Column field="title" header="Tiêu đề" />
    <Column header="Nhóm" style="width: 110px"><template #body="{ data }">{{ label(incidentCategoryLabel, data.category) }}</template></Column>
    <Column header="Mức độ" style="width: 120px">
      <template #body="{ data }"><Tag :value="incidentPriorityLabel[data.priority]" :severity="incidentPrioritySeverity[data.priority]" /></template>
    </Column>
    <Column header="Trạng thái" style="width: 130px">
      <template #body="{ data }"><Tag :value="incidentStatusLabel[data.status]" :severity="incidentStatusSeverity[data.status]" /></template>
    </Column>
    <Column field="buildingName" header="Toà nhà" />
    <Column field="roomName" header="Phòng" />
    <Column field="reportedByName" header="Người báo" />
    <Column field="assignedToName" header="Phụ trách" />
    <Column header="Ngày tạo" style="width: 150px">
      <template #body="{ data }">{{ formatDate(data.createdAt) }}</template>
    </Column>
    <Column header="Thao tác" style="width: 180px">
      <template #body="{ data }">
        <Button icon="pi pi-eye" text size="small" v-tooltip.top="'Chi tiết'" @click="openDetail(data)" />
        <Button v-if="auth.isStaff" icon="pi pi-user-plus" text size="small" v-tooltip.top="'Phân công'" @click="openAssign(data)" />
        <Button v-if="auth.isStaff" icon="pi pi-sync" text size="small" v-tooltip.top="'Cập nhật trạng thái'" @click="openStatus(data)" />
      </template>
    </Column>
    <template #empty><div class="empty"><i class="pi pi-exclamation-triangle" /><p>Chưa có sự cố nào.</p></div></template>
  </DataTable>

  <!-- Chi tiết -->
  <Dialog v-model:visible="showDetail" header="Chi tiết sự cố" modal style="width: 600px">
    <div v-if="detail" class="detail">
      <div class="row"><span class="k">Tiêu đề</span><span class="v">{{ detail.title }}</span></div>
      <div class="row">
        <span class="k">Mức độ</span>
        <span class="v"><Tag :value="incidentPriorityLabel[detail.priority]" :severity="incidentPrioritySeverity[detail.priority]" /></span>
      </div>
      <div class="row">
        <span class="k">Trạng thái</span>
        <span class="v"><Tag :value="incidentStatusLabel[detail.status]" :severity="incidentStatusSeverity[detail.status]" /></span>
      </div>
      <div class="row"><span class="k">Nhóm sự cố</span><span class="v">{{ label(incidentCategoryLabel, detail.category) }}</span></div>
      <div class="row"><span class="k">Toà nhà</span><span class="v">{{ detail.buildingName || '—' }}</span></div>
      <div class="row"><span class="k">Phòng</span><span class="v">{{ detail.roomName || '—' }}</span></div>
      <div class="row"><span class="k">Vị trí cụ thể</span><span class="v">{{ detail.locationDetail || '—' }}</span></div>
      <div class="row"><span class="k">SĐT liên hệ</span><span class="v">{{ detail.contactPhone || '—' }}</span></div>
      <div class="row"><span class="k">Dự kiến xử lý xong</span><span class="v">{{ detail.expectedResolutionDate ? formatDay(detail.expectedResolutionDate) : '—' }}</span></div>
      <div class="row"><span class="k">Chi phí</span><span class="v">{{ detail.cost != null ? formatCurrency(detail.cost) : '—' }}</span></div>
      <div class="row"><span class="k">Người báo</span><span class="v">{{ detail.reportedByName || '—' }}</span></div>
      <div class="row"><span class="k">Phụ trách</span><span class="v">{{ detail.assignedToName || '—' }}</span></div>
      <div class="row"><span class="k">Ngày tạo</span><span class="v">{{ formatDate(detail.createdAt) }}</span></div>
      <div class="row block"><span class="k">Mô tả</span><span class="v">{{ detail.description || '—' }}</span></div>
      <div class="row block"><span class="k">Ghi chú xử lý</span><span class="v">{{ detail.resolutionNote || '—' }}</span></div>
      <div v-if="detail.photoUrl" class="row block">
        <span class="k">Hình ảnh</span>
        <img :src="detail.photoUrl" class="detail-img" alt="" />
      </div>
    </div>
    <template #footer>
      <Button label="Đóng" text @click="showDetail = false" />
    </template>
  </Dialog>

  <!-- Phân công -->
  <Dialog v-model:visible="showAssign" header="Phân công xử lý" modal style="width: 460px">
    <div class="form">
      <div><label>Nhân viên phụ trách</label>
        <Select v-model="assignStaffId" :options="staffOptions" optionLabel="fullName" optionValue="id" placeholder="Chọn nhân viên" class="w-full" filter />
      </div>
    </div>
    <template #footer>
      <Button label="Hủy" text @click="showAssign = false" />
      <Button label="Phân công" icon="pi pi-check" :loading="assigning" @click="submitAssign" />
    </template>
  </Dialog>

  <!-- Cập nhật trạng thái -->
  <Dialog v-model:visible="showStatus" header="Cập nhật trạng thái" modal style="width: 480px">
    <div class="form">
      <div><label>Trạng thái</label>
        <Select v-model="statusValue" :options="statusOptions" optionLabel="label" optionValue="value" placeholder="Chọn trạng thái" class="w-full" />
      </div>
      <div><label>Ghi chú xử lý</label>
        <Textarea v-model="statusNote" rows="3" class="w-full" autoResize />
      </div>
    </div>
    <template #footer>
      <Button label="Hủy" text @click="showStatus = false" />
      <Button label="Lưu" icon="pi pi-check" :loading="savingStatus" @click="submitStatus" />
    </template>
  </Dialog>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--sp-3); }
.filters { display: flex; gap: var(--sp-2); align-items: center; margin-bottom: var(--sp-3); flex-wrap: wrap; }
.flt { min-width: 170px; }
.search :deep(input) { min-width: 220px; }
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
.empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); padding: var(--sp-8); color: var(--text-muted); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.form { display: flex; flex-direction: column; gap: var(--sp-3); }
.form label { font-weight: 600; font-size: 0.85rem; display: block; margin-bottom: 4px; }
.detail { display: flex; flex-direction: column; gap: var(--sp-2); }
.detail .row { display: flex; gap: var(--sp-3); }
.detail .row.block { flex-direction: column; gap: 4px; }
.detail .k { font-weight: 600; font-size: 0.85rem; min-width: 120px; color: var(--text-muted); }
.detail .v { flex: 1; }
.detail-img { max-width: 100%; border-radius: var(--radius-sm); border: 1px solid var(--border); margin-top: 4px; }
</style>
