<template>
  <div class="form-check form-check-radio" :class="[inlineClass, {disabled: disabled}]">
    <label :for="cbId" class="form-check-label">
      <input
:id="cbId"
             v-model="model"
             class="form-check-input"
             type="radio"
             :disabled="disabled"
             :value="name" />
      <slot></slot>
      <span class="form-check-sign"></span>
    </label>
  </div>
</template>
<script>
export default {
  name: "BaseRadio",
  model: {
    prop: 'value',
    event: 'input'
  },
  props: {
    name: {
      type: [String, Number],
      default: '',
      description: "Radio label"
    },
    disabled: {
      type: Boolean,
      default: false,
      description: "Whether radio is disabled"
    },
    value: {
      type: [String, Boolean],
      default: '',
      description: "Radio value"
    },
    inline: {
      type: Boolean,
      default: false,
      description: "Whether radio is inline"
    }
  },
  emits: ['input'],
  data() {
    return {
      cbId: ""
    };
  },
  computed: {
    model: {
      get() {
        return this.value;
      },
      set(value) {
        this.$emit("input", value);
      }
    },
    inlineClass() {
      if (this.inline) {
        return `form-check-inline`;
      }
      return "";
    }
  },
  created() {
    this.cbId = Math.random()
      .toString(16)
      .slice(2);
  }
};
</script>
