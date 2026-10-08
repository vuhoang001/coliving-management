<script setup lang="ts">
import { ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import { authApi } from '@/services'
import { extractError } from '@/services/api'

const toast = useToast()
const email = ref('')
const loading = ref(false)
const sent = ref(false)

async function submit() {
  loading.value = true
  try {
    await authApi.forgotPassword(email.value.trim())
    sent.value = true
    toast.add({ severity: 'success', summary: 'Đã gửi', detail: 'Kiểm tra email để đặt lại mật khẩu.', life: 3000 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="auth-wrap">
    <div class="auth-card">
      <div class="logo"><i class="pi pi-building" /> Co<b>living</b></div>
      <h1>Quên mật khẩu</h1>
      <p class="text-muted">Nhập email đã đăng ký, chúng tôi sẽ gửi liên kết đặt lại mật khẩu.</p>

      <template v-if="!sent">
        <form class="form" @submit.prevent="submit">
          <label>Email</label>
          <InputText v-model="email" type="email" required class="w-full" />
          <Button type="submit" label="Gửi liên kết đặt lại" :loading="loading" class="w-full mt-2" />
        </form>
      </template>
      <div v-else class="done">
        <i class="pi pi-check-circle" />
        <p>Nếu email tồn tại trong hệ thống, liên kết đặt lại mật khẩu đã được gửi (có hiệu lực trong 1 giờ).</p>
        <p class="text-muted">Chế độ demo: liên kết được ghi vào log của API (chưa cấu hình SMTP thật).</p>
      </div>

      <p class="text-center mt-2"><router-link to="/login" class="link">← Về trang đăng nhập</router-link></p>
    </div>
  </div>
</template>

<style scoped>
.auth-wrap { min-height: 100vh; display: flex; align-items: center; justify-content: center; background: var(--bg); padding: var(--sp-4); }
.auth-card { background: var(--surface); padding: var(--sp-8); border-top: 3px solid var(--brand); border-radius: var(--radius-lg); box-shadow: var(--shadow-md); width: 100%; max-width: 440px; }
.logo { display: flex; align-items: center; gap: 8px; font-size: 1.3rem; font-weight: 700; color: var(--text); margin-bottom: var(--sp-4); }
.logo .pi { color: var(--brand); }
h1 { margin: 0 0 var(--sp-1); color: var(--brand); }
.form { display: flex; flex-direction: column; gap: var(--sp-1); margin-top: var(--sp-4); }
.form label { font-weight: 600; font-size: 0.9rem; margin-top: var(--sp-2); color: var(--text-2); }
.link { color: var(--brand); font-weight: 600; }
.text-center { text-align: center; color: var(--text-2); }
.text-muted { color: var(--text-muted); font-size: 0.85rem; }
.done { margin-top: var(--sp-4); text-align: center; }
.done .pi { font-size: 2.4rem; color: var(--green, #16a34a); margin-bottom: var(--sp-2); }
</style>
