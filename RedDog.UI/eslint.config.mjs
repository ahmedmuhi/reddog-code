import js from '@eslint/js';
import tseslint from 'typescript-eslint';
import vue from 'eslint-plugin-vue';
import vueParser from 'vue-eslint-parser';
import eslintConfigPrettier from 'eslint-config-prettier';
import globals from 'globals';

export default [
  {
    ignores: [
      'dist/**',
      'node_modules/**',
      'src/assets/scripts/**/*.min.js',
      '.prettierrc.cjs'
    ]
  },
  {
    languageOptions: {
      globals: {
        ...globals.browser,
        atlas: 'readonly'
      }
    }
  },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  ...vue.configs['flat/recommended'],
  {
    files: ['**/*.vue'],
    languageOptions: {
      parser: vueParser,
      parserOptions: {
        parser: tseslint.parser,
        ecmaVersion: 'latest',
        sourceType: 'module',
        extraFileExtensions: ['.vue']
      }
    }
  },
  eslintConfigPrettier,
  {
    rules: {
      'vue/require-explicit-emits': 'error',
      'vue/require-default-prop': 'error',
      'vue/no-use-v-if-with-v-for': 'error',
      'vue/no-deprecated-dollar-listeners-api': 'error',
      'vue/no-deprecated-model-definition': 'off',
      'vue/no-mutating-props': 'error'
    }
  }
];
