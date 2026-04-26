# 📊 InvestLab — Arquitectura de Base de Datos

Sistema de persistencia de datos para la aplicación de simulación financiera. Maneja información transaccional de usuarios y datos históricos de mercado utilizando una arquitectura híbrida.

Tecnologías: PostgreSQL · MongoDB

---

## Estructura general

PostgreSQL (datos críticos)

- users
- user_settings
- assets
- portfolio
- transactions
- favorites
- alerts
- notifications
- user_temp_credentials
- contact_messages

MongoDB (datos de mercado)

- price_history
- market_snapshots (opcional)

---

## Regla de responsabilidades

- PostgreSQL → estado del sistema (source of truth)
- MongoDB → datos de mercado (lectura intensiva)

Ninguna lógica de negocio depende de MongoDB.

---

## PostgreSQL

Base de datos relacional que almacena toda la información crítica del sistema.

### Users
Usuario del sistema: credenciales, estado, balance y actividad.

### UserSettings
Configuración y límites:
- max_alerts
- max_favorites
- max_operations_per_day
- max_daily_searches

### Assets
Catálogo global de activos. Evita duplicación y centraliza información.

### Portfolio
Posición actual del usuario por activo.

### Transactions
Histórico de operaciones.
1 = BUY
2 = SELL

### Favorites
Activos seguidos por el usuario.

### Alerts
Reglas de monitoreo:

condition_type:
1 = PRICE
2 = PERCENTAGE

operator:
1 = >
2 = <
3 = >=
4 = <=

### Notifications
Eventos generados por alertas.

### UserTempCredentials
Recuperación de contraseña y activación de cuenta.

### ContactMessages
Mensajes enviados desde contacto.

---

## MongoDB

Base de datos no relacional para datos de mercado.

### price_history

{
  "symbol": "AAPL",
  "data": [...]
}

Optimizado para consultas históricas y gráficos.

### market_snapshots (opcional)

Precios actuales para evitar llamadas constantes a APIs externas.

---

## Integración

Ambas bases se relacionan por:

symbol (ej: AAPL)

No se comparten IDs.

---

## Conclusión

Arquitectura híbrida que separa datos críticos de datos masivos, mejorando rendimiento, escalabilidad y mantenibilidad.
