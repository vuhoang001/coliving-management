<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import DatePicker from 'primevue/datepicker'
import { userApi } from '@/services'
import type { User } from '@/types'
import { extractError } from '@/services/api'
import { roleLabel, genderLabel } from '@/composables/format'

type Sev = 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast'

const toast = useToast()
const confirm = useConfirm()

const items = ref<User[]>([])
const loading = ref(true)
const totalRecords = ref(0)
const page = ref(1)
const pageSize = ref(10)

const fRole = ref<string | null>(null)
const fKeyword = ref('')

const roleOptions = Object.entries(roleLabel).map(([value, label]) => ({ value, label }))
const roleSeverity: Record<string, Sev> = {
  Tenant: 'secondary', Staff: 'info', Manager: 'warn', Admin: 'danger'
}

const showDialog = ref(false)
const saving = ref(false)

interface Form {
  id?: number; fullName: string; email: string; phone?: string; password?: string; role: string
  identityNumber?: string; dateOfBirth?: Date | null; gender?: string; permanentAddress?: string
  occupation?: string; nationality?: string; emergencyContactName?: string; emergencyContactPhone?: string
  idIssueDate?: Date | null; idIssuePlace?: string
}
const form = ref<Form>(blank())
function blank(): Form {
  return {
    fullName: '', email: '', phone: '', password: '', role: 'Tenant',
    identityNumber: '', dateOfBirth: null, gender: undefined, permanentAddress: '',
    occupation: '', nationality: 'Việt Nam', emergencyContactName: '', emergencyContactPhone: '',
    idIssueDate: null, idIssuePlace: ''
  }
}
const genderOptions = Object.entries(genderLabel).map(([value, label]) => ({ value, label }))

async function load() {
  loading.value = true
  try {
    const res = await userApi.list({
      page: page.value, pageSize: pageSize.value,
      role: fRole.value ?? undefined, keyword: fKeyword.value || undefined
    })
    items.value = res.items
    totalRecords.value = res.totalItems
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { loading.value = false }
}
function onPage(e: { page: number; rows: number }) { page.value = e.page + 1; pageSize.value = e.rows; load() }
function applyFilter() { page.value = 1; load() }
function clearKeyword() { fKeyword.value = ''; applyFilter() }

function openNew() { form.value = blank(); showDialog.value = true }
function openEdit(u: User) {
  form.value = {
    id: u.id, fullName: u.fullName, email: u.email, phone: u.phone, role: u.role,
    identityNumber: u.identityNumber, dateOfBirth: u.dateOfBirth ? new Date(u.dateOfBirth) : null,
    gender: u.gender, permanentAddress: u.permanentAddress, occupation: u.occupation,
    nationality: u.nationality ?? 'Việt Nam', emergencyContactName: u.emergencyContactName,
    emergencyContactPhone: u.emergencyContactPhone, idIssueDate: u.idIssueDate ? new Date(u.idIssueDate) : null,
    idIssuePlace: u.idIssuePlace
  }
  showDialog.value = true
}

async function save() {
  if (!form.value.fullName.trim()) { toast.add({ severity: 'warn', summary: 'Nhập họ tên', life: 2500 }); return }
  if (!form.value.id) {
    if (!form.value.email.trim()) { toast.add({ severity: 'warn', summary: 'Nhập email', life: 2500 }); return }
    if (!form.value.password) { toast.add({ severity: 'warn', summary: 'Nhập mật khẩu', life: 2500 }); return }
  }
  saving.value = true
  try {
    const profile = {
      identityNumber: form.value.identityNumber || undefined,
      dateOfBirth: form.value.dateOfBirth ? form.value.dateOfBirth.toISOString() : undefined,
      gender: form.value.gender || undefined,
      permanentAddress: form.value.permanentAddress || undefined,
      occupation: form.value.occupation || undefined,
      nationality: form.value.nationality || undefined,
      emergencyContactName: form.value.emergencyContactName || undefined,
      emergencyContactPhone: form.value.emergencyContactPhone || undefined,
      idIssueDate: form.value.idIssueDate ? form.value.idIssueDate.toISOString() : undefined,
      idIssuePlace: form.value.idIssuePlace || undefined
    }
    if (form.value.id) {
      await userApi.update(form.value.id, {
        fullName: form.value.fullName.trim(), phone: form.value.phone, role: form.value.role, ...profile
      })
    } else {
      await userApi.create({
        fullName: form.value.fullName.trim(), email: form.value.email.trim(),
        phone: form.value.phone, password: form.value.password, role: form.value.role, ...profile
      })
    }
    showDialog.value = false; await load()
    toast.add({ severity: 'success', summary: 'Đã lưu', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { saving.value = false }
}

function toggleActive(u: User) {
  confirm.require({
    message: u.isActive ? `Khoá tài khoản "${u.fullName}"?` : `Mở khoá tài khoản "${u.fullName}"?`,
    header: 'Xác nhận', icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Hủy', acceptLabel: u.isActive ? 'Khoá' : 'Mở khoá',
    acceptClass: u.isActive ? 'p-button-danger' : '',
    accept: async () => {
      try {
        await userApi.setActive(u.id, !u.isActive); await load()
        toast.add({ severity: 'success', summary: 'Đã cập nhật', life: 2000 })
      } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
    }
  })
}
onMounted(load)
</script>

<template>
  <div class="head">
    <h1>Người dùng</h1>
    <Button label="Thêm người dùng" icon="pi pi-plus" size="small" @click="openNew" />
  </div>

  <div class="filters">
    <Select v-model="fRole" :options="roleOptions" optionLabel="label" optionValue="value" placeholder="Vai trò" showClear class="flt" @change="applyFilter" />
    <span class="p-input-icon-left search">
      <InputText v-model="fKeyword" placeholder="Tìm tên/email/SĐT" @keyup.enter="applyFilter" />
    </span>
    <Button label="Lọc" icon="pi pi-search" size="small" @click="applyFilter" />
    <Button label="Xoá lọc" icon="pi pi-times" size="small" text @click="clearKeyword" />
  </div>

  <DataTable :value="items" :loading="loading" stripedRows size="small" class="box" lazy paginator
    :rows="pageSize" :totalRecords="totalRecords" :rowsPerPageOptions="[10, 20, 50]" @page="onPage">
    <Column field="id" header="ID" style="width: 60px" />
    <Column field="fullName" header="Họ tên" />
    <Column field="email" header="Email" />
    <Column field="phone" header="SĐT" style="width: 130px" />
    <Column header="Vai trò" style="width: 130px">
      <template #body="{ data }"><Tag :value="roleLabel[data.role]" :severity="roleSeverity[data.role]" /></template>
    </Column>
    <Column header="Trạng thái" style="width: 120px">
      <template #body="{ data }">
        <Tag :value="data.isActive ? 'Hoạt động' : 'Khoá'" :severity="data.isActive ? 'success' : 'danger'" />
      </template>
    </Column>
    <Column header="Xác thực email" style="width: 120px">
      <template #body="{ data }">
        <i :class="data.emailConfirmed ? 'pi pi-check-circle ok' : 'pi pi-times-circle no'" />
      </template>
    </Column>
    <Column header="Thao tác" style="width: 130px">
      <template #body="{ data }">
        <Button icon="pi pi-pencil" text size="small" v-tooltip.top="'Sửa'" @click="openEdit(data)" />
        <Button :icon="data.isActive ? 'pi pi-lock' : 'pi pi-lock-open'" text size="small"
          :severity="data.isActive ? 'danger' : 'success'"
          v-tooltip.top="data.isActive ? 'Khoá' : 'Mở khoá'" @click="toggleActive(data)" />
      </template>
    </Column>
    <template #empty><div class="empty"><i class="pi pi-users" /><p>Chưa có người dùng nào.</p></div></template>
  </DataTable>

  <Dialog v-model:visible="showDialog" :header="form.id ? 'Sửa người dùng' : 'Thêm người dùng'" modal style="width: 720px">
    <div class="form grid2">
      <div><label>Họ tên</label><InputText v-model="form.fullName" class="w-full" /></div>
      <div><label>Email</label><InputText v-model="form.email" class="w-full" :disabled="!!form.id" /></div>
      <div><label>Số điện thoại</label><InputText v-model="form.phone" class="w-full" /></div>
      <div v-if="!form.id"><label>Mật khẩu</label><Password v-model="form.password" :feedback="false" toggleMask class="w-full" inputClass="w-full" /></div>
      <div><label>Vai trò</label><Select v-model="form.role" :options="roleOptions" optionLabel="label" optionValue="value" class="w-full" /></div>
      <div><label>CMND / CCCD</label><InputText v-model="form.identityNumber" class="w-full" /></div>
      <div><label>Ngày cấp</label><DatePicker v-model="form.idIssueDate" dateFormat="dd/mm/yy" showButtonBar class="w-full" /></div>
      <div><label>Nơi cấp</label><InputText v-model="form.idIssuePlace" class="w-full" /></div>
      <div><label>Ngày sinh</label><DatePicker v-model="form.dateOfBirth" dateFormat="dd/mm/yy" showButtonBar class="w-full" /></div>
      <div><label>Giới tính</label><Select v-model="form.gender" :options="genderOptions" optionLabel="label" optionValue="value" placeholder="Chọn" showClear class="w-full" /></div>
      <div><label>Quốc tịch</label><InputText v-model="form.nationality" class="w-full" /></div>
      <div><label>Nghề nghiệp</label><InputText v-model="form.occupation" class="w-full" /></div>
      <div><label>Người liên hệ khẩn cấp</label><InputText v-model="form.emergencyContactName" class="w-full" /></div>
      <div><label>SĐT liên hệ khẩn cấp</label><InputText v-model="form.emergencyContactPhone" class="w-full" /></div>
      <div class="span2"><label>Địa chỉ thường trú</label><InputText v-model="form.permanentAddress" class="w-full" /></div>
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
.search :deep(input) { min-width: 220px; }
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
.ok { color: #16a34a; font-size: 1.1rem; }
.no { color: #dc2626; font-size: 1.1rem; }
.empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); padding: var(--sp-8); color: var(--text-muted); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.form { display: flex; flex-direction: column; gap: var(--sp-1); }
.form label { font-weight: 600; font-size: 0.85rem; margin-top: var(--sp-2); }
.grid2 { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sp-3); align-items: start; }
.grid2 label { margin-top: 0; display: block; margin-bottom: 4px; }
.span2 { grid-column: 1 / -1; }
@media (max-width: 640px) { .grid2 { grid-template-columns: 1fr; } }
</style>
