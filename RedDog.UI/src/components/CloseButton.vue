<template>
    <button
type="button" class="navbar-toggler"
            data-toggle="collapse"
            :data-target="`#${target}`"
            :aria-controls="target"
            :aria-expanded="expanded"
            aria-label="Toggle navigation"
            @click="handleClick">
        <span></span>
        <span></span>
    </button>
</template>
<script>
export default {
  name: "CloseButton",
  props: {
    target: {
      type: [String, Number],
      default: '',
      description: "Close button target element"
    },
    expanded: {
      type: Boolean,
      default: false,
      description: "Whether button is expanded (aria-expanded attribute)"
    }
  },
  emits: ['click'],
  methods: {
    handleClick(evt) {
      this.$emit("click", evt);
    }
  }
};
</script>
<style>
</style>
