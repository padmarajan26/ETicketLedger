# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
# Empty VITE_API_URL = axios uses relative /api path, nginx proxies to api:5000
RUN VITE_API_URL="" npm run build

# ── Stage 2: Serve with Nginx ──────────────────────────────────────────────────
FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html

COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
