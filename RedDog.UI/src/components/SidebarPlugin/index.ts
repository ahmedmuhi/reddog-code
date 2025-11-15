import type { App } from 'vue';
import { reactive } from 'vue';
import Sidebar from './SideBar.vue';
import SidebarLink from './SidebarLink.vue';

type SidebarState = {
  showSidebar: boolean;
  sidebarLinks: unknown[];
};

const sidebarState = reactive<SidebarState>({
  showSidebar: false,
  sidebarLinks: []
});

function displaySidebar(value: boolean) {
  sidebarState.showSidebar = value;
}

const SidebarPlugin = {
  install(app: App) {
    app.config.globalProperties.$sidebar = {
      get showSidebar() {
        return sidebarState.showSidebar;
      },
      sidebarLinks: sidebarState.sidebarLinks,
      displaySidebar
    };

    app.component('SideBar', Sidebar);
    app.component('SidebarLink', SidebarLink);
  }
};

export type SidebarGlobals = {
  showSidebar: boolean;
  sidebarLinks: unknown[];
  displaySidebar: (value: boolean) => void;
};

export default SidebarPlugin;
