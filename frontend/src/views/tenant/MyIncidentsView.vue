<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import FileUpload from 'primevue/fileupload'
import { incidentApi, uploadApi } from '@/services'
import type { Incident } from '@/types'
import { extractError } from '@/services/api'
import { formatDate, incidentPriorityLabel, incidentPrioritySeverity, incidentStatusLabel, incidentStatusSeverity, incidentCategoryLabel } from '@/composables/format'

const toast = useToast()

const items = ref<Incident[]>([])
const loading = ref(true)

const showDialog = ref(false)
const saving = ref(false)
const uploading = ref(false)

const priorityOptions = Object.entries(incidentPriorityLabel).map(([value, label]) => ({ value, label }))
const categoryOptions = Object.entries(incidentCategoryLabel).map(([value, label]) => ({ value, label }))

interface Form { title: string; description: string; priority: string; photoUrl: string; category: string; locationDetail: string; contactPhone: string }
const form = ref<Form>(blank())
function blank(): Form { return { title: '', description: '', priority: 'Medium', photoUrl: '', category: 'Other', locationDetail: '', contactPhone: '' } }

async function load() {
  loading.value = true
  try {
    items.value = await incidentApi.mine()
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    loading.value = false
  }
}

function openNew() {
  form.value = blank()
  showDialog.value = true
}

async function onUpload(e: { files: File | File[] }) {
  const file = Array.isArray(e.files) ? e.files[0] : e.files
  if (!file) return
  uploading.value = true
  try {
    const { url } = await uploadApi.image(file, 'incidents')
    form.value.photoUrl = url
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Lỗi tải ảnh', detail: extractError(err), life: 3000 })
  } finally {
    uploading.value = false
  }
}

async function save() {
  if (!form.value.title.trim()) {
    toast.add({ severity: 'warn', summary: 'Nhập tiêu đề sự cố', life: 2500 })
    return
  }
  saving.value = true
  try {
    await incidentApi.create({
      title: form.value.title.trim(),
      description: form.value.description || undefined,
      priority: form.value.priority,
      photoUrl: form.value.photoUrl || undefined,
      category: form.value.category,
      locationDetail: form.value.locationDetail || undefined,
      contactPhone: form.value.contactPhone || undefined
    })
    showDialog.value = false
    await load()
    toast.add({ severity: 'success', summary: 'Đã báo sự cố', life: 2500 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="head">
    <h1>Sự cố của tôi</h1>
    <Button label="Báo sự cố" icon="pi pi-plus" size="small" @click="openNew" />
  </div>

  <DataTable :value="items" :loading="loading" stripedRows size="small" class="box" paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
    <Column field="id" header="ID" style="width: 60px" />
    <Column field="title" header="Tiêu đề" />
    <Column header="Mức độ" style="width: 120px">
      <template #body="{ data }"><Tag :value="incidentPriorityLabel[data.priority]" :severity="incidentPrioritySeverity[data.priority]" /></template>
    </Column>
    <Column header="Trạng thái" style="width: 140px">
      <template #body="{ data }"><Tag :value="incidentStatusLabel[data.status]" :severity="incidentStatusSeverity[data.status]" /></template>
    </Column>
    <Column header="Ngày tạo" style="width: 150px"><template #body="{ data }">{{ formatDate(data.createdAt) }}</template></Column>
    <template #empty><div class="empty"><i class="pi pi-exclamation-circle" /><p>Bạn chưa báo sự cố nào.</p></div></template>
  </DataTable>

  <Dialog v-model:visible="showDialog" header="Báo sự cố" modal style="width: 560px">
    <div class="form">
      <label>Tiêu đề</label>
      <InputText v-model="form.title" class="w-full" />
      <label>Nhóm sự cố</label>
      <Select v-model="form.category" :options="categoryOptions" optionLabel="label" optionValue="value" class="w-full" />
      <label>Mô tả</label>
      <Textarea v-model="form.description" rows="3" class="w-full" autoResize />
      <label>Mức độ</label>
      <Select v-model="form.priority" :options="priorityOptions" optionLabel="label" optionValue="value" class="w-full" />
      <label>Vị trí cụ thể</label>
      <InputText v-model="form.locationDetail" class="w-full" placeholder="Ví dụ: nhà vệ sinh tầng 2..." />
      <label>Số điện thoại liên hệ</label>
      <InputText v-model="form.contactPhone" class="w-full" />
      <label>Ảnh minh họa</label>
      <div class="img-row">
        <img :src="form.photoUrl || 'https://placehold.co/64?text=Anh'" class="preview" alt="" />
        <FileUpload mode="basic" customUpload auto accept="image/*" :maxFileSize="5000000" chooseLabel="Tải ảnh" chooseIcon="pi pi-upload" :disabled="uploading" @uploader="onUpload" />
      </div>
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
.img-row { display: flex; gap: var(--sp-3); align-items: center; margin-top: 4px; }
.preview { width: 64px; height: 64px; object-fit: cover; border-radius: var(--radius-sm); border: 1px solid var(--border); }
</style>
