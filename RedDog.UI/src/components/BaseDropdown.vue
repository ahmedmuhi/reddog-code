<template>
  <component
    :is="tag"
    v-click-outside="closeDropDown"
    class="dropdown"
    :class="{ show: isOpen }"
    @click="toggleDropDown"
  >
    <slot name="title-container" :is-open="isOpen">
      <component
        :is="titleTag"
        class="dropdown-toggle btn-rotate"
        :class="titleClasses"
        :aria-expanded="isOpen"
        :aria-label="title || ariaLabel"
        data-toggle="dropdown"
      >
        <slot name="title" :is-open="isOpen">
          <i :class="icon"></i>
          {{title}}
        </slot>
      </component>
    </slot>
    <ul class="dropdown-menu" :class="[{ show: isOpen }, { 'dropdown-menu-right': menuOnRight }, menuClasses]">
      <slot></slot>
    </ul>
  </component>
</template>
<script>
  export default {
    name: "BaseDropdown",
    props: {
      tag: {
        type: String,
        default: "div",
        description: "Dropdown html tag (e.g div, ul etc)"
      },
      titleTag: {
        type: String,
        default: "button",
        description: "Dropdown title (toggle) html tag"
      },
      title: {
        type: String,
        default: '',
        description: "Dropdown title",

      },
      icon: {
        type: String,
        default: '',
        description: "Dropdown icon"
      },
      titleClasses: {
        type: [String, Object, Array],
        default: () => [],
        description: "Title css classes"
      },
      menuClasses: {
        type: [String, Object, Array],
        default: () => [],
        description: "Menu css classes"
      },
      menuOnRight: {
        type: Boolean,
        default: false,
        description: "Whether menu should appear on the right"
      },
      ariaLabel: {
        type: String,
        default: ''
      }
    },
    emits: ['change'],
    data() {
      return {
        isOpen: false
      };
    },
    methods: {
      toggleDropDown() {
        this.isOpen = !this.isOpen;
        this.$emit("change", this.isOpen);
      },
      closeDropDown() {
        this.isOpen = false;
        this.$emit('change', false);
      }
    }
  };
</script>
