# InvestLab — Arquitectura del Backend

Aplicación web de simulación financiera. Permite buscar activos, ver precios de mercado, simular compra/venta con balance virtual, gestionar hasta 3 portfolios por usuario y crear alertas de precio.

**Stack:** .NET 8 (C#) · Angular · PostgreSQL · MongoDB · Docker · JWT

---

## Estructura general

```
InvestLab.slnx
├── docker-compose.yml
├── InvestLab.Api
├── InvestLab.Business
├── InvestLab.Data
├── InvestLab.Models
├── InvestLab.Integrations
├── InvestLab.Workers
└── InvestLab.Tests
```

### Regla de dependencias

Ninguna capa puede referenciar a una superior. `Api` nunca accede a `Data` ni a `Integrations` directamente. Todo pasa por `Business`.

```
Api        →  Business  →  Data          →  PostgreSQL / MongoDB
                  ↓            ↓
               Models  ←────────
                  ↑
Business   →  Integrations                →  Yahoo / EODHD / Gmail / Resend
Workers    →  Business
Tests      →  Business, Integrations
```

---

## InvestLab.Api

Capa de presentación. Recibe las peticiones HTTP, delega en `Business` y devuelve la respuesta. No contiene lógica de negocio ni queries a la base de datos.

```
InvestLab.Api/
├── Controllers/
│   ├── BaseController.cs          → Controller base con helpers comunes (ej. obtener el userId del token)
│   ├── AlertsController.cs        → Endpoints para crear, listar, editar y eliminar alertas de precio
│   ├── AuthController.cs          → Registro, login, refresh token, verificación de cuenta y reseteo de contraseña
│   ├── ContactController.cs       → Endpoint para enviar mensajes desde el formulario de contacto
│   ├── DashboardController.cs     → Resumen del usuario: balance, composición del portfolio, rendimiento, últimas transacciones
│   ├── FavoriteController.cs      → Endpoints para agregar, listar y eliminar activos favoritos (watchlist)
│   ├── MarketController.cs        → Búsqueda de activos, precios, índices, historial y estado del mercado
│   ├── NotificationController.cs  → Listado y marcado como leídas de las notificaciones generadas por alertas
│   ├── PortfolioController.cs     → Alta de portfolio, posiciones abiertas, compra/venta de activos, gráficos
│   └── UserController.cs          → Perfil de usuario: consulta, edición, cambio de email/contraseña
├── Extensions/
│   ├── ApplicationServiceExtensions.cs → Registra los servicios de Business, Data (incl. Mongo) y sus dependencias
│   ├── AuthenticationExtensions.cs     → Configura autenticación JWT
│   ├── HealthCheckExtensions.cs        → Configura health checks de PostgreSQL y MongoDB
│   ├── RateLimitingExtensions.cs       → Configura políticas de rate limiting
│   └── SwaggerExtensions.cs            → Configura Swagger/OpenAPI (solo en Development)
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs  → Captura excepciones no manejadas y devuelve respuestas de error estandarizadas
│   └── SecurityHeadersMiddleware.cs    → Agrega headers de seguridad a todas las respuestas
└── Program.cs                     → Punto de entrada. Registra servicios, JWT, CORS, HSTS, Swagger,
                                      health checks (`/health`) y el worker interno de refresh de precios
```

---

## InvestLab.Business

Lógica de negocio de la aplicación. Coordina operaciones, aplica reglas del dominio y mapea entidades a DTOs. `Api` y `Workers` solo hablan con las interfaces de esta capa.

Se organiza en dos familias: servicios consumidos por la **API** (`Interfaces/Api`, `Services/Api`) y servicios que soportan a los **Workers** (`Interfaces/Workers`, `Services/Workers`).

```
InvestLab.Business/
├── Interfaces/Api/
│   ├── IAlertService.cs           → Contrato para gestión de alertas de precio
│   ├── IAssetService.cs           → Contrato para búsqueda y detalle de activos
│   ├── IAuthService.cs            → Contrato para registro, login, verificación y recuperación de cuenta
│   ├── IContactService.cs         → Contrato para el envío de mensajes de contacto
│   ├── IDashboardService.cs       → Contrato para el resumen agregado del dashboard
│   ├── IFavoriteService.cs        → Contrato para gestión de favoritos (watchlist)
│   ├── IJwtService.cs             → Contrato para generación y validación de tokens JWT
│   ├── IMarketPriceCacheService.cs→ Contrato para leer/escribir precios cacheados en memoria
│   ├── IMarketService.cs          → Contrato para búsqueda de activos, índices y precios en tiempo real
│   ├── IMarketStatusService.cs    → Contrato para determinar si el mercado está abierto/cerrado
│   ├── INotificationService.cs    → Contrato para listar y marcar notificaciones
│   ├── IPortfolioService.cs       → Contrato para portfolios, holdings y ejecución de trades
│   ├── ITransactionService.cs     → Contrato para el historial de operaciones
│   ├── IUserService.cs            → Contrato para gestión del perfil de usuario
│   └── IVerificationCodeService.cs→ Contrato para códigos de verificación (activación, cambio de email)
├── Interfaces/Workers/
│   ├── IAlertProcessingService.cs      → Contrato para evaluar alertas activas contra el precio actual
│   ├── IDailySnapshotWorker.cs         → Contrato para la generación del snapshot diario de portfolios
│   ├── IMarketHistoryCleanupService.cs → Contrato para purgar históricos de precios antiguos
│   ├── IMarketHistoryService.cs        → Contrato para poblar/actualizar el historial de precios
│   ├── IMarketPriceRefreshService.cs   → Contrato para refrescar el cache de precios en memoria
│   └── IUserDailyLimitsResetService.cs → Contrato para resetear los contadores diarios de uso por usuario
├── Services/Api/                  → Implementación de cada interfaz homónima en Interfaces/Api
├── Services/Workers/              → Implementación de cada interfaz homónima en Interfaces/Workers
└── Workers/
    └── MarketPriceRefreshWorker.cs → BackgroundService registrado en InvestLab.Api (Program.cs) que
                                       refresca periódicamente el cache de precios en memoria, con intervalo
                                       configurable vía MarketPriceRefresh:IntervalSeconds
```

> `AuthService` valida credenciales, hashea contraseñas con BCrypt y usa `JwtService` para emitir tokens. `MarketService`/`MarketPriceCacheService` consultan `Integrations` y aplican cache en memoria antes de tocar la base de datos.

---

## InvestLab.Data

Acceso a datos. Única capa que conoce PostgreSQL (vía EF Core) y MongoDB (vía el driver oficial). El resto del sistema le habla a través de interfaces.

```
InvestLab.Data/
├── Context/
│   └── InvestLabDbContext.cs      → Configuración de EF Core sobre PostgreSQL: DbSets, relaciones, índices y constraints
├── DependencyInjection/
│   └── MongoExtensions.cs         → Registra IMongoClient / IMongoDatabase a partir de Mongo:ConnectionString y Mongo:Database
├── Interfaces/
│   ├── IUserRepository.cs             → Búsqueda y persistencia de usuarios
│   ├── IUserSettingRepository.cs      → Preferencias y contadores de uso del usuario
│   ├── IUserTempCredentialRepository.cs → Credenciales temporales (activación, recuperación de contraseña)
│   ├── IRefreshTokenRepository.cs     → Emisión, validación y revocación de refresh tokens
│   ├── IAssetRepository.cs            → Catálogo de activos
│   ├── IUserPortfolioRepository.cs    → Portfolios del usuario (hasta 3, uno activo)
│   ├── IPortfolioHoldingRepository.cs → Posiciones (holdings) de cada portfolio
│   ├── ITransactionRepository.cs      → Historial de operaciones de compra/venta
│   ├── IFavoriteRepository.cs         → Activos favoritos (watchlist)
│   ├── IAlertRepository.cs            → CRUD de alertas de precio
│   ├── INotificationRepository.cs     → Notificaciones generadas por alertas
│   ├── IPriceHistoryRepository.cs     → Historial de precios OHLCV (MongoDB)
│   ├── IPortfolioHistoryRepository.cs → Snapshots diarios de valor de portfolio (MongoDB)
│   ├── IMarketMetadataRepository.cs   → Metadata de sincronización de mercado (MongoDB)
│   └── IUnitOfWork.cs                 → Coordina el guardado transaccional de los repositorios de PostgreSQL
└── Repositories/                  → Una implementación por cada interfaz de arriba, con el mismo nombre sin el prefijo `I`
```

> No hay carpeta `Migrations/` de EF Core: el esquema de PostgreSQL se versiona a mano en [database/DataBase.sql](../database/DataBase.sql).

---

## InvestLab.Models

Modelos compartidos entre todos los proyectos. No contiene lógica, solo estructura de datos. Es el único proyecto que todos los demás pueden referenciar.

```
InvestLab.Models/
├── Entities/                      → Mapeadas por EF Core a tablas de PostgreSQL
│   ├── User.cs                    → Usuario del sistema (credenciales, estado, intentos fallidos, bloqueo)
│   ├── UserSetting.cs             → Preferencias y límites de uso (alertas, favoritos, operaciones, búsquedas)
│   ├── UserTempCredential.cs      → Contraseña temporal para activación/recuperación de cuenta
│   ├── RefreshToken.cs            → Refresh token emitido, con expiración y revocación
│   ├── Asset.cs                   → Activo financiero (símbolo, nombre, sector)
│   ├── UserPortfolio.cs           → Portfolio de simulación del usuario (balance inicial/actual, activo o no)
│   ├── PortfolioHolding.cs        → Posición de un portfolio en un activo (cantidad, precio promedio)
│   ├── Transaction.cs             → Operación de compra o venta (activo, cantidad, precio, balance antes/después)
│   ├── Favorite.cs                → Activo marcado como favorito por el usuario
│   └── Notification.cs           → Notificación generada cuando una alerta se dispara
├── Documents/                     → Mapeados a colecciones de MongoDB
│   ├── PriceHistory.cs            → Precio OHLCV diario de un símbolo (colección `price_history`)
│   ├── PortfolioHistory.cs        → Snapshot diario de balance/valor de un portfolio
│   └── MarketMetadata.cs          → Última fecha de cierre de mercado sincronizada y fecha de la última sync
├── DTOs/
│   ├── Auth/                      → Login, registro, refresh, verificación, cambio y reseteo de contraseña
│   ├── Market/                    → Activos, índices, movers, historial, estado y cache de precios
│   ├── Portfolio/                 → Alta de portfolio, compra/venta, posiciones, gráficos de línea y torta
│   ├── Transaction/                → Historial y filtros de búsqueda de operaciones
│   ├── Alerts/                    → Filtros, creación y edición de alertas
│   ├── Favorite/                  → Alta, filtro y detalle de favoritos
│   ├── Notifications/              → Listado y filtros de notificaciones
│   ├── Dashboard/                  → Tarjetas resumen, distribución del portfolio, rendimiento, últimas transacciones
│   ├── Contact/                    → Mensaje de contacto
│   ├── User/                       → Edición de perfil y cambio de email
│   └── ExternalProvider/           → DTOs comunes usados por InvestLab.Integrations (precio, histórico)
└── Enums.cs                        → ConditionType, AlertOperator, TransactionType, MarketProviderType, EmailProviderType
```

> **Regla importante:** los controllers nunca exponen una `Entity` ni un `Document` directamente. Siempre se mapea a un DTO antes de responder.

---

## InvestLab.Integrations

Proveedores externos intercambiables. Aísla al resto del sistema de los detalles de cada API/servicio de terceros.

```
InvestLab.Integrations/
├── Configuration/
│   ├── MarketDataOptions.cs       → Configuración de proveedores de mercado (habilitado, base URL, API key)
│   └── EmailOptions.cs            → Configuración de proveedores de email
├── Interfaces/
│   ├── IExternalProvider.cs           → Contrato base de un proveedor externo
│   ├── IMarketProviderResolver.cs     → Resuelve qué proveedor de mercado usar según configuración
│   ├── IEmailProvider.cs              → Contrato de envío de email
│   └── IEmailProviderResolver.cs      → Resuelve qué proveedor de email usar según configuración
├── Providers/
│   ├── YahooMarketProvider.cs         → Precios e históricos vía Yahoo Finance
│   ├── EodHistoricalDataProvider.cs   → Precios e históricos vía EOD Historical Data
│   ├── GmailEmailProvider.cs          → Envío de email vía SMTP de Gmail
│   └── ResendEmailProvider.cs         → Envío de email vía Resend
├── Resolvers/
│   ├── MarketProviderResolver.cs      → Implementación de IMarketProviderResolver
│   └── EmailProviderResolver.cs       → Implementación de IEmailProviderResolver
└── DependencyInjection/
    ├── ExternalProviderServiceExtensions.cs → Registra los proveedores de mercado
    └── EmailProviderServiceExtensions.cs    → Registra los proveedores de email
```

El proveedor activo de cada tipo se elige por configuración (`MarketData:DefaultProvider`, `Email:Provider`), sin cambiar código en `Business`.

---

## InvestLab.Workers

Proyecto ejecutable independiente (proceso en segundo plano, corre por fuera de `InvestLab.Api`) con varios `BackgroundService`. Usan `Business` para ejecutar su lógica.

```
InvestLab.Workers/
├── Program.cs                          → Punto de entrada del proceso worker
└── Workers/
    ├── AlertWorker.cs                  → Evalúa periódicamente las alertas activas contra el precio actual (usa IAlertProcessingService) y genera notificaciones
    ├── DailySnapshotWorker.cs          → Genera una vez por día el snapshot de valor de cada portfolio en MongoDB (IDailySnapshotWorker)
    ├── MarketHistoryCleanupWorker.cs   → Purga periódicamente el historial de precios más antiguo que la ventana configurada
    ├── MarketSeederWorker.cs           → Puebla/actualiza el historial de precios (OHLCV) de los activos del catálogo
    └── UserDailyLimitsResetWorker.cs   → Resetea a medianoche los contadores diarios de uso (operaciones y búsquedas) de cada usuario
```

> Distinto de `MarketPriceRefreshWorker`, que vive en `InvestLab.Business` y corre embebido dentro de `InvestLab.Api` (refresco de cache de precios en memoria, más frecuente y de corta duración).

---

## InvestLab.Tests

Tests unitarios de `Business` e `Integrations`. Prueban los servicios de forma aislada, con mocks de los repositorios y proveedores externos.

```
InvestLab.Tests/
├── Business/Services/Api/          → Tests de AlertService, AuthService, ContactService, DashboardService,
│                                      FavoriteService, MarketService, NotificationService, PortfolioService,
│                                      TransactionService, UserService
├── Business/Services/Workers/       → Tests de AlertProcessingService, DailySnapshotService,
│                                      MarketHistoryCleanupService, MarketHistoryService,
│                                      MarketPriceRefreshService, UserDailyLimitsResetService
└── Integrations/
    ├── Providers/EodHistoricalDataProviderTests.cs
    └── Resolvers/MarketProviderResolverTests.cs
```

---

## docker-compose.yml

Orquesta todos los contenedores del sistema para levantar el entorno completo con un solo comando.

| Servicio             | Descripción                                          |
|----------------------|-------------------------------------------------------|
| `investlab.api`      | Contenedor de InvestLab.Api (.NET 8), puerto 8080     |
| `investlab.workers`  | Contenedor de InvestLab.Workers (procesos en background) |
| `investlab.frontend` | Frontend Angular servido con Nginx, puerto 4200       |
| `postgres`           | Base de datos principal (PostgreSQL 17), puerto 5432  |
| `mongo`              | Historial de precios y snapshots (MongoDB 8), puerto 27017 |

```bash
# Levantar todo el entorno
cd backend
docker-compose up --build
```

---

## Flujo de una petición típica

```
Usuario hace POST /api/portfolio/buy
    → PortfolioController recibe la request
    → Llama a IPortfolioService.BuyAssetAsync(dto)
    → PortfolioService valida reglas de negocio (balance suficiente, límite diario de operaciones, mercado abierto)
    → Obtiene el precio actual vía IMarketPriceCacheService (cache en memoria) o IMarketService (fallback a Integrations)
    → Llama a IUserPortfolioRepository / IPortfolioHoldingRepository / ITransactionRepository (vía IUnitOfWork)
    → Persiste en PostgreSQL via EF Core
    → PortfolioService retorna UserPortfolioDto actualizado
    → Controller responde 200 OK con el DTO
```

## Flujo de evaluación de alertas (background)

```
InvestLab.Workers → AlertWorker (se ejecuta cada N minutos)
    → Llama a IAlertProcessingService.ProcessActiveAlertsAsync()
    → Obtiene las alertas activas desde IAlertRepository
    → Consulta el precio actual de cada activo (cache en memoria / Integrations)
    → Si se cumple la condición (operator + value), crea una Notification vía INotificationRepository
    → Marca la alerta como disparada (last_triggered)
```
