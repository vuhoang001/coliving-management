<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import { useNotificationStore } from '@/stores/notification'
import { formatDate } from '@/composables/format'

const notif = useNotificationStore()
const router = useRouter()

function open(n: { id: number; isRead: boolean; link?: string }) {
  if (!n.isRead) notif.markRead(n.id)
  if (n.link) router.push(n.link)
}
onMounted(() => notif.fetch())
</script>

<template>
  <div class="head">
    <h1>Thông báo</h1>
    <Button v-if="notif.unreadCount" label="Đánh dấu tất cả đã đọc" icon="pi pi-check" size="small" text @click="notif.markAllRead()" />
  </div>

  <div class="list surface-card">
    <button v-for="n in notif.items" :key="n.id" class="item" :class="{ unread: !n.isRead }" @click="open(n)">
      <span class="dot" :class="{ hidden: n.isRead }" />
      <span class="body">
        <strong>{{ n.title }}</strong>
        <span class="msg">{{ n.message }}</span>
        <span class="time">{{ formatDate(n.createdAt) }}</span>
      </span>
    </button>
    <p v-if="!notif.items.length" class="empty">Chưa có thông báo nào.</p>
  </div>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--sp-4); }
.list { padding: var(--sp-2); }
.item { display: flex; gap: 12px; width: 100%; text-align: left; background: none; border: none; border-left: 3px solid transparent; cursor: pointer; padding: 12px; border-radius: var(--radius-sm); font-family: inherit; transition: background var(--ease); }
.item:hover { background: var(--surface-2); }
.item.unread { background: var(--brand-50); border-left-color: var(--brand); }
.dot { width: 9px; height: 9px; border-radius: 50%; margin-top: 6px; flex-shrink: 0; background: var(--brand); }
.dot.hidden { visibility: hidden; }
.body { display: flex; flex-direction: column; gap: 2px; }
.msg { color: var(--text-2); font-size: 0.9rem; }
.time { color: var(--text-muted); font-size: 0.78rem; }
.empty { text-align: center; color: var(--text-muted); padding: var(--sp-8); }
</style>
