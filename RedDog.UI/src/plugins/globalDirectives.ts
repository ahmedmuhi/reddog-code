import { App } from 'vue';
import clickOutside from '../directives/click-outside';

const GlobalDirectives = {
  install(app: App) {
    app.directive('click-outside', clickOutside);
  }
};

export default GlobalDirectives;
