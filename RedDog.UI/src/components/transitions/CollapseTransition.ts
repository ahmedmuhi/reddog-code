import { Transition, h } from 'vue';

const collapseHandlers = {
  onBeforeEnter(el: HTMLElement) {
    el.style.height = '0';
    el.style.opacity = '0';
  },
  onEnter(el: HTMLElement) {
    el.style.height = el.scrollHeight + 'px';
    el.style.opacity = '1';
  },
  onAfterEnter(el: HTMLElement) {
    el.style.height = '';
    el.style.opacity = '';
  },
  onBeforeLeave(el: HTMLElement) {
    el.style.height = el.scrollHeight + 'px';
    el.style.opacity = '1';
  },
  onLeave(el: HTMLElement) {
    void el.offsetHeight;
    el.style.height = '0';
    el.style.opacity = '0';
  },
  onAfterLeave(el: HTMLElement) {
    el.style.height = '';
    el.style.opacity = '';
  }
};

export default {
  name: 'CollapseTransition',
  setup(_, { slots }) {
    return () =>
      h(
        Transition,
        {
          name: 'collapse-transition',
          ...collapseHandlers
        },
        slots
      );
  }
};
