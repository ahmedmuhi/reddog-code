<template>
  <transition name="slide-up">
    <div
v-show="show"
         class="modal fade"
         :class="[{'show d-block': show}, {'d-none': !show}, {'modal-mini': type === 'mini'}]"
         tabindex="-1"
         role="dialog"
         :aria-hidden="!show"
         :style="{ '--modal-transition-duration': animationDuration + 'ms' }"
         @click.self="closeModal">

      <div
class="modal-dialog"
           :class="[{'modal-notice': type === 'notice'}, {'modal-dialog-centered': centered}, modalClasses]">
        <div class="modal-content" :class="[gradient ? `bg-gradient-${gradient}` : '',modalContentClasses]">

          <div v-if="$slots.header" class="modal-header" :class="[headerClasses]">
            <slot name="header"></slot>
            <slot name="close-button">
              <button
v-if="showClose"
                      type="button"
                      class="close"
                      data-dismiss="modal"
                      aria-label="Close"
                      @click="closeModal">
                <i class="tim-icons icon-simple-remove"></i>
              </button>
            </slot>
          </div>

          <div v-if="$slots.default" class="modal-body" :class="bodyClasses">
            <slot></slot>
          </div>

          <div v-if="$slots.footer" class="modal-footer" :class="footerClasses">
            <slot name="footer"></slot>
          </div>
        </div>
      </div>

    </div>
  </transition>
</template>
<script>
export default {
  name: "AppModal",
  props: {
    show: {
      type: Boolean,
      default: false
    },
    showClose: {
      type: Boolean,
      default: true
    },
    centered: {
      type: Boolean,
      default: true
    },
    type: {
      type: String,
      default: "",
      validator(value) {
        let acceptedValues = ["", "notice", "mini"];
        return acceptedValues.indexOf(value) !== -1;
      },
      description: 'Modal type (notice|mini|"") '
    },
    modalClasses: {
      type: [Object, String],
      default: () => ({}),
      description: "Modal dialog css classes"
    },
    modalContentClasses: {
      type: [Object, String],
      default: () => ({}),
      description: "Modal dialog content css classes"
    },
    gradient: {
      type: String,
      default: '',
      description: "Modal gradient type (danger, primary etc)"
    },
    headerClasses: {
      type: [Object, String],
      default: () => ({}),
      description: "Modal Header css classes"
    },
    bodyClasses: {
      type: [Object, String],
      default: () => ({}),
      description: "Modal Body css classes"
    },
    footerClasses: {
      type: [Object, String],
      default: () => ({}),
      description: "Modal Footer css classes"
    },
    animationDuration: {
      type: Number,
      default: 500,
      description: "Modal transition duration"
    }
  },
  emits: ['update:show', 'close'],
  watch: {
    show(val) {
      let documentClasses = document.body.classList;
      if (val) {
        documentClasses.add("modal-open");
      } else {
        documentClasses.remove("modal-open");
      }
    }
  },
  methods: {
    closeModal() {
      this.$emit("update:show", false);
      this.$emit("close");
    }
  }
};
</script>
<style>
.modal.show {
  background-color: rgba(0, 0, 0, 0.3);
}
</style>
