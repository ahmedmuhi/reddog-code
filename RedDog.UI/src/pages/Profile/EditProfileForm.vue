<template>
  <card>
    <template #header>
      <h5 class="title">Edit Profile</h5>
    </template>
    <div class="row">
      <div class="col-md-5 pr-md-1">
        <base-input
v-model="editableModel.company"
                  label="Company (disabled)"
                  placeholder="Company"
                  disabled>
        </base-input>
      </div>
      <div class="col-md-3 px-md-1">
        <base-input
v-model="editableModel.username"
                  label="Username"
                  placeholder="Username">
        </base-input>
      </div>
      <div class="col-md-4 pl-md-1">
        <base-input
                  label="Email address"
                  type="email"
                  placeholder="Email">
        </base-input>
      </div>
    </div>
    <div class="row">
      <div class="col-md-6 pr-md-1">
        <base-input
v-model="editableModel.firstName"
                  label="First Name"
                  placeholder="First Name">
        </base-input>
      </div>
      <div class="col-md-6 pl-md-1">
        <base-input
v-model="editableModel.lastName"
                  label="Last Name"
                  placeholder="Last Name">
        </base-input>
      </div>
    </div>
    <div class="row">
      <div class="col-md-12">
        <base-input
v-model="editableModel.address"
                  label="Address"
                  placeholder="Home Address">
        </base-input>
      </div>
    </div>
    <div class="row">
      <div class="col-md-4 pr-md-1">
        <base-input
v-model="editableModel.city"
                  label="City"
                  placeholder="City">
        </base-input>
      </div>
      <div class="col-md-4 px-md-1">
        <base-input
v-model="editableModel.country"
                  label="Country"
                  placeholder="Country">
        </base-input>
      </div>
      <div class="col-md-4 pl-md-1">
        <base-input
                  label="Postal Code"
                  placeholder="ZIP Code">
        </base-input>
      </div>
    </div>
    <div class="row">
      <div class="col-md-8">
        <base-input>
          <label>About Me</label>
          <textarea
v-model="editableModel.about" rows="4"
                    cols="80"
                    class="form-control"
                    placeholder="Here can be your description">

              </textarea>
        </base-input>
      </div>
    </div>
    <template #footer>
      <base-button type="primary" fill>Save</base-button>
    </template>
  </card>
</template>
<script>
  export default {
    props: {
      model: {
        type: Object,
        default: () => ({})
      }
    },
    emits: ['update:model'],
    data() {
      return {
        editableModel: { ...this.model }
      };
    },
    watch: {
      model: {
        deep: true,
        handler(newVal) {
          this.editableModel = { ...newVal };
        }
      },
      editableModel: {
        deep: true,
        handler(newVal) {
          this.$emit('update:model', { ...newVal });
        }
      }
    }
  }
</script>
<style>
</style>
