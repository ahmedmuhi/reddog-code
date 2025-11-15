import { App } from 'vue';
import {
  BaseInput,
  Card,
  BaseDropdown,
  BaseButton,
  BaseCheckbox
} from '@/components/index';

const GlobalComponents = {
  install(app: App) {
    app.component(BaseInput.name || 'BaseInput', BaseInput);
    app.component(Card.name || 'Card', Card);
    app.component(BaseDropdown.name || 'BaseDropdown', BaseDropdown);
    app.component(BaseButton.name || 'BaseButton', BaseButton);
    app.component(BaseCheckbox.name || 'BaseCheckbox', BaseCheckbox);
  }
};

export default GlobalComponents;
