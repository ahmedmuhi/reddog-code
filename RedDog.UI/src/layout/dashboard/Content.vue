<template>
  <div class="content" @click="$emit('click', $event)">
    <transition name="fade" mode="out-in">

      <router-view></router-view>
    </transition>
  </div>
</template>
<script>
  export default {
    name: 'DashboardContentView',
    emits: ['click']
  };
</script>
