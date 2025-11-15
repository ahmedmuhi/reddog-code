<template>
  <div class="card" :class="[type && `card-${type}`]">
    <div v-if="$slots.image" class="card-image">
      <slot name="image"></slot>
    </div>
    <div v-if="$slots.header || title" class="card-header" :class="headerClasses">
      <slot name="header">
        <h4 class="card-title">{{title}}</h4>
        <p v-if="subTitle" class="card-category">{{subTitle}}</p>
      </slot>
    </div>
    <div v-if="$slots.default" class="card-body">
      <slot></slot>
    </div>
    <div v-if="$slots['image-bottom']" class="card-image">
      <slot name="image-bottom"></slot>
    </div>
    <slot name="raw-content"></slot>
    <div v-if="$slots.footer" class="card-footer" :class="footerClasses">
      <slot name="footer"></slot>
    </div>
  </div>
</template>
<script>
  export default {
    name: "BaseCard",
    props: {
      title: {
        type: String,
        default: '',
        description: "Card title"
      },
      subTitle: {
        type: String,
        default: '',
        description: "Card subtitle"
      },
      type: {
        type: String,
        default: '',
        description: "Card type (e.g primary/danger etc)"
      },
      headerClasses: {
        type: [String, Object, Array],
        default: () => [],
        description: "Card header css classes"
      },
      bodyClasses: {
        type: [String, Object, Array],
        default: () => [],
        description: "Card body css classes"
      },
      footerClasses: {
        type: [String, Object, Array],
        default: () => [],
        description: "Card footer css classes"
      }
    }
  };
</script>
<style>
</style>
