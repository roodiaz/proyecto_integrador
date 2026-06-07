# ── Etapa 1: build ──
FROM node:20-alpine AS build
WORKDIR /app

# Permiten sobreescribir en build-time las URLs definidas en environment.ts
# (por defecto apuntan a localhost, lo cual no sirve dentro de un contenedor)
ARG API_URL=http://localhost:5237/api
ARG SERVER_URL=http://localhost:5237

COPY package*.json ./
RUN npm ci

COPY . .

# Reemplaza los valores de environment.ts antes de compilar
RUN sed -i \
    -e "s#apiUrl: '.*'#apiUrl: '${API_URL}'#" \
    -e "s#serverUrl: '.*'#serverUrl: '${SERVER_URL}'#" \
    src/environments/environment.ts

RUN npm run build -- --configuration production

# ── Etapa 2: runtime (nginx) ──
FROM nginx:1.27-alpine AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/market_alerts/browser /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
