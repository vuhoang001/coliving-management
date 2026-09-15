import { createApp } from 'vue'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import ColivingPreset from './theme'
import ToastService from 'primevue/toastservice'
import ConfirmationService from 'primevue/confirmationservice'
import Tooltip from 'primevue/tooltip'

import 'primeicons/primeicons.css'
import 'primeflex/primeflex.css'
import '@/assets/main.css'

import App from './App.vue'
import router from './router'

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(PrimeVue, {
  theme: {
    preset: ColivingPreset,
    options: { darkModeSelector: '.app-dark', cssLayer: false }
  },
  locale: {
    accept: 'Đồng ý',
    reject: 'Hủy',
    choose: 'Chọn',
    upload: 'Tải lên',
    cancel: 'Hủy',
    clear: 'Xóa',
    emptyMessage: 'Không có dữ liệu',
    emptySearchMessage: 'Không tìm thấy kết quả'
  }
})
app.use(ToastService)
app.use(ConfirmationService)
app.directive('tooltip', Tooltip)

app.mount('#app')
