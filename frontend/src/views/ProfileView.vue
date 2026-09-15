<script setup lang="ts">
import { ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import FileUpload from 'primevue/fileupload'
import { useAuthStore } from '@/stores/auth'
import { authApi, uploadApi } from '@/services'
import { extractError } from '@/services/api'
import { roleLabel } from '@/composables/format'

const toast = useToast()
const auth = useAuthStore()

const fullName = ref(auth.user?.fullName || '')
const phone = ref(auth.user?.phone || '')
const avatarUrl = ref(auth.user?.avatarUrl || '')
const savingProfile = ref(false)

const currentPassword = ref('')
const newPassword = ref('')
const savingPass = ref(false)

async function onUploadAvatar(e: { files: File | File[] }) {
  const file = Array.isArray(e.files) ? e.files[0] : e.files
  if (!file) return
  try {
    const { url } = await uploadApi.image(file, 'avatars')
    avatarUrl.value = url
    toast.add({ severity: 'success', summary: 'Đã tải ảnh', life: 1800 })
  } catch (err) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(err), life: 3000 }) }
}

async function saveProfile() {
  savingProfile.value = true
  try {
    const u = await authApi.updateProfile({ fullName: fullName.value, phone: phone.value || undefined, avatarUrl: avatarUrl.value || undefined })
    auth.updateUser(u)
    toast.add({ severity: 'success', summary: 'Đã lưu hồ sơ', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { savingProfile.value = false }
}

async function changePassword() {
  if (!currentPassword.value || !newPassword.value) { toast.add({ severity: 'warn', summary: 'Nhập đủ mật khẩu', life: 2500 }); return }
  savingPass.value = true
  try {
    await authApi.changePassword({ currentPassword: currentPassword.value, newPassword: newPassword.value })
    currentPassword.value = ''; newPassword.value = ''
    toast.add({ severity: 'success', summary: 'Đã đổi mật khẩu', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { savingPass.value = false }
}
</script>

<template>
  <h1>Hồ sơ cá nhân</h1>
  <div class="cols">
    <div class="surface-card">
      <h3>Thông tin</h3>
      <div class="avatar-row">
        <img :src="avatarUrl || 'https://placehold.co/80?text=?'" class="avatar" alt="" />
        <FileUpload mode="basic" customUpload auto accept="image/*" :maxFileSize="5000000" chooseLabel="Đổi ảnh" chooseIcon="pi pi-upload" @uploader="onUploadAvatar" />
      </div>
      <div class="form">
        <label>Email</label>
        <InputText :modelValue="auth.user?.email" disabled class="w-full" />
        <label>Vai trò</label>
        <InputText :modelValue="roleLabel[auth.user?.role || ''] || auth.user?.role" disabled class="w-full" />
        <label>Họ và tên</label>
        <InputText v-model="fullName" class="w-full" />
        <label>Số điện thoại</label>
        <InputText v-model="phone" class="w-full" />
        <Button label="Lưu hồ sơ" icon="pi pi-check" :loading="savingProfile" class="mt-3" @click="saveProfile" />
      </div>
    </div>

    <div class="surface-card">
      <h3>Đổi mật khẩu</h3>
      <div class="form">
        <label>Mật khẩu hiện tại</label>
        <Password v-model="currentPassword" :feedback="false" toggleMask fluid inputClass="w-full" />
        <label>Mật khẩu mới</label>
        <Password v-model="newPassword" :feedback="false" toggleMask fluid inputClass="w-full" />
        <Button label="Đổi mật khẩu" icon="pi pi-lock" :loading="savingPass" class="mt-3" @click="changePassword" />
      </div>
    </div>
  </div>
</template>

<style scoped>
h1 { margin-bottom: var(--sp-4); }
.cols { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sp-4); align-items: start; }
.avatar-row { display: flex; align-items: center; gap: var(--sp-3); margin: var(--sp-3) 0; }
.avatar { width: 80px; height: 80px; border-radius: 50%; object-fit: cover; border: 1px solid var(--border); }
.form { display: flex; flex-direction: column; gap: var(--sp-1); }
.form label { font-weight: 600; font-size: 0.85rem; margin-top: var(--sp-2); }
@media (max-width: 800px) { .cols { grid-template-columns: 1fr; } }
</style>
