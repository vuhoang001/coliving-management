<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'
import Button from 'primevue/button'
import { auditApi } from '@/services'
import type { AuditLog } from '@/types'
import { extractError } from '@/services/api'

type Sev = 'success' | 'info' | 'warn' | 'danger' | 'secondary'

const toast = useToast()
const items = ref<AuditLog[]>([])
const loading = ref(true)
const totalRecords = ref(0)
const page = ref(1)
const pageSize = ref(20)

const actionSeverity: Record<string, Sev> = {
  Create: 'success', Update: 'info', SetActive: 'warn', Delete: 'danger'
}
const actionLabel: Record<string, string> = {
  Create: 'Tạo mới', Update: 'Cập nhật', SetActive: 'Đổi trạng thái', Delete: 'Xoá'
}

function fmt(d: string) { return new Date(d).toLocaleString('vi-VN') }

async function load() {
  loading.value = true
  try {
    const res = await auditApi.list(page.value, pageSize.value)
    items.value = res.items
    totalRecords.value = res.totalItems
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { loading.value = false }
}
function onPage(e: { page: number; rows: number }) { page.value = e.page + 1; pageSize.value = e.rows; load() }
onMounted(load)
</script>

<template>
  <div class="head">
    <h1>Nhật ký thao tác</h1>
    <Button label="Làm mới" icon="pi pi-refresh" size="small" text @click="load" />
  </div>

  <DataTable :value="items" :loading="loading" stripedRows size="small" class="box" lazy paginator
    :rows="pageSize" :totalRecords="totalRecords" :rowsPerPageOptions="[20, 50, 100]" @page="onPage">
    <Column header="Thời gian" style="width: 180px">
      <template #body="{ data }">{{ fmt(data.createdAt) }}</template>
    </Column>
    <Column header="Người thực hiện" style="width: 150px">
      <template #body="{ data }">{{ data.userEmail || (data.userId ? '#' + data.userId : 'Hệ thống') }}</template>
    </Column>
    <Column header="Hành động" style="width: 130px">
      <template #body="{ data }">
        <Tag :value="actionLabel[data.action] || data.action" :severity="actionSeverity[data.action] || 'secondary'" />
      </template>
    </Column>
    <Column header="Đối tượng" style="width: 150px">
      <template #body="{ data }">{{ data.entityType }}{{ data.entityId ? ' #' + data.entityId : '' }}</template>
    </Column>
    <Column field="detail" header="Chi tiết" />
    <template #empty><div class="empty">Chưa có nhật ký nào.</div></template>
  </DataTable>
</template>

<style scoped>
.head { display: flex; align-items: center; justify-content: space-between; margin-bottom: var(--sp-4); }
.head h1 { margin: 0; color: var(--brand); }
.box { background: var(--surface); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
.empty { text-align: center; color: var(--text-muted); padding: var(--sp-5); }
</style>
