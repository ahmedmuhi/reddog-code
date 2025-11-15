<template>
  <div>
    <notifications></notifications>
    <router-view :key="$route.fullPath"></router-view>
  </div>
</template>

<script>
export default {
  name: 'App',
  mounted() {
    this.$watch('$route', this.disableRTL, { immediate: true });
    this.$watch('$sidebar.showSidebar', this.toggleNavOpen);
  },
  methods: {
    disableRTL() {
      if (!this.$rtl.isRTL) {
        this.$rtl.disableRTL();
      }
    },
    toggleNavOpen() {
      const root = document.documentElement;
      root.classList.toggle('nav-open');
    }
  }
};
</script>

<style lang="scss"></style>
