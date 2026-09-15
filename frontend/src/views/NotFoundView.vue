<script setup lang="ts">
import Button from 'primevue/button'
import { useRouter } from 'vue-router'
const router = useRouter()
</script>

<template>
  <div class="nf">
    <h1>404</h1>
    <p>Không tìm thấy trang bạn yêu cầu.</p>
    <Button label="Về trang chủ" icon="pi pi-home" @click="router.push('/')" />
  </div>
</template>

<style scoped>
.nf { min-height: 100vh; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: var(--sp-3); background: var(--bg); }
.nf h1 { font-size: 5rem; color: var(--brand); }
</style>
