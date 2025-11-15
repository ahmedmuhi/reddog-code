<template>
    <button
class="navbar-toggler" type="button"
            data-toggle="collapse"
            :data-target="target"
            :aria-controls="target"
            :aria-expanded="toggled"
            aria-label="Toggle navigation"
            @click="$emit('click', $event)">
        <span class="navbar-toggler-icon"></span>
    </button>
</template>
<script>
export default {
  props: {
    target: {
      type: [String, Number],
      default: '',
      description: "Button target element"
    },
    toggled: {
      type: Boolean,
      default: false,
      description: "Whether button is toggled"
    }
  },
  emits: ['click']
};
</script>
<style>
</style>
