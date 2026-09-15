<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
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
import DatePicker from 'primevue/datepicker'
import Tag from 'primevue/tag'
import { invoiceApi, userApi, roomApi, paymentApi } from '@/services'
import type { Invoice, User, Room, InvoiceItem } from '@/types'
import { extractError } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { formatCurrency, formatDay, formatDate, invoiceStatusLabel, invoiceStatusSeverity, paymentMethodLabel, invoiceItemTypeLabel } from '@/composables/format'

const toast = useToast()
const confirm = useConfirm()
const auth = useAuthStore()

const items = ref<Invoice[]>([])
const loading = ref(true)
const totalRecords = ref(0)
const page = ref(1)
const pageSize = ref(10)
const fStatus = ref<string | null>(null)
const statusOptions = Object.entries(invoiceStatusLabel).map(([value, label]) => ({ value, label }))

const tenants = ref<User[]>([])
const rooms = ref<Room[]>([])

// ----- Tạo hoá đơn -----
const showCreate = ref(false)
const savingCreate = ref(false)
interface CreateForm {
  tenantId?: number; roomId?: number; contractId?: number
  periodStart?: Date | null; periodEnd?: Date | null; dueDate?: Date | null; note?: string
  items: InvoiceItem[]
  discount: number; tax: number
  previousElectricityReading?: number; currentElectricityReading?: number
  previousWaterReading?: number; currentWaterReading?: number
}
const cform = ref<CreateForm>(blankCreate())
function blankCreate(): CreateForm {
  return {
    tenantId: undefined, roomId: undefined, periodStart: null, periodEnd: null, dueDate: null, note: '',
    items: [{ type: 'Rent', description: 'Tiền thuê phòng', quantity: 1, unitPrice: 0 }],
    discount: 0, tax: 0,
    previousElectricityReading: undefined, currentElectricityReading: undefined,
    previousWaterReading: undefined, currentWaterReading: undefined
  }
}
const itemTypes = ['Rent', 'Electricity', 'Water', 'Service', 'Other'].map(v => ({ value: v, label: invoiceItemTypeLabel[v] ?? v }))
function addItem() { cform.value.items.push({ type: 'Other', description: '', quantity: 1, unitPrice: 0 }) }
function removeItem(i: number) { cform.value.items.splice(i, 1) }
const createSubtotal = computed(() => cform.value.items.reduce((s, it) => s + (it.quantity || 0) * (it.unitPrice || 0), 0))
// Tổng = Tạm tính - Giảm giá + Thuế (không âm)
const createTotal = computed(() => Math.max(0, createSubtotal.value - (cform.value.discount || 0) + (cform.value.tax || 0)))

// ----- Chi tiết / thanh toán / chia sẻ -----
const showDetail = ref(false)
const detail = ref<Invoice | null>(null)
const payAmount = ref(0)
const payMethod = ref('Cash')
const paying = ref(false)
const payMethodOptions = [
  { value: 'Cash', label: paymentMethodLabel.Cash },
  { value: 'BankTransfer', label: paymentMethodLabel.BankTransfer }
]
const outstanding = computed(() => detail.value ? Math.max(0, detail.value.total - detail.value.paidAmount) : 0)

// ----- Chia hoá đơn -----
const showSplit = ref(false)
const splitParts = ref<{ tenantId?: number; amount?: number }[]>([])
const savingSplit = ref(false)

async function load() {
  loading.value = true
  try {
    const res = await invoiceApi.list({ page: page.value, pageSize: pageSize.value, status: fStatus.value ?? undefined })
    items.value = res.items
    totalRecords.value = res.totalItems
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { loading.value = false }
}
async function loadRefs() {
  try {
    tenants.value = (await userApi.list({ page: 1, pageSize: 200, role: 'Tenant' })).items
    rooms.value = (await roomApi.list({ page: 1, pageSize: 200 })).items
  } catch { /* */ }
}
function onPage(e: { page: number; rows: number }) { page.value = e.page + 1; pageSize.value = e.rows; load() }
function applyFilter() { page.value = 1; load() }

function openCreate() { cform.value = blankCreate(); showCreate.value = true }
async function saveCreate() {
  if (!cform.value.tenantId) { toast.add({ severity: 'warn', summary: 'Chọn khách thuê', life: 2500 }); return }
  if (!cform.value.items.length) { toast.add({ severity: 'warn', summary: 'Thêm ít nhất 1 mục', life: 2500 }); return }
  savingCreate.value = true
  try {
    const payload = {
      tenantId: cform.value.tenantId,
      roomId: cform.value.roomId,
      periodStart: cform.value.periodStart?.toISOString(),
      periodEnd: cform.value.periodEnd?.toISOString(),
      dueDate: cform.value.dueDate?.toISOString(),
      note: cform.value.note,
      discount: cform.value.discount,
      tax: cform.value.tax,
      previousElectricityReading: cform.value.previousElectricityReading,
      currentElectricityReading: cform.value.currentElectricityReading,
      previousWaterReading: cform.value.previousWaterReading,
      currentWaterReading: cform.value.currentWaterReading,
      items: cform.value.items.map((it) => ({ type: it.type, description: it.description, quantity: it.quantity, unitPrice: it.unitPrice, unit: it.unit, note: it.note }))
    }
    await invoiceApi.create(payload)
    showCreate.value = false; await load()
    toast.add({ severity: 'success', summary: 'Đã tạo hoá đơn', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { savingCreate.value = false }
}

async function openDetail(inv: Invoice) {
  try { detail.value = await invoiceApi.byId(inv.id) } catch { detail.value = inv }
  payAmount.value = detail.value ? Math.max(0, detail.value.total - detail.value.paidAmount) : 0
  payMethod.value = 'Cash'
  showDetail.value = true
}
async function refreshDetail() {
  if (!detail.value) return
  try { detail.value = await invoiceApi.byId(detail.value.id) } catch { /* */ }
  await load()
}

async function issue(inv: Invoice) {
  try { await invoiceApi.issue(inv.id); await load(); toast.add({ severity: 'success', summary: 'Đã phát hành', life: 2000 }) }
  catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
}

async function doPay() {
  if (!detail.value) return
  paying.value = true
  try {
    const res = await paymentApi.pay({ invoiceId: detail.value.id, amount: payAmount.value, method: payMethod.value })
    toast.add({ severity: res.success ? 'success' : 'warn', summary: 'Thanh toán', detail: res.message, life: 3000 })
    await refreshDetail()
    payAmount.value = outstanding.value
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { paying.value = false }
}
async function payVnpay() {
  if (!detail.value) return
  try {
    const res = await paymentApi.vnpay(detail.value.id)
    if (res.isMock) {
      toast.add({ severity: 'info', summary: 'VNPay (giả lập)', detail: 'Chuyển tới trang kết quả...', life: 2500 })
    }
    if (res.paymentUrl.startsWith('/')) window.location.assign(res.paymentUrl)
    else window.location.href = res.paymentUrl
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
}

function openSplit(inv: Invoice) {
  detail.value = inv
  splitParts.value = [{ tenantId: inv.tenantId, amount: undefined }]
  showSplit.value = true
}
function addPart() { splitParts.value.push({ tenantId: undefined, amount: undefined }) }
function removePart(i: number) { splitParts.value.splice(i, 1) }
async function saveSplit() {
  if (!detail.value) return
  const parts = splitParts.value.filter((p) => p.tenantId).map((p) => ({ tenantId: p.tenantId!, amount: p.amount }))
  if (!parts.length) { toast.add({ severity: 'warn', summary: 'Chọn ít nhất 1 người', life: 2500 }); return }
  savingSplit.value = true
  try {
    await invoiceApi.split(detail.value.id, parts)
    showSplit.value = false; await load()
    toast.add({ severity: 'success', summary: 'Đã chia hoá đơn', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { savingSplit.value = false }
}

function remove(inv: Invoice) {
  confirm.require({
    message: `Xóa hoá đơn #${inv.id}?`, header: 'Xác nhận', icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Hủy', acceptLabel: 'Xóa', acceptClass: 'p-button-danger',
    accept: async () => {
      try { await invoiceApi.remove(inv.id); await load(); toast.add({ severity: 'success', summary: 'Đã xóa', life: 2000 }) }
      catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
    }
  })
}

onMounted(async () => { await loadRefs(); await load() })
</script>

<template>
  <div class="head">
    <h1>Hoá đơn</h1>
    <Button v-if="auth.isStaff" label="Tạo hoá đơn" icon="pi pi-plus" size="small" @click="openCreate" />
  </div>

  <div class="filters">
    <Select v-model="fStatus" :options="statusOptions" optionLabel="label" optionValue="value" placeholder="Trạng thái" showClear class="flt" @change="applyFilter" />
    <Button label="Lọc" icon="pi pi-search" size="small" @click="applyFilter" />
  </div>

  <DataTable :value="items" :loading="loading" stripedRows size="small" class="box" lazy paginator
    :rows="pageSize" :totalRecords="totalRecords" :rowsPerPageOptions="[10, 20, 50]" @page="onPage">
    <Column header="Mã" style="width: 90px"><template #body="{ data }">{{ data.code || ('#' + data.id) }}</template></Column>
    <Column field="tenantName" header="Khách thuê" />
    <Column field="roomName" header="Phòng" />
    <Column header="Kỳ" ><template #body="{ data }">{{ formatDay(data.periodStart) }} - {{ formatDay(data.periodEnd) }}</template></Column>
    <Column header="Tổng" style="width: 130px"><template #body="{ data }">{{ formatCurrency(data.total) }}</template></Column>
    <Column header="Đã thu" style="width: 130px"><template #body="{ data }">{{ formatCurrency(data.paidAmount) }}</template></Column>
    <Column header="Trạng thái" style="width: 150px">
      <template #body="{ data }"><Tag :value="invoiceStatusLabel[data.status]" :severity="invoiceStatusSeverity[data.status]" /></template>
    </Column>
    <Column header="Thao tác" style="width: 220px">
      <template #body="{ data }">
        <Button icon="pi pi-eye" text size="small" v-tooltip.top="'Chi tiết / thanh toán'" @click="openDetail(data)" />
        <Button v-if="auth.isStaff && data.status === 'Draft'" icon="pi pi-send" text size="small" v-tooltip.top="'Phát hành'" @click="issue(data)" />
        <Button v-if="auth.isStaff" icon="pi pi-users" text size="small" v-tooltip.top="'Chia hoá đơn'" @click="openSplit(data)" />
        <Button v-if="auth.isManager" icon="pi pi-trash" text size="small" severity="danger" @click="remove(data)" />
      </template>
    </Column>
    <template #empty><div class="empty"><i class="pi pi-receipt" /><p>Chưa có hoá đơn nào.</p></div></template>
  </DataTable>

  <!-- Dialog tạo hoá đơn -->
  <Dialog v-model:visible="showCreate" header="Tạo hoá đơn" modal style="width: 760px">
    <div class="form grid2">
      <div><label>Khách thuê</label><Select v-model="cform.tenantId" :options="tenants" optionLabel="fullName" optionValue="id" placeholder="Chọn khách thuê" class="w-full" filter /></div>
      <div><label>Phòng</label><Select v-model="cform.roomId" :options="rooms" optionLabel="name" optionValue="id" placeholder="Chọn phòng" class="w-full" filter showClear /></div>
      <div><label>Từ ngày (kỳ)</label><DatePicker v-model="cform.periodStart" dateFormat="dd/mm/yy" class="w-full" showIcon /></div>
      <div><label>Đến ngày (kỳ)</label><DatePicker v-model="cform.periodEnd" dateFormat="dd/mm/yy" class="w-full" showIcon /></div>
      <div><label>Hạn thanh toán</label><DatePicker v-model="cform.dueDate" dateFormat="dd/mm/yy" class="w-full" showIcon /></div>
      <div class="span2"><label>Ghi chú</label><InputText v-model="cform.note" class="w-full" /></div>
    </div>

    <h4 class="items-title">Chi tiết hoá đơn</h4>
    <table class="items-table">
      <thead><tr><th>Loại</th><th>Mô tả</th><th style="width:90px">SL</th><th style="width:90px">Đơn vị</th><th style="width:150px">Đơn giá</th><th style="width:130px">Thành tiền</th><th></th></tr></thead>
      <tbody>
        <tr v-for="(it, i) in cform.items" :key="i">
          <td><Select v-model="it.type" :options="itemTypes" optionLabel="label" optionValue="value" class="w-full" /></td>
          <td><InputText v-model="it.description" class="w-full" /></td>
          <td><InputNumber v-model="it.quantity" :min="0" class="w-full" inputClass="w-full" /></td>
          <td><InputText v-model="it.unit" class="w-full" placeholder="kWh, m³..." /></td>
          <td><InputNumber v-model="it.unitPrice" :min="0" class="w-full" inputClass="w-full" /></td>
          <td class="amt">{{ formatCurrency((it.quantity || 0) * (it.unitPrice || 0)) }}</td>
          <td><Button icon="pi pi-times" text size="small" severity="danger" @click="removeItem(i)" /></td>
        </tr>
      </tbody>
    </table>
    <div class="items-foot">
      <Button label="Thêm mục" icon="pi pi-plus" text size="small" @click="addItem" />
    </div>

    <h4 class="items-title">Chỉ số công tơ (tuỳ chọn)</h4>
    <div class="form grid2">
      <div><label>Điện — chỉ số cũ</label><InputNumber v-model="cform.previousElectricityReading" :min="0" :useGrouping="false" class="w-full" inputClass="w-full" /></div>
      <div><label>Điện — chỉ số mới</label><InputNumber v-model="cform.currentElectricityReading" :min="0" :useGrouping="false" class="w-full" inputClass="w-full" /></div>
      <div><label>Nước — chỉ số cũ</label><InputNumber v-model="cform.previousWaterReading" :min="0" :useGrouping="false" class="w-full" inputClass="w-full" /></div>
      <div><label>Nước — chỉ số mới</label><InputNumber v-model="cform.currentWaterReading" :min="0" :useGrouping="false" class="w-full" inputClass="w-full" /></div>
    </div>

    <h4 class="items-title">Giảm giá / Thuế</h4>
    <div class="form grid2">
      <div><label>Giảm giá (VNĐ)</label><InputNumber v-model="cform.discount" :min="0" class="w-full" inputClass="w-full" /></div>
      <div><label>Thuế (VNĐ)</label><InputNumber v-model="cform.tax" :min="0" class="w-full" inputClass="w-full" /></div>
    </div>

    <div class="create-totals">
      <div><span>Tạm tính</span><strong>{{ formatCurrency(createSubtotal) }}</strong></div>
      <div><span>Giảm giá</span><strong>- {{ formatCurrency(cform.discount || 0) }}</strong></div>
      <div><span>Thuế</span><strong>+ {{ formatCurrency(cform.tax || 0) }}</strong></div>
      <div class="grand"><span>Tổng cộng</span><strong>{{ formatCurrency(createTotal) }}</strong></div>
    </div>

    <template #footer>
      <Button label="Hủy" text @click="showCreate = false" />
      <Button label="Lưu hoá đơn" icon="pi pi-check" :loading="savingCreate" @click="saveCreate" />
    </template>
  </Dialog>

  <!-- Dialog chi tiết + thanh toán -->
  <Dialog v-model:visible="showDetail" :header="detail ? ('Hoá đơn ' + (detail.code || ('#' + detail.id))) : 'Hoá đơn'" modal style="width: 720px">
    <div v-if="detail" class="detail">
      <div class="meta">
        <span>Khách thuê: <strong>{{ detail.tenantName }}</strong></span>
        <Tag :value="invoiceStatusLabel[detail.status]" :severity="invoiceStatusSeverity[detail.status]" />
      </div>

      <table class="items-table">
        <thead><tr><th>Mô tả</th><th style="width:70px">SL</th><th style="width:140px">Đơn giá</th><th style="width:140px">Thành tiền</th></tr></thead>
        <tbody>
          <tr v-for="(it, i) in detail.items" :key="i">
            <td>{{ it.description }}</td><td>{{ it.quantity }}</td>
            <td>{{ formatCurrency(it.unitPrice) }}</td>
            <td>{{ formatCurrency(it.amount ?? it.quantity * it.unitPrice) }}</td>
          </tr>
        </tbody>
      </table>

      <div class="totals">
        <div><span>Tạm tính</span><strong>{{ formatCurrency(detail.subtotal) }}</strong></div>
        <div v-if="detail.discount"><span>Giảm giá</span><strong>- {{ formatCurrency(detail.discount) }}</strong></div>
        <div v-if="detail.tax"><span>Thuế</span><strong>+ {{ formatCurrency(detail.tax) }}</strong></div>
        <div><span>Tổng cộng</span><strong>{{ formatCurrency(detail.total) }}</strong></div>
        <div><span>Đã thanh toán</span><strong class="ok">{{ formatCurrency(detail.paidAmount) }}</strong></div>
        <div><span>Còn lại</span><strong class="danger">{{ formatCurrency(outstanding) }}</strong></div>
      </div>

      <div v-if="detail.currentElectricityReading != null || detail.currentWaterReading != null" class="block">
        <h4>Chỉ số công tơ</h4>
        <div class="totals">
          <div><span>Điện (cũ → mới)</span><strong>{{ detail.previousElectricityReading ?? '—' }} → {{ detail.currentElectricityReading ?? '—' }}</strong></div>
          <div><span>Nước (cũ → mới)</span><strong>{{ detail.previousWaterReading ?? '—' }} → {{ detail.currentWaterReading ?? '—' }}</strong></div>
        </div>
      </div>

      <div v-if="detail.shares?.length" class="block">
        <h4>Chia sẻ giữa các thành viên</h4>
        <table class="items-table">
          <thead><tr><th>Thành viên</th><th style="width:150px">Số tiền</th><th style="width:120px">Trạng thái</th></tr></thead>
          <tbody>
            <tr v-for="(s, i) in detail.shares" :key="i">
              <td>{{ s.tenantName }}</td><td>{{ formatCurrency(s.shareAmount) }}</td>
              <td><Tag :value="s.isPaid ? 'Đã trả' : 'Chưa trả'" :severity="s.isPaid ? 'success' : 'warn'" /></td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-if="detail.payments?.length" class="block">
        <h4>Lịch sử thanh toán</h4>
        <ul class="pay-list">
          <li v-for="p in detail.payments" :key="p.id">
            {{ formatCurrency(p.amount) }} · {{ paymentMethodLabel[p.method] || p.method }} · {{ formatDate(p.paidAt) }}
          </li>
        </ul>
      </div>

      <div v-if="detail.status !== 'Paid' && detail.status !== 'Cancelled' && detail.status !== 'Draft'" class="block pay-box">
        <h4>Thanh toán</h4>
        <div class="pay-row">
          <InputNumber v-model="payAmount" :min="0" :max="outstanding" class="w-full" inputClass="w-full" />
          <Select v-model="payMethod" :options="payMethodOptions" optionLabel="label" optionValue="value" class="pm" />
          <Button label="Thanh toán" icon="pi pi-check" :loading="paying" @click="doPay" />
          <Button label="VNPay" icon="pi pi-qrcode" severity="help" outlined @click="payVnpay" />
        </div>
      </div>
      <p v-else-if="detail.status === 'Draft'" class="hint">Hoá đơn nháp — cần phát hành trước khi thanh toán.</p>
    </div>
    <template #footer><Button label="Đóng" text @click="showDetail = false" /></template>
  </Dialog>

  <!-- Dialog chia hoá đơn -->
  <Dialog v-model:visible="showSplit" header="Chia hoá đơn cho các thành viên" modal style="width: 560px">
    <p class="hint">Chọn thành viên và (tuỳ chọn) số tiền mỗi người. Bỏ trống số tiền để chia đều.</p>
    <div v-for="(p, i) in splitParts" :key="i" class="split-row">
      <Select v-model="p.tenantId" :options="tenants" optionLabel="fullName" optionValue="id" placeholder="Chọn thành viên" class="w-full" filter />
      <InputNumber v-model="p.amount" :min="0" placeholder="Số tiền (tuỳ chọn)" class="w-full" inputClass="w-full" />
      <Button icon="pi pi-times" text size="small" severity="danger" @click="removePart(i)" />
    </div>
    <Button label="Thêm thành viên" icon="pi pi-plus" text size="small" @click="addPart" />
    <template #footer>
      <Button label="Hủy" text @click="showSplit = false" />
      <Button label="Chia hoá đơn" icon="pi pi-check" :loading="savingSplit" @click="saveSplit" />
    </template>
  </Dialog>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--sp-3); }
.filters { display: flex; gap: var(--sp-2); align-items: center; margin-bottom: var(--sp-3); }
.flt { min-width: 180px; }
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
.empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); padding: var(--sp-8); color: var(--text-muted); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.form label { font-weight: 600; font-size: 0.85rem; display: block; margin-bottom: 4px; }
.grid2 { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sp-3); }
.span2 { grid-column: 1 / -1; }
.items-title { margin: var(--sp-4) 0 var(--sp-2); }
.items-table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
.items-table th, .items-table td { border-bottom: 1px solid var(--border); padding: 6px 8px; text-align: left; vertical-align: middle; }
.items-table th { color: var(--text-muted); font-weight: 600; }
.amt { text-align: right; font-weight: 600; }
.items-foot { display: flex; justify-content: space-between; align-items: center; margin-top: var(--sp-2); }
.create-totals { display: flex; flex-direction: column; gap: 4px; margin-top: var(--sp-3); max-width: 340px; margin-left: auto; }
.create-totals div { display: flex; justify-content: space-between; border-bottom: 1px dashed var(--border); padding: 4px 0; }
.create-totals .grand { border-bottom: none; border-top: 2px solid var(--border); padding-top: 6px; font-size: 1.05rem; }
@media (max-width: 640px) { .grid2 { grid-template-columns: 1fr; } }
.detail .meta { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--sp-3); }
.totals { display: grid; grid-template-columns: repeat(2, 1fr); gap: var(--sp-1) var(--sp-4); margin: var(--sp-3) 0; }
.totals div { display: flex; justify-content: space-between; border-bottom: 1px dashed var(--border); padding: 4px 0; }
.totals .ok { color: var(--success); }
.totals .danger { color: var(--danger); }
.block { margin-top: var(--sp-4); }
.block h4 { margin-bottom: var(--sp-2); }
.pay-list { margin: 0; padding-left: var(--sp-4); color: var(--text-2); font-size: 0.88rem; }
.pay-box { background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius); padding: var(--sp-3); }
.pay-row { display: flex; gap: var(--sp-2); align-items: center; flex-wrap: wrap; }
.pm { min-width: 160px; }
.split-row { display: flex; gap: var(--sp-2); align-items: center; margin-bottom: var(--sp-2); }
.hint { color: var(--text-muted); font-size: 0.85rem; }
</style>
