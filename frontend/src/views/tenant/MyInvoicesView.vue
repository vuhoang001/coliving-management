<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useRouter } from 'vue-router'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputNumber from 'primevue/inputnumber'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import { invoiceApi, paymentApi } from '@/services'
import type { Invoice } from '@/types'
import { extractError } from '@/services/api'
import { formatCurrency, formatDate, formatDay, invoiceStatusLabel, invoiceStatusSeverity, paymentMethodLabel } from '@/composables/format'

const toast = useToast()
const router = useRouter()

const items = ref<Invoice[]>([])
const loading = ref(true)

const showDialog = ref(false)
const selected = ref<Invoice | null>(null)
const paying = ref(false)
const vnpaying = ref(false)

const payAmount = ref<number>(0)
const payMethod = ref<string>('Cash')
const methodOptions = [
  { value: 'Cash', label: paymentMethodLabel['Cash'] },
  { value: 'BankTransfer', label: paymentMethodLabel['BankTransfer'] }
]

const outstanding = computed(() => {
  if (!selected.value) return 0
  return Math.max(0, (selected.value.total ?? 0) - (selected.value.paidAmount ?? 0))
})

const canPay = computed(() => {
  if (!selected.value) return false
  return selected.value.status !== 'Paid' && selected.value.status !== 'Cancelled'
})

async function load() {
  loading.value = true
  try {
    items.value = await invoiceApi.mine()
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    loading.value = false
  }
}

function openDetail(inv: Invoice) {
  selected.value = inv
  payMethod.value = 'Cash'
  payAmount.value = Math.max(0, (inv.total ?? 0) - (inv.paidAmount ?? 0))
  showDialog.value = true
}

async function refreshSelected(id: number) {
  await load()
  try {
    selected.value = await invoiceApi.byId(id)
  } catch {
    selected.value = items.value.find((i) => i.id === id) ?? selected.value
  }
  if (selected.value) payAmount.value = Math.max(0, (selected.value.total ?? 0) - (selected.value.paidAmount ?? 0))
}

async function pay() {
  if (!selected.value) return
  if (!payAmount.value || payAmount.value <= 0) {
    toast.add({ severity: 'warn', summary: 'Nhập số tiền hợp lệ', life: 2500 })
    return
  }
  paying.value = true
  const id = selected.value.id
  try {
    await paymentApi.pay({ invoiceId: id, amount: payAmount.value, method: payMethod.value })
    toast.add({ severity: 'success', summary: 'Thanh toán thành công', life: 2500 })
    await refreshSelected(id)
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    paying.value = false
  }
}

async function payVnpay() {
  if (!selected.value) return
  vnpaying.value = true
  try {
    const res = await paymentApi.vnpay(selected.value.id)
    if (res.isMock) {
      toast.add({ severity: 'info', summary: 'Chuyển tới cổng thanh toán (mô phỏng)', life: 2500 })
      if (res.paymentUrl.startsWith('/')) router.push(res.paymentUrl)
      else window.location.href = res.paymentUrl
    } else {
      window.location.href = res.paymentUrl
    }
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    vnpaying.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="head">
    <h1>Hóa đơn của tôi</h1>
  </div>

  <DataTable :value="items" :loading="loading" stripedRows size="small" class="box" paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
    <Column header="Mã" style="width: 120px"><template #body="{ data }">{{ data.code || ('#' + data.id) }}</template></Column>
    <Column header="Kỳ" style="width: 190px"><template #body="{ data }">{{ formatDay(data.periodStart) }} — {{ formatDay(data.periodEnd) }}</template></Column>
    <Column header="Tổng tiền" style="width: 140px"><template #body="{ data }">{{ formatCurrency(data.total) }}</template></Column>
    <Column header="Đã trả" style="width: 140px"><template #body="{ data }">{{ formatCurrency(data.paidAmount) }}</template></Column>
    <Column header="Trạng thái" style="width: 150px">
      <template #body="{ data }"><Tag :value="invoiceStatusLabel[data.status]" :severity="invoiceStatusSeverity[data.status]" /></template>
    </Column>
    <Column header="Hạn thanh toán" style="width: 130px"><template #body="{ data }">{{ formatDay(data.dueDate) }}</template></Column>
    <Column header="Thao tác" style="width: 170px">
      <template #body="{ data }">
        <Button label="Chi tiết & Thanh toán" icon="pi pi-wallet" text size="small" @click="openDetail(data)" />
      </template>
    </Column>
    <template #empty><div class="empty"><i class="pi pi-receipt" /><p>Bạn chưa có hóa đơn nào.</p></div></template>
  </DataTable>

  <Dialog v-model:visible="showDialog" :header="selected ? `Hóa đơn ${selected.code || ('#' + selected.id)}` : 'Hóa đơn'" modal style="width: 760px">
    <div v-if="selected" class="detail">
      <label class="sec">Chi tiết khoản mục</label>
      <DataTable :value="selected.items" size="small" class="mini">
        <Column field="description" header="Mô tả" />
        <Column field="quantity" header="SL" style="width: 70px" />
        <Column header="Đơn giá" style="width: 130px"><template #body="{ data }">{{ formatCurrency(data.unitPrice) }}</template></Column>
        <Column header="Thành tiền" style="width: 130px"><template #body="{ data }">{{ formatCurrency(data.amount ?? data.quantity * data.unitPrice) }}</template></Column>
        <template #empty><span class="muted">Không có khoản mục.</span></template>
      </DataTable>

      <div class="totals">
        <div class="row"><span class="lbl">Tạm tính</span><span>{{ formatCurrency(selected.subtotal) }}</span></div>
        <div class="row"><span class="lbl">Tổng cộng</span><span class="strong">{{ formatCurrency(selected.total) }}</span></div>
        <div class="row"><span class="lbl">Đã thanh toán</span><span>{{ formatCurrency(selected.paidAmount) }}</span></div>
        <div class="row"><span class="lbl">Còn lại</span><span class="strong danger">{{ formatCurrency(outstanding) }}</span></div>
      </div>

      <template v-if="selected.shares && selected.shares.length">
        <label class="sec">Phân chia chi phí</label>
        <DataTable :value="selected.shares" size="small" class="mini">
          <Column field="tenantName" header="Khách thuê" />
          <Column header="Số tiền" style="width: 140px"><template #body="{ data }">{{ formatCurrency(data.shareAmount) }}</template></Column>
          <Column header="Tình trạng" style="width: 120px">
            <template #body="{ data }"><Tag :value="data.isPaid ? 'Đã trả' : 'Chưa trả'" :severity="data.isPaid ? 'success' : 'warn'" /></template>
          </Column>
        </DataTable>
      </template>

      <template v-if="selected.payments && selected.payments.length">
        <label class="sec">Lịch sử thanh toán</label>
        <ul class="pays">
          <li v-for="p in selected.payments" :key="p.id">
            <span class="strong">{{ formatCurrency(p.amount) }}</span>
            · {{ paymentMethodLabel[p.method] || p.method }}
            · {{ formatDate(p.paidAt) }}
          </li>
        </ul>
      </template>

      <template v-if="canPay">
        <label class="sec">Thanh toán</label>
        <div class="payform">
          <div class="fld"><label>Số tiền</label><InputNumber v-model="payAmount" :min="0" class="w-full" inputClass="w-full" /></div>
          <div class="fld"><label>Phương thức</label><Select v-model="payMethod" :options="methodOptions" optionLabel="label" optionValue="value" class="w-full" /></div>
        </div>
        <div class="payactions">
          <Button label="Thanh toán" icon="pi pi-check" :loading="paying" @click="pay" />
          <Button label="Thanh toán VNPay" icon="pi pi-credit-card" severity="secondary" :loading="vnpaying" @click="payVnpay" />
        </div>
      </template>
    </div>
    <template #footer>
      <Button label="Đóng" text @click="showDialog = false" />
    </template>
  </Dialog>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--sp-4); }
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
.empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); padding: var(--sp-8); color: var(--text-muted); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.detail { display: flex; flex-direction: column; gap: var(--sp-2); }
.sec { font-weight: 600; font-size: 0.9rem; margin-top: var(--sp-3); }
.mini { border: 1px solid var(--border); border-radius: var(--radius-sm); }
.muted { color: var(--text-muted); font-size: 0.85rem; }
.totals { display: flex; flex-direction: column; gap: 4px; margin-top: var(--sp-2); }
.row { display: flex; justify-content: space-between; max-width: 320px; }
.lbl { color: var(--text-muted); font-size: 0.85rem; }
.strong { font-weight: 700; }
.danger { color: #dc2626; }
.pays { margin: 0; padding-left: var(--sp-4); font-size: 0.85rem; display: flex; flex-direction: column; gap: 4px; }
.payform { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sp-3); }
.payform label { font-weight: 600; font-size: 0.82rem; display: block; margin-bottom: 4px; }
.payactions { display: flex; gap: var(--sp-2); margin-top: var(--sp-3); }
</style>
