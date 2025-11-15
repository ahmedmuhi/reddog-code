import { Transition, defineComponent, h } from 'vue';

const asElement = (el: Element): HTMLElement => el as HTMLElement;

const collapseHandlers = {
  onBeforeEnter(el: Element) {
    const element = asElement(el);
    element.style.height = '0';
    element.style.opacity = '0';
  },
  onEnter(el: Element) {
    const element = asElement(el);
    element.style.height = element.scrollHeight + 'px';
    element.style.opacity = '1';
  },
  onAfterEnter(el: Element) {
    const element = asElement(el);
    element.style.height = '';
    element.style.opacity = '';
  },
  onBeforeLeave(el: Element) {
    const element = asElement(el);
    element.style.height = element.scrollHeight + 'px';
    element.style.opacity = '1';
  },
  onLeave(el: Element) {
    const element = asElement(el);
    void element.offsetHeight;
    element.style.height = '0';
    element.style.opacity = '0';
  },
  onAfterLeave(el: Element) {
    const element = asElement(el);
    element.style.height = '';
    element.style.opacity = '';
  }
};

export default defineComponent({
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
});
