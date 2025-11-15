import type { DirectiveBinding } from 'vue';

type ClickOutsideEl = HTMLElement & {
  clickOutsideEvent?: (event: MouseEvent) => void;
};

const clickOutside = {
  beforeMount(el: ClickOutsideEl, binding: DirectiveBinding) {
    el.clickOutsideEvent = (event: MouseEvent) => {
      if (!(el === event.target || el.contains(event.target as Node))) {
        if (typeof binding.value === 'function') {
          binding.value(event);
        }
      }
    };
    document.body.addEventListener('click', el.clickOutsideEvent);
  },
  unmounted(el: ClickOutsideEl) {
    if (el.clickOutsideEvent) {
      document.body.removeEventListener('click', el.clickOutsideEvent);
    }
  }
};

export default clickOutside;
