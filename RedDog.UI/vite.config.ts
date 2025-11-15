import { defineConfig, loadEnv } from 'vite';
import vue from '@vitejs/plugin-vue';
import path from 'path';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'VITE_');

  return {
    plugins: [vue()],
    resolve: {
      alias: {
        '@': path.resolve(__dirname, 'src')
      }
    },
    define: {
      __APP_CONFIG__: JSON.stringify({
        IS_CORP: env.VITE_IS_CORP,
        STORE_ID: env.VITE_STORE_ID,
        SITE_TYPE: env.VITE_SITE_TYPE,
        SITE_TITLE: env.VITE_SITE_TITLE,
        MAKELINE_BASE_URL: env.VITE_MAKELINE_BASE_URL,
        ACCOUNTING_BASE_URL: env.VITE_ACCOUNTING_BASE_URL
      })
    }
  };
});
