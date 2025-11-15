import { RouteRecordRaw } from 'vue-router';
const DashboardLayout = () => import('@/layout/dashboard/DashboardLayout.vue');
import NotFound from '@/pages/NotFoundPage.vue';

const Dashboard = () => import('@/pages/Dashboard.vue');
const Maps = () => import('@/pages/Maps.vue');
const Profile = () => import('@/pages/Profile.vue');

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    component: DashboardLayout,
    redirect: '/dashboard',
    children: [
      { path: 'dashboard', name: 'dashboard', component: Dashboard },
      { path: 'maps', name: 'maps', component: Maps },
      { path: 'profile', name: 'profile', component: Profile }
    ]
  },
  { path: '/:catchAll(.*)', component: NotFound }
];

export default routes;
