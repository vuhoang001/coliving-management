<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Tag from 'primevue/tag'
import { contractApi } from '@/services'
import type { Contract } from '@/types'
import { extractError } from '@/services/api'
import { formatDate, formatDay, contractStatusLabel, contractStatusSeverity } from '@/composables/format'

const toast = useToast()

const items = ref<Contract[]>([])
const loading = ref(true)

const showDialog = ref(false)
const signing = ref(false)
const selected = ref<Contract | null>(null)
const signature = ref('')

async function load() {
  loading.value = true
  try {
    items.value = await contractApi.list()
  } catch {
    // Tenant có thể không có quyền hoặc chưa có hợp đồng — hiển thị rỗng.
    items.value = []
  } finally {
    loading.value = false
  }
}

function canSign(c: Contract): boolean {
  return c.status === 'PendingSignature' || c.status === 'Draft'
}

function openContract(c: Contract) {
  selected.value = c
  signature.value = ''
  showDialog.value = true
}

async function sign() {
  if (!selected.value) return
  if (!signature.value.trim()) {
    toast.add({ severity: 'warn', summary: 'Nhập họ tên để ký', life: 2500 })
    return
  }
  signing.value = true
  try {
    await contractApi.sign(selected.value.id, { signature: signature.value.trim() })
    showDialog.value = false
    await load()
    toast.add({ severity: 'success', summary: 'Đã ký hợp đồng', life: 2500 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    signing.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="head">
    <h1>Hợp đồng của tôi</h1>
  </div>

  <DataTable :value="items" :loading="loading" stripedRows size="small" class="box" paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
    <Column field="id" header="ID" style="width: 60px" />
    <Column field="roomName" header="Phòng" />
    <Column header="Bắt đầu" style="width: 120px"><template #body="{ data }">{{ formatDay(data.startDate) }}</template></Column>
    <Column header="Kết thúc" style="width: 120px"><template #body="{ data }">{{ formatDay(data.endDate) }}</template></Column>
    <Column header="Trạng thái" style="width: 130px">
      <template #body="{ data }"><Tag :value="contractStatusLabel[data.status]" :severity="contractStatusSeverity[data.status]" /></template>
    </Column>
    <Column header="Ngày ký" style="width: 150px"><template #body="{ data }">{{ formatDate(data.signedAt) }}</template></Column>
    <Column header="Thao tác" style="width: 130px">
      <template #body="{ data }">
        <Button label="Xem & Ký" icon="pi pi-file-edit" text size="small" @click="openContract(data)" />
      </template>
    </Column>
    <template #empty><div class="empty"><i class="pi pi-file" /><p>Bạn chưa có hợp đồng nào.</p></div></template>
  </DataTable>

  <Dialog v-model:visible="showDialog" :header="selected ? `Hợp đồng #${selected.id}` : 'Hợp đồng'" modal style="width: 640px">
    <div v-if="selected" class="detail">
      <div class="row"><span class="lbl">Phòng</span><span>{{ selected.roomName || '—' }}</span></div>
      <div class="row"><span class="lbl">Thời hạn</span><span>{{ formatDay(selected.startDate) }} — {{ formatDay(selected.endDate) }}</span></div>
      <div class="row"><span class="lbl">Trạng thái</span><Tag :value="contractStatusLabel[selected.status]" :severity="contractStatusSeverity[selected.status]" /></div>

      <label class="sec">Điều khoản hợp đồng</label>
      <pre class="terms">{{ selected.terms || 'Không có điều khoản.' }}</pre>

      <template v-if="canSign(selected)">
        <label class="sec">Ký hợp đồng (nhập họ tên đầy đủ)</label>
        <InputText v-model="signature" class="w-full" placeholder="Nguyễn Văn A" />
      </template>
      <template v-else>
        <label class="sec">Chữ ký</label>
        <div class="row"><span class="lbl">Đã ký bởi</span><span>{{ selected.signature || '—' }}</span></div>
        <div class="row"><span class="lbl">Ngày ký</span><span>{{ formatDate(selected.signedAt) }}</span></div>
      </template>
    </div>
    <template #footer>
      <Button label="Đóng" text @click="showDialog = false" />
      <Button v-if="selected && canSign(selected)" label="Ký hợp đồng" icon="pi pi-check" :loading="signing" @click="sign" />
    </template>
  </Dialog>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--sp-4); }
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
.empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); padding: var(--sp-8); color: var(--text-muted); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.detail { display: flex; flex-direction: column; gap: var(--sp-2); }
.row { display: flex; gap: var(--sp-2); align-items: center; }
.lbl { min-width: 120px; font-weight: 600; font-size: 0.85rem; color: var(--text-muted); }
.sec { font-weight: 600; font-size: 0.9rem; margin-top: var(--sp-3); }
.terms { white-space: pre-wrap; background: var(--surface-alt, #f9fafb); border: 1px solid var(--border); border-radius: var(--radius-sm); padding: var(--sp-3); font-family: inherit; font-size: 0.85rem; max-height: 260px; overflow: auto; margin: 0; }
</style>
