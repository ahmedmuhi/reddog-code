FROM node:24-alpine

LABEL org.opencontainers.image.source='https://github.com/cloudnativegbb/paas-vnext'
LABEL org.opencontainers.image.url='https://github.com/cloudnativegbb/paas-vnext'

WORKDIR /usr/src/app
COPY . .
WORKDIR "/usr/src/app/RedDog.UI"
RUN npm ci
RUN npm run build

ENV NODE_ENV="production"
ENV VITE_IS_CORP=true
ENV VITE_STORE_ID="Austin"
ENV VITE_SITE_TYPE="Pharmacia"
ENV VITE_SITE_TITLE="Contoso :: Pharmacy & Bodega"
ENV VITE_MAKELINE_BASE_URL="http://austin.makeline.brianredmond.io"
ENV VITE_ACCOUNTING_BASE_URL="http://austin.accounting.brianredmond.io"

EXPOSE 8080

CMD [ "npm", "run", "preview", "--", "--host", "0.0.0.0", "--port", "8080" ]
