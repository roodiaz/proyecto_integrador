# InvestLab — Arquitectura de la Solución

Aplicación web de simulación financiera. Permite buscar activos, ver precios en tiempo real, simular compra/venta con balance virtual, gestionar un portfolio y crear alertas de precios.

**Stack:** .NET 8 · Angular · PostgreSQL · MongoDB · Docker · JWT

---

## Estructura general

```
InvestLab.sln
├── docker-compose.yml
├── InvestLab.Api
├── InvestLab.Business
├── InvestLab.Data
├── InvestLab.Models
├── InvestLab.Workers
└── InvestLab.Tests
```

### Regla de dependencias

Ninguna capa puede referenciar a una superior. `Api` nunca accede a `Data` directamente. Todo pasa por `Business`.

```
Api  →  Business  →  Data
 ↘         ↓          ↓
         Models  ←————
Workers  →  Business
Tests    →  Business
```

---

## InvestLab.Api

Capa de presentación. Recibe las peticiones HTTP, delega en `Business` y devuelve la respuesta. No contiene lógica de negocio ni queries a la base de datos.

```
InvestLab.Api/
├── Controllers/
│   ├── AlertsController.cs       → Endpoints para crear, listar y eliminar alertas de precio
│   ├── AuthController.cs         → Endpoints de registro, login y refresh de token
│   ├── DashboardController.cs    → Endpoint con resumen general del usuario (balance, activos, rendimiento)
│   ├── MarketController.cs       → Endpoints para buscar activos y consultar precios en tiempo real
│   ├── PortfolioController.cs    → Endpoints para ver el portfolio, ejecutar trades y ver historial
│   └── UserController.cs         → Endpoints para ver y editar el perfil del usuario
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs  → Captura excepciones no manejadas y devuelve respuestas de error estandarizadas
└── Program.cs                    → Punto de entrada. Registra servicios, middlewares, JWT, CORS y Swagger
```

---

## InvestLab.Business

Lógica de negocio de la aplicación. Es el cerebro del sistema. Coordina operaciones, aplica reglas del dominio y mapea entidades a DTOs. `Api` y `Workers` solo hablan con las interfaces de esta capa.

```
InvestLab.Business/
├── Interfaces/
│   ├── IAlertService.cs          → Contrato para gestión de alertas de precio
│   ├── IAuthService.cs           → Contrato para registro, login y generación de tokens
│   ├── IMarketService.cs         → Contrato para búsqueda de activos y consulta de precios
│   ├── IPortfolioService.cs      → Contrato para operaciones del portfolio y ejecución de trades
│   └── IUserService.cs           → Contrato para gestión del perfil de usuario
├── Services/
│   ├── AlertService.cs           → Crea y evalúa alertas; compara precio actual con el umbral definido
│   ├── AuthService.cs            → Valida credenciales, hashea passwords con BCrypt, emite JWT
│   ├── MarketService.cs          → Consulta la API externa de mercado, aplica caché y transforma datos
│   ├── PortfolioService.cs       → Ejecuta compras/ventas, actualiza balance virtual, calcula rendimiento
│   └── UserService.cs            → Obtiene y actualiza datos del perfil del usuario autenticado
└── Helpers/
    └── JwtHelper.cs              → Genera y valida tokens JWT (firma, claims, expiración)
```

---

## InvestLab.Data

Acceso a datos. Única capa que conoce la base de datos. El resto del sistema le habla a través de interfaces, sin saber que existe EF Core ni MongoDB.

```
InvestLab.Data/
├── Context/
│   └── InvestLabDbContext.cs     → Configuración de EF Core: DbSets, relaciones, índices y constraints
├── Interfaces/
│   ├── IAlertRepository.cs       → Contrato para operaciones CRUD de alertas en la DB
│   ├── IPortfolioRepository.cs   → Contrato para leer y actualizar portfolios y trades
│   ├── ITradeRepository.cs       → Contrato para registrar y consultar el historial de operaciones
│   └── IUserRepository.cs        → Contrato para buscar y persistir usuarios
├── Repositories/
│   ├── AlertRepository.cs        → Implementación con EF Core: queries de alertas activas por usuario
│   ├── PortfolioRepository.cs    → Implementación con EF Core: obtiene portfolio con sus trades incluidos
│   ├── TradeRepository.cs        → Implementación con EF Core: registra nuevas operaciones de compra/venta
│   └── UserRepository.cs         → Implementación con EF Core: busca usuario por email o ID
└── Migrations/                   → Archivos autogenerados por EF Core. No se editan manualmente.
                                    Generados con: dotnet ef migrations add <Nombre>
```

---

## InvestLab.Models

Modelos compartidos entre todos los proyectos. No contiene lógica, solo estructura de datos. Es el único proyecto que todos los demás pueden referenciar.

```
InvestLab.Models/
├── Entities/
│   ├── User.cs                   → Usuario del sistema (id, email, password hash, balance virtual)
│   ├── Asset.cs                  → Activo financiero (símbolo, nombre, tipo)
│   ├── Portfolio.cs              → Portfolio del usuario (relación con trades y balance)
│   ├── Trade.cs                  → Operación de compra o venta (activo, cantidad, precio, fecha)
│   └── PriceAlert.cs             → Alerta de precio (activo, umbral, condición, estado)
├── DTOs/
│   ├── Auth/
│   │   ├── LoginRequestDto.cs    → Datos que el usuario envía para iniciar sesión (email, password)
│   │   └── TokenResponseDto.cs   → Respuesta del login (access token, refresh token, expiración)
│   ├── Market/
│   │   └── AssetDto.cs           → Datos de un activo para mostrar en el frontend (símbolo, precio, variación)
│   ├── Portfolio/
│   │   ├── PortfolioDto.cs       → Resumen del portfolio del usuario (activos, valor total, rendimiento)
│   │   └── TradeRequestDto.cs    → Datos para ejecutar una operación (símbolo, cantidad, tipo)
│   └── Alert/
│       ├── AlertDto.cs           → Datos de una alerta existente para mostrar al usuario
│       └── CreateAlertDto.cs     → Datos para crear una nueva alerta (activo, precio objetivo, condición)
└── Enums/
    ├── AssetType.cs              → Tipo de activo: Stock | ETF | Crypto
    └── TradeType.cs              → Tipo de operación: Buy | Sell
```

> **Regla importante:** los controllers nunca exponen una `Entity` directamente. Siempre se mapea a un DTO antes de responder.

---

## InvestLab.Workers

Procesos en segundo plano. Corren de forma automática sin intervención del usuario. Usan `Business` para ejecutar su lógica.

```
InvestLab.Workers/
├── AlertEvaluatorWorker.cs       → Se ejecuta cada N minutos. Obtiene todas las alertas activas,
│                                   consulta el precio actual de cada activo y dispara la notificación
│                                   si se cumple la condición definida por el usuario.
└── PriceHistoryWorker.cs         → Se ejecuta una vez por día. Actualiza el historial de precios
                                    de los activos más consultados y los persiste en MongoDB.
```

---

## InvestLab.Tests

Tests unitarios de la capa de negocio. Prueba los servicios de `Business` de forma aislada, sin base de datos real ni llamadas HTTP externas.

```
InvestLab.Tests/
├── Business/
│   ├── AlertServiceTests.cs      → Verifica que las alertas se disparan bajo las condiciones correctas
│   ├── PortfolioServiceTests.cs  → Verifica que los trades actualizan correctamente el balance virtual
│   └── AuthServiceTests.cs       → Verifica el flujo de login, hashing y generación de token
└── Helpers/
    └── MockRepositoryFactory.cs  → Crea mocks de los repositorios para inyectar en los tests
```

---

## docker-compose.yml

Orquesta todos los contenedores del sistema para levantar el entorno completo con un solo comando.

| Servicio     | Descripción                              |
|--------------|------------------------------------------|
| `api`        | Contenedor de InvestLab.Api (.NET 8)     |
| `postgres`   | Base de datos principal (PostgreSQL)     |
| `mongodb`    | Base de datos de históricos (MongoDB)    |
| `angular`    | Frontend (opcional en dev)               |

```bash
# Levantar todo el entorno
docker-compose up --build
```

---

## Flujo de una petición típica

```
Usuario hace POST /api/portfolio/trade
    → PortfolioController recibe la request
    → Llama a IPortfolioService.ExecuteTradeAsync(dto)
    → PortfolioService valida reglas de negocio (balance suficiente, mercado abierto, etc.)
    → Llama a IPortfolioRepository.AddTradeAsync(trade)
    → PortfolioRepository persiste en PostgreSQL via EF Core
    → PortfolioService retorna PortfolioDto actualizado
    → Controller responde 200 OK con el DTO
```
