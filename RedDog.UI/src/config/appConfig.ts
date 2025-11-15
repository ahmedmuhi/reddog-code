const DEFAULTS = {
  IS_CORP: 'false',
  STORE_ID: 'Redmond',
  SITE_TYPE: 'Pharmacy',
  SITE_TITLE: 'Red Dog Bodega :: Market fresh food, pharmaceuticals, and fireworks!',
  MAKELINE_BASE_URL: 'http://austin.makeline.brianredmond.io',
  ACCOUNTING_BASE_URL: 'http://austin.accounting.brianredmond.io'
} as const;

const rawConfig = typeof __APP_CONFIG__ !== 'undefined' ? __APP_CONFIG__ : {};

const booleanFrom = (value: unknown) => {
  if (typeof value === 'boolean') {
    return value;
  }
  if (typeof value === 'string') {
    return value.toLowerCase() === 'true';
  }
  return false;
};

const stringFrom = (value: unknown, fallback: string) => {
  if (typeof value === 'string' && value.trim().length > 0) {
    return value;
  }
  if (typeof value === 'number') {
    return value.toString();
  }
  return fallback;
};

type AppConfig = {
  isCorp: boolean;
  storeId: string;
  siteType: string;
  siteTitle: string;
  makelineBaseUrl: string;
  accountingBaseUrl: string;
};

const env = import.meta.env;

const resolveValue = <K extends keyof typeof DEFAULTS>(key: K) => {
  const maybeFromConfig = rawConfig[key as keyof typeof rawConfig];
  const maybeFromEnv = env[`VITE_${key}` as keyof ImportMetaEnv];
  return (maybeFromConfig ?? maybeFromEnv ?? DEFAULTS[key]) as string | boolean | undefined;
};

export const appConfig: Readonly<AppConfig> = Object.freeze({
  isCorp: booleanFrom(resolveValue('IS_CORP')),
  storeId: stringFrom(resolveValue('STORE_ID'), DEFAULTS.STORE_ID),
  siteType: stringFrom(resolveValue('SITE_TYPE'), DEFAULTS.SITE_TYPE),
  siteTitle: stringFrom(resolveValue('SITE_TITLE'), DEFAULTS.SITE_TITLE),
  makelineBaseUrl: stringFrom(resolveValue('MAKELINE_BASE_URL'), DEFAULTS.MAKELINE_BASE_URL),
  accountingBaseUrl: stringFrom(resolveValue('ACCOUNTING_BASE_URL'), DEFAULTS.ACCOUNTING_BASE_URL)
});

export type { AppConfig };
