<template>
  <div class="wrapper">
    <side-bar :background-color="sideBarColor">
      <template #links>
        <sidebar-link to="/dashboard" :name="$t('sidebar.dashboard')" icon="tim-icons icon-chart-pie-36"/>
        <sidebar-link to="/maps" :name="$t('sidebar.maps')" icon="tim-icons icon-globe-2"/>
      </template>
    </side-bar>
    <div class="main-panel">
      <top-navbar></top-navbar>
      <dashboard-content @click="toggleSidebar" />
      <content-footer></content-footer>
    </div>
  </div>
</template>
<script>
import TopNavbar from "./TopNavbar.vue";
import ContentFooter from "./ContentFooter.vue";
import DashboardContent from "./Content.vue";
import { appConfig } from '@/config/appConfig';
export default {
  components: {
    TopNavbar,
    ContentFooter,
    DashboardContent
  },
  data() {
    return {
      isCorp: appConfig.isCorp,
      sideBarColor: appConfig.isCorp ? 'corp' : 'branch'
    }
  },
  methods: {
    toggleSidebar() {
      if (this.$sidebar.showSidebar) {
        this.$sidebar.displaySidebar(false);
      }
    },
  }
};
</script>
<style lang="scss">
</style>
<style>
</style>
