<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import { useAuthStore } from '@/stores/auth'
import { extractError } from '@/services/api'

const router = useRouter()
const toast = useToast()
const auth = useAuthStore()

const fullName = ref('')
const email = ref('')
const phone = ref('')
const password = ref('')
const loading = ref(false)

async function submit() {
  if (!fullName.value.trim() || !email.value.trim() || !password.value) {
    toast.add({ severity: 'warn', summary: 'Vui lòng nhập đủ thông tin', life: 2500 }); return
  }
  loading.value = true
  try {
    await auth.register({ fullName: fullName.value.trim(), email: email.value.trim(), phone: phone.value || undefined, password: password.value })
    toast.add({ severity: 'success', summary: 'Đăng ký thành công', life: 2000 })
    router.push(auth.isStaff ? '/' : '/home')
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
      <h1>Đăng ký</h1>
      <p class="text-muted">Tạo tài khoản khách thuê</p>
      <form class="form" @submit.prevent="submit">
        <label>Họ và tên</label>
        <InputText v-model="fullName" class="w-full" />
        <label>Email</label>
        <InputText v-model="email" type="email" class="w-full" />
        <label>Số điện thoại</label>
        <InputText v-model="phone" class="w-full" />
        <label>Mật khẩu</label>
        <Password v-model="password" :feedback="false" toggleMask fluid inputClass="w-full" />
        <Button type="submit" label="Đăng ký" :loading="loading" class="w-full mt-2" />
      </form>
      <p class="text-center mt-2">Đã có tài khoản? <router-link to="/login" class="link">Đăng nhập</router-link></p>
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
</style>
