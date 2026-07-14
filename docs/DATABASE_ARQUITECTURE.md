# 📊 InvestLab — Arquitectura de Base de Datos

Sistema de persistencia de datos para la aplicación de simulación financiera. Maneja información transaccional de usuarios en PostgreSQL y datos de mercado / series temporales en MongoDB, utilizando una arquitectura híbrida.

Tecnologías: PostgreSQL 17 · MongoDB 8

---

## Estructura general

**PostgreSQL** (datos críticos, source of truth)

- `users`
- `user_settings`
- `user_temp_credentials`
- `refresh_tokens`
- `assets`
- `user_portfolios`
- `portfolio_holdings`
- `transactions`
- `favorites`
- `alerts`
- `notifications`

**MongoDB** (datos de mercado y series temporales)

- `price_history`
- snapshots diarios de portfolio (documento `PortfolioHistory`)
- metadata de sincronización de mercado (documento `MarketMetadata`)

---

## Regla de responsabilidades

- PostgreSQL → estado del sistema y datos transaccionales (source of truth).
- MongoDB → históricos de precios y snapshots, de escritura periódica y lectura intensiva para gráficos.

Ambas bases se relacionan por `symbol` (activos) o por `user_id` / `portfolio_id` (histórico de portfolio). No se comparten IDs de documento entre ambas.

---

## PostgreSQL

Esquema definido en [database/DataBase.sql](../database/DataBase.sql) (sin migraciones de EF Core; el schema se versiona a mano).

### users
Usuario del sistema: credenciales, datos de contacto, estado de cuenta y control de acceso.

Columnas relevantes: `username`, `email` (único), `phone`, `birth_date`, `profile_image_url`, `password_hash`, `password_changed_at`, `last_login_at`, `is_active`, `failed_login_attempts`, `locked_until`.

### user_temp_credentials
Contraseña temporal para activación de cuenta o recuperación de contraseña, con expiración y control de uso (`expires_at`, `is_used`, `pending_email`). Un índice único (`uq_temp_active`) garantiza una sola credencial activa (no usada) por usuario.

### refresh_tokens
Refresh tokens emitidos por usuario para renovar el access token sin volver a loguear. Se invalidan al usarse, revocarse o expirar (`is_revoked`, `revoked_at`, `created_by_ip`, `user_agent`).

### assets
Catálogo global de activos financieros (acciones, índices). Evita duplicar tickers en el resto de las tablas.

Columnas: `symbol` (único, ej. `AAPL`), `name`, `sector`, `history_loaded`, `last_market_update_at`.

### user_portfolios
Cada usuario puede tener **hasta 3 portfolios** de simulación, con balance inicial y actual propios; solo uno puede estar activo a la vez (`is_active`, con índice único parcial `uq_user_portfolios_active`).

### portfolio_holdings
Posición actual de un portfolio en un activo determinado: `quantity` y `avg_price` (precio promedio de compra). Único por `(portfolio_id, asset_id)` (`uq_portfolio_asset`).

### transactions
Histórico de operaciones de compra/venta de cada portfolio.

`type`: `1 = BUY`, `2 = SELL` (constraint `chk_transactions_type`).
Guarda además `balance_before` y `balance_after` para poder reconstruir el balance en cualquier punto del historial.

### favorites
Activos seguidos por el usuario (watchlist). Único por `(user_id, asset_id)` (`uq_fav`).

### alerts
Reglas de monitoreo definidas por el usuario sobre un activo.

`condition_type`: `1 = PRICE`, `2 = PERCENTAGE`.
`operator`: `1 = >`, `2 = <`, `3 = >=`, `4 = <=`, `5 = =`.

Guarda `value` (umbral), `is_active` y `last_triggered` (última vez que se disparó).

### notifications
Eventos generados cuando una alerta se dispara: `message`, `price` (precio al momento del disparo), `is_read`.

### user_settings
Preferencias del usuario (`currency`, `theme`, `language`, `email_notifications`) y contadores de uso diario/total: `alerts_used`, `favorites_used`, `operations_used_today`, `searches_used_today`. Los límites máximos (`Limits__MaxAlerts`, `Limits__MaxFavorites`, `Limits__MaxOperationsPerDay`, `Limits__MaxDailySearches`, `Limits__InitialBalance`) se configuran por variable de entorno, no por columna.

---

## MongoDB

Colecciones/documentos definidos en `InvestLab.Models/Documents/` y accedidos vía `InvestLab.Data/Repositories/` (driver oficial de MongoDB, sin EF Core).

### price_history — `PriceHistory.cs`

```json
{
  "symbol": "AAPL",
  "date": "2026-07-13T00:00:00Z",
  "open": 210.5,
  "high": 213.1,
  "low": 209.8,
  "close": 212.3,
  "volume": 54123000
}
```

Un documento por símbolo y día (OHLCV). Poblado y actualizado por `MarketSeederWorker` (InvestLab.Workers) y purgado periódicamente por `MarketHistoryCleanupWorker`. Optimizado para consultas de rango (`GetBySymbolAndDateAsync`) y para obtener el último precio por símbolo (`GetLatestPricesAsync`).

### PortfolioHistory (snapshots diarios de portfolio)

```json
{
  "userId": 1,
  "portfolioId": 3,
  "date": "2026-07-13T00:00:00Z",
  "availableBalance": 4500.00,
  "investedValue": 5500.00,
  "totalValue": 10000.00
}
```

Generado una vez por día por `DailySnapshotWorker`, usado para graficar la evolución del valor de un portfolio a lo largo del tiempo (`PortfolioLineChart*` en el dashboard/portfolio).

### MarketMetadata

```json
{
  "last_market_close_date": "2026-07-13T00:00:00Z",
  "last_sync_date": "2026-07-13T21:05:00Z"
}
```

Registra la última fecha de cierre de mercado sincronizada y cuándo se ejecutó exitosamente la última sincronización, para que los workers de históricos sepan desde dónde continuar.

---

## Conclusión

Arquitectura híbrida: PostgreSQL como fuente de verdad para todo lo transaccional (usuarios, portfolios, alertas), y MongoDB para datos de series temporales de alto volumen (históricos de precio y snapshots de portfolio), que se consultan intensivamente para graficar pero no participan de las reglas de negocio críticas (balance, límites, validaciones de trade).
