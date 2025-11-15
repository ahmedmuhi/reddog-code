<template>
  <component
:is="tag"
             class="nav-item"
             v-bind="$attrs"
             @click="hideSidebar">
    <a class="nav-link">
      <slot>
        <i v-if="icon" :class="icon"></i>
        <p>{{name}}</p>
      </slot>
    </a>
  </component>
</template>
<script>
export default {
  name: "SidebarLink",
  inject: {
    autoClose: {
      default: true
    },
    addLink: {
      default: ()=>{}
    },
    removeLink: {
      default: ()=>{}
    }
  },
  inheritAttrs: false,
  props: {
    name: {
      type: String,
      default: ''
    },
    icon: {
      type: String,
      default: ''
    },
    tag: {
      type: String,
      default: "router-link"
    }
  },
  mounted() {
    if (this.addLink) {
      this.addLink(this);
    }
  },
  beforeUnmount() {
    if (this.$el && this.$el.parentNode) {
      this.$el.parentNode.removeChild(this.$el)
    }
    if (this.removeLink) {
      this.removeLink(this);
    }
  },
  methods: {
    hideSidebar() {
      if (this.autoClose) {
        this.$sidebar.displaySidebar(false);
      }
    },
    isActive() {
      return this.$el.classList.contains("active");
    }
  }
};
</script>
<style>
</style>
