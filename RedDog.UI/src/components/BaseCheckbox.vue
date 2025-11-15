<template>
  <div
    class="form-check"
    :class="[{ disabled: disabled }, inlineClass]"
  >
    <label :for="cbId" class="form-check-label">
      <input
        :id="cbId"
        v-model="model"
        class="form-check-input"
        type="checkbox"
        :disabled="disabled"
      />
      <span class="form-check-sign"></span>
      <slot>
        <span v-if="inline">&nbsp;</span>
      </slot>
    </label>
  </div>
</template>
<script>
  export default {
    name: "BaseCheckbox",
    model: {
      prop: "checked"
    },
    props: {
      checked: {
        type: [Array, Boolean],
        default: false,
        description: "Whether checkbox is checked"
      },
      disabled: {
        type: Boolean,
        default: false,
        description: "Whether checkbox is disabled"
      },
      inline: {
        type: Boolean,
        default: false,
        description: "Whether checkbox should be inline with other checkboxes"
      }
    },
    emits: ['input', 'update:checked'],
    data() {
      return {
        cbId: '',
        touched: false
      }
    },
    computed: {
      model: {
        get() {
          return this.checked
        },
        set(check) {
          if (!this.touched) {
            this.touched = true
          }
          this.$emit('input', check)
          this.$emit('update:checked', check)
        }
      },
      inlineClass() {
        return this.inline ? 'form-check-inline' : ''
      }
    },
    created() {
      this.cbId = Math.random().toString(16).slice(2)
    }
  }
</script>
