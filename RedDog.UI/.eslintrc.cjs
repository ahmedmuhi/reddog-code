module.exports = {
  root: true,
  env: {
    browser: true,
    es2021: true
  },
  extends: ['eslint:recommended', 'plugin:vue/vue3-recommended', '@vue/eslint-config-typescript', 'prettier'],
  parserOptions: {
    ecmaVersion: 'latest',
    sourceType: 'module'
  },
  rules: {
    'vue/require-explicit-emits': 'error',
    'vue/require-default-prop': 'error',
    'vue/no-use-v-if-with-v-for': 'error',
    'vue/no-deprecated-dollar-listeners-api': 'error',
    'vue/no-mutating-props': 'error'
  }
};
