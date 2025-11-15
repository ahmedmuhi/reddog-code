<template>
  <div
data-notify="container"
       class="alert open"
       :class="[{'alert-with-icon': icon}, verticalAlign, horizontalAlign, alertType]"
       role="alert"
       :style="customPosition"
       data-notify-position="top-center"
       @click="tryClose">
    <button
      v-if="showClose"
      type="button"
      aria-hidden="true"
      class="close col-xs-1"
      data-notify="dismiss"
      @click="close">
      <i class="tim-icons icon-simple-remove"></i>
    </button>

    <span v-if="icon" data-notify="icon" :class="['alert-icon', icon]"></span>
    <div data-notify="message">
      <div v-if="title" class="title"><b>{{title}}<br/></b></div>
      <!-- eslint-disable-next-line vue/no-v-html -->
      <div v-if="message" v-html="message"></div>
      <component :is="component" v-else-if="component"></component>
    </div>
  </div>
</template>
<script>
  export default {
    name: 'AppNotification',
  props: {
      message: {
        type: String,
        default: ''
      },
      title: {
        type: String,
        default: ''
      },
      icon: {
        type: String,
        default: ''
      },
    verticalAlign: {
      type: String,
      default: 'top',
      validator: value => {
        let acceptedValues = ['top', 'bottom'];
        return acceptedValues.indexOf(value) !== -1;
      }
    },
    horizontalAlign: {
      type: String,
      default: 'right',
      validator: value => {
        let acceptedValues = ['left', 'center', 'right'];
        return acceptedValues.indexOf(value) !== -1;
      }
    },
    type: {
      type: String,
      default: 'info',
      validator: value => {
        let acceptedValues = [
          'info',
          'primary',
          'danger',
          'warning',
          'success'
        ];
        return acceptedValues.indexOf(value) !== -1;
      }
    },
    timeout: {
      type: Number,
      default: 5000,
      validator: value => {
        return value >= 0;
      }
    },
    timestamp: {
      type: Date,
      default: () => new Date()
    },
      component: {
        type: [Object, Function],
        default: null
      },
    showClose: {
      type: Boolean,
      default: true
    },
    closeOnClick: {
      type: Boolean,
      default: true
    },
    clickHandler: {
      type: Function,
      default: null
    }
  },
  emits: ['close'],
  data() {
    return {
      elmHeight: 0
    };
  },
  computed: {
    hasIcon() {
      return this.icon && this.icon.length > 0;
    },
    alertType() {
      return `alert-${this.type}`;
    },
    customPosition() {
      let initialMargin = 20;
      let alertHeight = this.elmHeight + 10;
      let sameAlertsCount = this.$notifications.state.filter(alert => {
        return (
          alert.horizontalAlign === this.horizontalAlign &&
          alert.verticalAlign === this.verticalAlign &&
          alert.timestamp <= this.timestamp
        );
      }).length;
      if (this.$notifications.settings.overlap) {
        sameAlertsCount = 1;
      }
      let pixels = (sameAlertsCount - 1) * alertHeight + initialMargin;
      let styles = {};
      if (this.verticalAlign === 'top') {
        styles.top = `${pixels}px`;
      } else {
        styles.bottom = `${pixels}px`;
      }
      return styles;
    }
  },
  mounted() {
    this.elmHeight = this.$el.clientHeight;
    if (this.timeout) {
      setTimeout(this.close, this.timeout);
    }
  },
  methods: {
    close() {
      this.$emit('close', this.timestamp);
    },
    tryClose(evt) {
      if (this.clickHandler) {
        this.clickHandler(evt, this);
      }
      if (this.closeOnClick) {
        this.close();
      }
    }
  }
};
</script>
<style lang="scss">
.notifications .alert {
  position: fixed;
  z-index: 10000;

  &[data-notify='container'] {
    width: 480px;
    cursor: pointer;
  }

  &.center {
    left: 0px;
    right: 0px;
    margin: 0 auto;
  }
  &.left {
    left: 20px;
  }
  &.right {
    right: 20px;
  }
}
</style>
