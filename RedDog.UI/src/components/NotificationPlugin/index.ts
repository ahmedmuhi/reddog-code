import type { App } from 'vue';
import { reactive } from 'vue';
import Notifications from './Notifications.vue';

export type NotificationInput =
  | string
  | {
      message?: string;
      verticalAlign?: string;
      horizontalAlign?: string;
      type?: string;
      timeout?: number;
      closeOnClick?: boolean;
      showClose?: boolean;
    };

type NotificationInstance = Required<{
  message: string;
  verticalAlign: string;
  horizontalAlign: string;
  type: string;
  timeout: number;
  closeOnClick: boolean;
  showClose: boolean;
}> & {
  timestamp: Date;
};

const notificationStore = reactive({
  notifications: [] as NotificationInstance[],
  settings: {
    overlap: false,
    verticalAlign: 'top',
    horizontalAlign: 'right',
    type: 'info',
    timeout: 5000,
    closeOnClick: true,
    showClose: true
  }
});

function setOptions(options: Partial<typeof notificationStore.settings>) {
  Object.assign(notificationStore.settings, options);
}

function removeNotification(timestamp: Date) {
  const indexToDelete = notificationStore.notifications.findIndex(
    n => n.timestamp.getTime() === timestamp.getTime()
  );
  if (indexToDelete !== -1) {
    notificationStore.notifications.splice(indexToDelete, 1);
  }
}

function addNotification(notification: NotificationInput) {
  const base = typeof notification === 'string' ? { message: notification } : notification;
  const timestamp = new Date();
  timestamp.setMilliseconds(timestamp.getMilliseconds() + notificationStore.notifications.length);

  const instance: NotificationInstance = {
    message: base.message || '',
    verticalAlign: base.verticalAlign || notificationStore.settings.verticalAlign,
    horizontalAlign: base.horizontalAlign || notificationStore.settings.horizontalAlign,
    type: base.type || notificationStore.settings.type,
    timeout: base.timeout || notificationStore.settings.timeout,
    closeOnClick: base.closeOnClick ?? notificationStore.settings.closeOnClick,
    showClose: base.showClose ?? notificationStore.settings.showClose,
    timestamp
  };

  notificationStore.notifications.push(instance);
}

function notify(notification: NotificationInput | NotificationInput[]) {
  if (Array.isArray(notification)) {
    notification.forEach(addNotification);
  } else {
    addNotification(notification);
  }
}

const NotificationsPlugin = {
  install(app: App, options?: Partial<typeof notificationStore.settings>) {
    if (options) {
      setOptions(options);
    }

    app.config.globalProperties.$notify = notify;
    app.config.globalProperties.$notifications = {
      notifications: notificationStore.notifications,
      settings: notificationStore.settings,
      removeNotification
    };

    app.component('AppNotifications', Notifications);
  }
};

export type NotificationGlobals = {
  notifications: NotificationInstance[];
  settings: typeof notificationStore.settings;
  removeNotification: (timestamp: Date) => void;
};

export default NotificationsPlugin;
