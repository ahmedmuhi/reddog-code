<template>
<div :span="6" style="margin:10px">
    <card style="width: 20rem" >
      <template #image>
        <img
          class="card-img-top"
          :src="orderItem.imageUrl"
          alt="Card image cap"
        />
      </template>
      <h4 class="card-title">{{ orderItem.name }}</h4>
      <p class="card-text">{{ "$" + orderItem.price }}</p>
      <a href="#" class="btn btn-primary">{{buttonText}}</a>
    </card>
</div>
</template>

<script>
export default {
  name: 'KioskOrderItem',
  props: {
    orderItem: {
      type: Object,
      required: true
    }
  },
  data() {
    return {
      buttonText: "Add To Cart",
    };
  },
  methods: {
    addToCart() {},
    //
  },
};
</script>
