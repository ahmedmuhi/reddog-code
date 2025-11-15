import { App, reactive } from 'vue';

const rtlState = reactive({
  isRTL: false
});

function getDocClasses() {
  return document.body.classList;
}

function toggleBootstrapRTL(value: boolean) {
  for (let i = 0; i < document.styleSheets.length; i += 1) {
    const styleSheet = document.styleSheets[i];
    const { href } = styleSheet;
    if (href && href.endsWith('bootstrap-rtl.css')) {
      styleSheet.disabled = !value;
    }
  }
}

function enableRTL() {
  rtlState.isRTL = true;
  getDocClasses().add('rtl');
  getDocClasses().add('menu-on-right');
  toggleBootstrapRTL(true);
}

function disableRTL() {
  rtlState.isRTL = false;
  getDocClasses().remove('rtl');
  getDocClasses().remove('menu-on-right');
  toggleBootstrapRTL(false);
}

const RTLPlugin = {
  install(app: App) {
    app.config.globalProperties.$rtl = {
      get isRTL() {
        return rtlState.isRTL;
      },
      enableRTL,
      disableRTL
    };
  }
};

export default RTLPlugin;
