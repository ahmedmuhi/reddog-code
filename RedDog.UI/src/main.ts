import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import router from './router';
import i18n from './i18n';
import BlackDashboard from './plugins/blackDashboard';

import './assets/sass/black-dashboard.scss';
import './assets/css/nucleo-icons.css';
import './assets/css/transitions.css';

const app = createApp(App);

app.use(createPinia());
app.use(router);
app.use(i18n);
app.use(BlackDashboard);

app.mount('#app');
