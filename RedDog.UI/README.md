![Red Dog :: Contoso](./public/img/android-chrome-192x192.png)
## RedDog.UI :: RedDog single site location user interface

> Prerequisites

- [Node.js](https://nodejs.org/)
- [NPM](https://npm.org) 



### QUI
> To run locally with env vars

- add a `.env` file to `RedDog.UI/` with the following (adjust per environment):
```shell
VITE_IS_CORP=false
VITE_STORE_ID="Austin"
VITE_SITE_TYPE="Pharmacy"
VITE_SITE_TITLE="Contoso :: BODEGA"
VITE_MAKELINE_BASE_URL="http://austin.makeline.brianredmond.io"
VITE_ACCOUNTING_BASE_URL="http://austin.accounting.brianredmond.io"
```




> Then run
```shell
npm install
npm run dev
```

### Rebuilding the dashboard theme

The Creative Tim Black Dashboard styles now ship as a precompiled CSS asset so Vite no longer needs to process the legacy Sass sources (removing the Dart Sass deprecation spam). If you edit anything under `src/assets/sass/black-dashboard/`, regenerate the CSS bundle before committing:

```bash
npm run build:theme
```

This command writes `src/assets/css/black-dashboard.css`, which is the file imported from `src/main.ts`.
The script intentionally runs Sass without silencing deprecation warnings, so if Dart Sass reports a warning, treat it as a blocker and fix the offending partial before committing.


## Hand crafted by
- [Lynn Orrell](https://github.com/lynn-orrell)
- [Brian Redmond](https://github.com/chzbrgr71)
- [Linda Nichols](https://github.com/lynnaloo)
- [Steve Griffith](https://github.com/swgriffith)
- [Ray Kao](https://github.com/raykao)
- [Alice J Gibbons](https://github.com/alicejgibbons)
- [Joey Schluchter](https://github.com/jschluchter)


![Cloud Native GBB](./public/img/cngbb-wide-wt.png)

---
##### Open Source Credit
Template by [Creative Tim](https://www.creative-tim.com/product/vue-black-dashboard)
