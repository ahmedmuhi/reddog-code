<template>
  <div
    class="form-group"
    :class="{
      'input-group': hasIcon,
      'input-group-focus': focused
    }"
  >
    <slot name="label">
      <label v-if="label" class="control-label">
        {{label}}
      </label>
    </slot>
    <slot name="addonLeft">
      <span v-if="addonLeftIcon" class="input-group-prepend">
        <div class="input-group-text">
          <i :class="addonLeftIcon"></i>
        </div>
      </span>
    </slot>
    <slot>
      <input
        :value="value"
        v-bind="$attrs"
        class="form-control"
        aria-describedby="addon-right addon-left"
        v-on="listeners"
      />
    </slot>
    <slot name="addonRight">
      <span v-if="addonRightIcon" class="input-group-append">
        <div class="input-group-text">
          <i :class="addonRightIcon"></i>
        </div>
      </span>
    </slot>
    <slot name="helperText"></slot>
  </div>
</template>
<script>
  export default {
    name: "BaseInput",
    inheritAttrs: false,
    props: {
      modelValue: {
        type: [String, Number],
        default: '',
        description: 'Input value'
      },
      label: {
        type: String,
        default: '',
        description: "Input label"
      },
      addonRightIcon: {
        type: String,
        default: '',
        description: "Input icon on the right"
      },
      addonLeftIcon: {
        type: String,
        default: '',
        description: "Input icon on the left"
      },
    },
    emits: ['update:modelValue', 'input', 'focus', 'blur'],
    data() {
      return {
        focused: false
      }
    },
    computed: {
      hasIcon() {
        const { addonRight, addonLeft } = this.$slots;
        return (
          addonRight !== undefined ||
          addonLeft !== undefined ||
          this.addonRightIcon !== undefined ||
          this.addonLeftIcon !== undefined
        );
      },
      listeners() {
        return {
          input: this.onInput,
          blur: this.onBlur,
          focus: this.onFocus
        }
      }
    },
    methods: {
      onInput(evt) {
        this.$emit('update:modelValue', evt.target.value);
        this.$emit('input', evt.target.value)
      },
      onFocus() {
        this.focused = true;
        this.$emit('focus');
      },
      onBlur() {
        this.focused = false;
        this.$emit('blur');
      }
    }
  }
</script>
<style>

</style>
