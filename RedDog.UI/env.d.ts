/// <reference types="vite/client" />

import type { SidebarGlobals } from './src/components/SidebarPlugin';
import type {
  NotificationGlobals,
  NotificationInput
} from './src/components/NotificationPlugin';

declare const __APP_CONFIG__: {
  IS_CORP?: string | boolean;
  STORE_ID?: string;
  SITE_TYPE?: string;
  SITE_TITLE?: string;
  MAKELINE_BASE_URL?: string;
  ACCOUNTING_BASE_URL?: string;
};

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
