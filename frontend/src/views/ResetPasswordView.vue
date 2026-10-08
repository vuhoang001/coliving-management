<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import Password from 'primevue/password'
import Button from 'primevue/button'
import { authApi } from '@/services'
import { extractError } from '@/services/api'

const router = useRouter()
const route = useRoute()
const toast = useToast()

const token = ref('')
const password = ref('')
const confirm = ref('')
const loading = ref(false)

const mismatch = computed(() => confirm.value.length > 0 && password.value !== confirm.value)

onMounted(() => { token.value = (route.query.token as string) || '' })

async function submit() {
  if (!token.value) {
    toast.add({ severity: 'error', summary: 'Thiếu token', detail: 'Liên kết không hợp lệ.', life: 3000 })
    return
  }
  if (mismatch.value) return
  loading.value = true
  try {
    await authApi.resetPassword(token.value, password.value)
    toast.add({ severity: 'success', summary: 'Thành công', detail: 'Mật khẩu đã được đặt lại. Vui lòng đăng nhập.', life: 3000 })
    router.push({ name: 'login' })
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
      <h1>Đặt lại mật khẩu</h1>
      <p class="text-muted">Nhập mật khẩu mới cho tài khoản của bạn.</p>

      <div v-if="!token" class="warn">
        <i class="pi pi-exclamation-triangle" />
        <p>Liên kết không hợp lệ hoặc thiếu token. Hãy yêu cầu lại từ trang
          <router-link to="/forgot-password" class="link">Quên mật khẩu</router-link>.</p>
      </div>

      <form v-else class="form" @submit.prevent="submit">
        <label>Mật khẩu mới</label>
        <Password v-model="password" :feedback="true" toggleMask fluid inputClass="w-full" />
        <label>Nhập lại mật khẩu</label>
        <Password v-model="confirm" :feedback="false" toggleMask fluid inputClass="w-full" />
        <small v-if="mismatch" class="err">Mật khẩu nhập lại không khớp.</small>
        <Button type="submit" label="Đặt lại mật khẩu" :loading="loading" :disabled="mismatch || password.length < 6" class="w-full mt-2" />
      </form>

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
.err { color: var(--red, #dc2626); font-size: 0.8rem; }
.warn { margin-top: var(--sp-4); text-align: center; }
.warn .pi { font-size: 2.2rem; color: #f59e0b; margin-bottom: var(--sp-2); }
</style>
