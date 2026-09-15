<script setup lang="ts">
import { ref, onMounted, nextTick } from 'vue'
import { Chart, registerables } from 'chart.js'
import { dashboardApi } from '@/services'
import type { DashboardStats } from '@/types'
import { extractError } from '@/services/api'
import { formatCurrency } from '@/composables/format'
const fmtCurrency = formatCurrency
import { useToast } from 'primevue/usetoast'

Chart.register(...registerables)
const toast = useToast()

const stats = ref<DashboardStats | null>(null)
const loading = ref(true)
const revenueCanvas = ref<HTMLCanvasElement>()
const occCanvas = ref<HTMLCanvasElement>()

const cards = ref<{ label: string; value: string; icon: string; color: string }[]>([])

async function load() {
  loading.value = true
  try {
    const [s, rev, occ] = await Promise.all([
      dashboardApi.stats(),
      dashboardApi.revenue(6),
      dashboardApi.occupancy()
    ])
    stats.value = s
    cards.value = [
      { label: 'Tỷ lệ lấp đầy', value: `${Math.round((s.occupancyRate || 0) * 100)}%`, icon: 'pi-percentage', color: '#0071e3' },
      { label: 'Doanh thu tháng này', value: fmtCurrency(s.revenueThisMonth), icon: 'pi-dollar', color: '#1d8a4e' },
      { label: 'Sự cố đang mở', value: String(s.openIncidents), icon: 'pi-exclamation-triangle', color: '#d97a1f' },
      { label: 'Đặt phòng chờ duyệt', value: String(s.pendingBookings), icon: 'pi-calendar-plus', color: '#6a5acd' },
      { label: 'Công nợ chưa thu', value: fmtCurrency(s.outstandingAmount), icon: 'pi-wallet', color: '#e0402f' },
      { label: 'Hợp đồng hiệu lực', value: String(s.activeContracts), icon: 'pi-file-edit', color: '#109a9c' }
    ]
    await nextTick()
    drawRevenue(rev.points.map((p) => p.month), rev.points.map((p) => p.revenue), rev.points.map((p) => p.expected))
    drawOccupancy(occ.buildings.map((b) => b.buildingName), occ.buildings.map((b) => Math.round(b.occupancyRate * 100)))
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    loading.value = false
  }
}

let revChart: Chart | null = null
let occChart: Chart | null = null

function drawRevenue(labels: string[], revenue: number[], expected: number[]) {
  if (!revenueCanvas.value) return
  revChart?.destroy()
  revChart = new Chart(revenueCanvas.value, {
    type: 'line',
    data: {
      labels,
      datasets: [
        { label: 'Doanh thu thực tế', data: revenue, borderColor: '#0071e3', backgroundColor: 'rgba(0,113,227,0.1)', fill: true, tension: 0.35 },
        { label: 'Dự kiến', data: expected, borderColor: '#d97a1f', borderDash: [6, 4], fill: false, tension: 0.35 }
      ]
    },
    options: {
      responsive: true, maintainAspectRatio: false,
      plugins: { legend: { position: 'bottom' } },
      scales: { y: { ticks: { callback: (v) => formatCurrency(Number(v)) } } }
    }
  })
}

function drawOccupancy(labels: string[], rates: number[]) {
  if (!occCanvas.value) return
  occChart?.destroy()
  occChart = new Chart(occCanvas.value, {
    type: 'bar',
    data: { labels, datasets: [{ label: 'Tỷ lệ lấp đầy (%)', data: rates, backgroundColor: '#0071e3', borderRadius: 6 }] },
    options: {
      responsive: true, maintainAspectRatio: false,
      plugins: { legend: { display: false } },
      scales: { y: { beginAtZero: true, max: 100, ticks: { callback: (v) => `${v}%` } } }
    }
  })
}

onMounted(load)
</script>

<template>
  <h1>Bảng điều khiển</h1>

  <div class="cards">
    <div v-for="c in cards" :key="c.label" class="stat surface-card">
      <span class="ic" :style="{ background: c.color + '18', color: c.color }"><i class="pi" :class="c.icon" /></span>
      <div class="meta">
        <span class="lbl">{{ c.label }}</span>
        <strong class="val">{{ c.value }}</strong>
      </div>
    </div>
  </div>

  <div class="charts">
    <div class="surface-card chart-box">
      <h3>Doanh thu 6 tháng (thực tế vs dự kiến)</h3>
      <div class="canvas-wrap"><canvas ref="revenueCanvas" /></div>
    </div>
    <div class="surface-card chart-box">
      <h3>Tỷ lệ lấp đầy theo toà nhà</h3>
      <div class="canvas-wrap"><canvas ref="occCanvas" /></div>
    </div>
  </div>
</template>

<style scoped>
h1 { margin-bottom: var(--sp-4); }
.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: var(--sp-3); margin-bottom: var(--sp-4); }
.stat { display: flex; align-items: center; gap: var(--sp-3); padding: var(--sp-4); }
.ic { width: 48px; height: 48px; border-radius: var(--radius); display: grid; place-items: center; font-size: 1.3rem; flex-shrink: 0; }
.meta { display: flex; flex-direction: column; min-width: 0; }
.lbl { font-size: 0.82rem; color: var(--text-muted); }
.val { font-size: 1.35rem; }
.charts { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sp-4); }
.chart-box h3 { margin-bottom: var(--sp-3); }
.canvas-wrap { position: relative; height: 300px; }
@media (max-width: 900px) { .charts { grid-template-columns: 1fr; } }
</style>
