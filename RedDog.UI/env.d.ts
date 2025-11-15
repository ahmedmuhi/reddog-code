/// <reference types="vite/client" />

import type { DefineComponent } from 'vue';
import type { SidebarGlobals } from './src/components/SidebarPlugin';
import type {
  NotificationGlobals,
  NotificationInput,
} from './src/components/NotificationPlugin';

declare module '*.vue' {
  const component: DefineComponent<Record<string, never>, Record<string, never>, any>;
  export default component;
}

declare module '@/components/index' {
  const components: Record<string, unknown>;
  export default components;
}

declare global {
  const __APP_CONFIG__: {
    IS_CORP?: string | boolean;
    STORE_ID?: string;
    SITE_TYPE?: string;
    SITE_TITLE?: string;
    MAKELINE_BASE_URL?: string;
    ACCOUNTING_BASE_URL?: string;
  };

  interface ImportMetaEnv {
    readonly VITE_IS_CORP?: string;
    readonly VITE_STORE_ID?: string;
    readonly VITE_SITE_TYPE?: string;
    readonly VITE_SITE_TITLE?: string;
    readonly VITE_MAKELINE_BASE_URL?: string;
    readonly VITE_ACCOUNTING_BASE_URL?: string;
    readonly BASE_URL: string;
    readonly DEV: boolean;
    readonly PROD: boolean;
    readonly MODE: string;
  }

  interface ImportMeta {
    readonly env: ImportMetaEnv;
  }
}

declare module '@vue/runtime-core' {
  interface ComponentCustomProperties {
    $rtl: {
      isRTL: boolean;
      enableRTL: () => void;
      disableRTL: () => void;
    };
    $sidebar: SidebarGlobals;
    $notify: (notification: NotificationInput | NotificationInput[]) => void;
    $notifications: NotificationGlobals;
  }
}
