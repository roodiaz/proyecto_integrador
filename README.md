# 📈 InvestLab

Plataforma web de simulación financiera desarrollada como Proyecto Integrador Final de la Tecnicatura en Desarrollo de Software (IFTS N°24).

InvestLab permite a los usuarios explorar el mercado financiero, simular operaciones de compra/venta con un balance virtual, armar y seguir un portfolio, y configurar alertas de precio que se evalúan automáticamente en segundo plano.

---

## ✨ Funcionalidades principales

- **Autenticación** — Registro, login, recuperación de contraseña y sesión basada en JWT.
- **Dashboard** — Resumen del portafolio, rendimiento del día y alertas activas.
- **Mercado** — Búsqueda de activos, precios en tiempo real, índices principales y tendencias.
- **Portafolio** — Simulación de compra/venta con balance virtual, historial de operaciones y métricas de rendimiento.
- **Alertas de precio** — Creación de reglas por símbolo, condición (`>`, `<`, `>=`, `<=`) y valor objetivo, evaluadas por un worker en segundo plano.
- **Favoritos / Watchlist** — Seguimiento rápido de los activos de interés.
- **Perfil de usuario** — Datos personales, preferencias y configuración de cuenta.
- **Notificaciones** — Registro de eventos generados cuando una alerta se dispara.

Un detalle completo de cada pantalla está en [docs/PANTALLAS_Y_FUNCIONALIDADES.md](docs/PANTALLAS_Y_FUNCIONALIDADES.md).

---

## 🏗️ Arquitectura

Arquitectura por capas en el backend (.NET) + SPA desacoplada (Angular), con persistencia híbrida y workers en segundo plano.

```
                ┌────────────────┐
                │   Angular SPA  │
                └───────┬────────┘
                        │ REST / JWT
                ┌───────▼────────┐
                │  InvestLab.Api │
                └───────┬────────┘
                        │
                ┌───────▼────────┐
                │ InvestLab.Business │
                └───────┬────────┘
                        │
                ┌───────▼────────┐
                │  InvestLab.Data │
                └───┬────────┬────┘
                    │        │
              PostgreSQL   MongoDB

InvestLab.Workers ──▶ InvestLab.Business   (alertas y precios históricos, corren en background)
Redis                                       (cache de precios de mercado)
```

- **PostgreSQL** — datos críticos y transaccionales (usuarios, portfolio, alertas, transacciones).
- **MongoDB** — históricos de precios, pensado para lectura intensiva.
- **Redis** — cache del precio de mercado, refrescado periódicamente por los workers.

Documentación técnica ampliada:
- [docs/BACKEND_ARQUITECTURE.md](docs/BACKEND_ARQUITECTURE.md)
- [docs/DATABASE_ARQUITECTURE.md](docs/DATABASE_ARQUITECTURE.md)

---

## 🛠️ Stack tecnológico

**Backend**
- .NET 8 (C#)
- Entity Framework Core
- PostgreSQL · MongoDB · Redis
- JWT (autenticación) · BCrypt (hash de contraseñas)
- Docker / Docker Compose

**Frontend**
- Angular 20
- Angular Material
- Chart.js + chartjs-chart-financial (gráficos financieros)
- ngx-translate (i18n)
- RxJS

---

## 📂 Estructura del repositorio

```
market_alerts/
├── backend/                  # Solución .NET
│   ├── InvestLab.Api/         # Controllers, middlewares, punto de entrada
│   ├── InvestLab.Business/    # Lógica de negocio (servicios)
│   ├── InvestLab.Data/        # EF Core, repositorios, migraciones
│   ├── InvestLab.Models/      # Entidades, DTOs y enums compartidos
│   ├── InvestLab.Workers/     # Procesos en background (alertas, históricos)
│   ├── InvestLab.Tests/       # Tests unitarios de Business
│   └── docker-compose.yml     # Orquestación de api, workers, frontend, postgres, mongo, redis
├── frontend/                  # SPA Angular
│   └── src/app/
│       ├── core/               # Guards, interceptors, modelos, servicios
│       ├── features/           # Alerts, Dashboard, Market, Portfolio, Settings, Watchlist, Auth
│       ├── layout/
│       └── shared/
├── database/                  # Scripts SQL (schema + seed)
└── docs/                      # Documentación funcional y de arquitectura
```

---

## 🚀 Puesta en marcha

### Requisitos previos

- [Docker](https://www.docker.com/) y Docker Compose
- [Node.js](https://nodejs.org/) 20+ y [Angular CLI](https://angular.dev/tools/cli) (solo si se corre el frontend fuera de Docker)
- [.NET SDK 8](https://dotnet.microsoft.com/) (solo si se corre el backend fuera de Docker)

### Backend + bases de datos (Docker)

```bash
cd backend
cp env .env          # completar con tus propias credenciales/API keys
docker-compose up --build
```

Esto levanta:

| Servicio             | Puerto  |
|----------------------|---------|
| API (.NET)           | 8080    |
| Frontend (Angular)   | 4200    |
| PostgreSQL           | 5432    |
| MongoDB              | 27017   |
| Redis                | 6379    |

> ⚠️ El archivo `env` incluido es solo un ejemplo para entorno local. No usar esos valores ni el `.env` resultante en un entorno productivo, y no commitear `.env` con credenciales reales.

### Frontend en modo desarrollo (sin Docker)

```bash
cd frontend
npm install
npm start          # ng serve, disponible en http://localhost:4200
```

### Tests

```bash
# Backend
cd backend
dotnet test

# Frontend
cd frontend
npm test
```

---

## 📖 Documentación adicional

- [docs/PANTALLAS_Y_FUNCIONALIDADES.md](docs/PANTALLAS_Y_FUNCIONALIDADES.md) — detalle de cada pantalla y sus estados.
- [docs/BACKEND_ARQUITECTURE.md](docs/BACKEND_ARQUITECTURE.md) — capas, responsabilidades y flujo de una request.
- [docs/DATABASE_ARQUITECTURE.md](docs/DATABASE_ARQUITECTURE.md) — modelo de datos en PostgreSQL y MongoDB.
- [docs/Manual_de_Usuario_InvestLab.docx](docs/Manual_de_Usuario_InvestLab.docx) — manual de usuario final.

---

## 🎓 Contexto académico

Proyecto desarrollado por **Rocío Díaz** como trabajo final de la materia de Desarrollo Web Backend, IFTS N°24.
