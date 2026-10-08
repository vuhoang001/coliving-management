<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import { useAuthStore } from '@/stores/auth'
import { extractError } from '@/services/api'

const router = useRouter()
const route = useRoute()
const toast = useToast()
const auth = useAuthStore()

const email = ref('manager@coliving.local')
const password = ref('Manager@123')
const loading = ref(false)

const hints = [
  { email: 'admin@coliving.local', pass: 'Admin@123', role: 'Quản trị viên' },
  { email: 'manager@coliving.local', pass: 'Manager@123', role: 'Quản lý' },
  { email: 'staff@coliving.local', pass: 'Staff@123', role: 'Nhân viên' },
  { email: 'an@coliving.local', pass: 'Tenant@123', role: 'Khách thuê' }
]
function useHint(h: { email: string; pass: string }) { email.value = h.email; password.value = h.pass }

async function submit() {
  loading.value = true
  try {
    await auth.login(email.value, password.value)
    toast.add({ severity: 'success', summary: 'Đăng nhập thành công', life: 2000 })
    const redirect = (route.query.redirect as string) || (auth.isStaff ? '/' : '/home')
    router.push(redirect)
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
      <h1>Đăng nhập</h1>
      <p class="text-muted">Hệ thống quản lý căn hộ chia sẻ</p>
      <form class="form" @submit.prevent="submit">
        <label>Email</label>
        <InputText v-model="email" type="email" required class="w-full" />
        <label>Mật khẩu</label>
        <Password v-model="password" :feedback="false" toggleMask fluid inputClass="w-full" />
        <Button type="submit" label="Đăng nhập" :loading="loading" class="w-full mt-2" />
      </form>

      <p class="text-center mt-2"><router-link to="/forgot-password" class="link">Quên mật khẩu?</router-link></p>

      <div class="hints">
        <p class="hints-title">Tài khoản mẫu (bấm để điền)</p>
        <button v-for="h in hints" :key="h.email" class="hint" @click="useHint(h)">
          <strong>{{ h.role }}</strong>
          <span>{{ h.email }} · {{ h.pass }}</span>
        </button>
      </div>

      <p class="text-center mt-2">Chưa có tài khoản? <router-link to="/register" class="link">Đăng ký</router-link></p>
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
.hints { margin-top: var(--sp-5); display: flex; flex-direction: column; gap: var(--sp-1); }
.hints-title { font-size: 0.8rem; color: var(--text-muted); margin: 0 0 var(--sp-1); }
.hint { display: flex; flex-direction: column; text-align: left; background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius-sm); padding: 6px 10px; cursor: pointer; font-family: inherit; transition: all var(--ease); }
.hint:hover { border-color: var(--brand); background: var(--brand-50); }
.hint strong { font-size: 0.82rem; }
.hint span { font-size: 0.75rem; color: var(--text-muted); }
</style>
