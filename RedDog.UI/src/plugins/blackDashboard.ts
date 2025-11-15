import type { App } from 'vue';
import SideBar from '@/components/SidebarPlugin';
import Notify from '@/components/NotificationPlugin';
import GlobalComponents from './globalComponents';
import GlobalDirectives from './globalDirectives';
import RTLPlugin from './RTLPlugin';

export default {
  install(app: App) {
    app.use(GlobalComponents);
    app.use(GlobalDirectives);
    app.use(SideBar);
    app.use(Notify);
    app.use(RTLPlugin);
  }
};
