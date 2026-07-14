# Pantallas y Funcionalidades - InvestLab

## 📋 Visión General
Descripción de cada pantalla del frontend (Angular) y qué funcionalidades contiene, sin entrar en detalle de la estructura de base de datos.

---

## 🔐 1. Login / Register

### Pantalla: `features/auth/components/login-form/`, `register-form/`, `forgot-password-form/`, `registration-success/`

#### Funcionalidades
- **Login**: email y contraseña.
- **Registro de nuevo usuario**: nombre, email, contraseña, confirmación.
- **Recuperación de contraseña**: flujo de `forgot-password-form` con envío de código y `registration-success` como paso de confirmación.
- **Redirección automática**: post-login → Dashboard.

#### Elementos de UI
- Input email con validación.
- Input contraseña con mostrar/ocultar.
- Botón de ingreso con estado de carga.
- Link "¿Olvidaste tu contraseña?".
- Mensajes de error específicos por campo.

#### Estados y Validaciones
- Formulario inválido: campos vacíos, email inválido, contraseña débil.
- Credenciales incorrectas.
- Usuario ya existente (email duplicado).
- Éxito: redirección con mensaje de bienvenida.

> El token de sesión se guarda en `sessionStorage` (no persiste entre reinicios del navegador) y se renueva mediante `TokenRefreshService`.

---

## 👤 2. Dashboard

### Pantalla: `features/dashboard/`

#### Funcionalidades
- **Selector de portfolio activo**: el usuario puede tener hasta 3 portfolios; el dashboard opera sobre el que esté marcado como activo (`ActivePortfolioService`).
- **Resumen de portfolio**: balance disponible, valor invertido, valor total y rendimiento.
- **Gráfico de composición**: distribución entre disponible e invertido.
- **Comparación contra benchmarks**: chips con rendimiento del portfolio vs. índices (S&P 500, NASDAQ).
- **Últimas transacciones** y **últimas notificaciones** como widgets de acceso rápido.

#### Elementos de UI
- Cards de resumen con valores e indicadores de variación.
- Gráfico de línea/torta (Chart.js).
- Listas resumidas de transacciones y notificaciones recientes.

#### Datos que muestra
- Portfolio y transacciones: PostgreSQL, en tiempo real vía HTTP.
- Históricos para gráficos: MongoDB (precios y snapshots diarios de portfolio).

---

## 🚨 3. Alertas

### Pantalla: `features/alerts/`

#### Funcionalidades
- **Gestión de alertas**: crear, editar, eliminar, activar/desactivar.
- **Historial de notificaciones**: alertas disparadas.
- **Filtros**: por símbolo, estado, condición y rango de fechas (`createdFrom` / `createdTo`).
- **Navegación por query params**: se puede llegar a esta pantalla desde otra (ej. Mercado) con `ticker`, `view=history` o `action=create` preseteados.
- **Límite de alertas**: configurado por el backend (`Limits__MaxAlerts`), no es ilimitado; se muestra el conteo usado/total.

#### Elementos de UI
- Tabs: "Mis Alertas" y "Notificaciones".
- Cards de resumen: activas, pausadas, disparadas hoy, alertas usadas / límite.
- Tabla de alertas con switches de activar/desactivar y acciones de editar/eliminar.
- Modal de creación/edición con símbolo, condición, operador y valor objetivo.

#### Estados y Validaciones
- Alerta activa / pausada / disparada, con estilos diferenciados.
- Botón de creación deshabilitado al alcanzar el límite configurado.
- Sin alertas: mensaje con CTA para crear la primera.

---

## 📊 4. Mercado

### Pantalla: `features/market/`

#### Funcionalidades
- **Índices de mercado**: S&P 500, NASDAQ, DOW, Russell 2000.
- **Acciones en tendencia** (movers) con variación y sparkline.
- **Market Intelligence (noticias)**: carrusel real de noticias (`loadNews`, `nextNews` / `previousNews`), consumidas desde el backend — ya no es un placeholder "Próximamente".
- **Búsqueda de activos** con autocomplete.
- **Gráfico comparativo**: selector de timeframe y tipo de gráfico (línea, barras, velas), con comparación contra hasta 4 índices.
- **Compra/venta directa**: desde la ficha de un activo, sin pasar por Portafolio.
- **Favoritos inline**: toggle para agregar/quitar de watchlist directamente desde la tarjeta.

#### Elementos de UI
- Header con valores de índices e indicador positivo/negativo.
- Tarjetas de activos: símbolo, precio, variación, sparkline, botón de favorito.
- Carrusel de noticias con navegación.
- Modal de compra/venta (`PortfolioModal`).
- Buscador con resultados instantáneos.

> No existen actualmente las secciones "Sectores" ni el estado "Próximamente" que aparecían en versiones anteriores del producto.

---

## 💼 5. Portafolio

### Pantalla: `features/portfolio/`

#### Funcionalidades
- **Multi-portfolio**: tabs para navegar entre los portfolios del usuario (hasta 3), con creación de nuevos portfolios mientras no se alcance el límite.
- **Configuración inicial obligatoria**: wizard (`PortfolioSetupDialog`) para dar de alta un portfolio nuevo con nombre y balance inicial.
- **Reset de simulación**: opción para reiniciar un portfolio a su estado inicial.
- **Resumen y posiciones**: valor total, rendimiento, distribución por sector (gráfico de torta) y tabla de posiciones abiertas.
- **Historial de transacciones** con filtros.
- **Exportación a Excel**: descarga de tenencias (`downloadHoldings`) y de historial de operaciones (`downloadTransactions`).

#### Elementos de UI
- Tabs de portfolio (deshabilitado "agregar" si ya hay 3 o si es el único y no se puede eliminar).
- Tarjeta de resumen con indicador de ganancia/pérdida.
- Gráfico de torta por sector/activo.
- Tabla de posiciones: símbolo, cantidad, precio actual, valor, ganancia/pérdida.
- Botones de comprar/vender y exportar.

#### Datos que muestra
- Posiciones y transacciones: PostgreSQL, en tiempo real.
- Evolución histórica del valor del portfolio: MongoDB (snapshots diarios).

---

## ⭐ 6. Watchlist (Favoritos)

### Pantalla: `features/watchlist/`

#### Funcionalidades
- **Favoritos**: lista de activos seguidos, con **límite configurable desde el backend** (`Limits__MaxFavorites`, no un valor fijo en el frontend).
- **Agregar/eliminar ticker** con autocomplete.
- **Ordenamiento por columna** y **paginación** (7 elementos por página).
- **Sparklines**: mini-gráficos de tendencia por activo.

#### Elementos de UI
- Input de búsqueda para agregar símbolos.
- Tabla/lista de favoritos: símbolo, precio, variación, sparkline, botón eliminar.
- Indicador de uso: "X de N favoritos usados" (N viene del backend).
- Botón "Agregar" deshabilitado al alcanzar el límite.

---

## ⚙️ 7. Configuración de Usuario

### Pantalla: `features/settings/components/user-profile/`

#### Funcionalidades
- **Información personal**: nombre, email, fecha de nacimiento, teléfono.
- **Preferencias**: moneda, idioma, tema (claro/oscuro).
- **Cambio de email**: flujo con verificación por código (`onVerifyEmail` / `onValidateEmailCode`).
- **Cambio de contraseña**.
- **Avatar**: subida y cambio de foto de perfil (`onFileSelected`, `uploadProfileImage`).
- **Eliminación de cuenta**: con confirmación (`confirmDeleteAccount` / `deleteAccount`).
- **Notificaciones**: activación/desactivación de notificaciones por email.

#### Elementos de UI
- Formulario de perfil organizado en secciones.
- Avatar interactivo (click para cambiar foto).
- Switch de notificaciones por email.
- Selector de idioma y de tema.
- Sección de seguridad: cambiar contraseña, eliminar cuenta.

> No hay autenticación de dos factores (2FA) implementada en el producto actual.

---

## 🧭 8. Navegación (Sidebar / Header / Bottom Nav)

### Componentes: `shared/components/sidebar/`, `layout/layout-sidebar.ts` (contenedor), `header/`, `bottom-nav/`

#### Funcionalidades
- **Navegación principal** (desktop): sidebar con links a todas las secciones, colapsable.
- **Navegación mobile**: barra inferior (`BottomNav`) como alternativa al sidebar en pantallas chicas.
- **Header**: información del usuario activo y accesos rápidos.
- **Indicadores**: badges para notificaciones no leídas.

#### Datos que muestra
- Usuario activo: desde el token en `sessionStorage` + estado de la sesión.
- Notificaciones no leídas: contador desde el servicio correspondiente.

---

## 🔄 9. Actualización de datos

El frontend **no usa WebSockets**: no hay `socket.io`, `SignalR` ni conexiones `WebSocket` en el código. Toda la comunicación es HTTP request/response:

- Los datos se cargan al entrar a cada pantalla (`ngOnInit`) y se refrescan tras acciones del usuario (comprar, vender, crear alerta, etc.).
- No hay reconexión automática ni streaming de precios en tiempo real; la frecuencia de actualización de precios depende del ciclo de refresh interno del backend, no de push hacia el cliente.

---

## 📱 10. Estado de la Aplicación

### Servicios de estado global
- **`ActivePortfolioService`** — portfolios del usuario y cuál está activo.
- **`TokenRefreshService`** — renovación de JWT.
- **`ThemeService`** — tema claro/oscuro.
- **`LanguageService`** — idioma (i18n vía ngx-translate).
- **`SidebarService`** — estado colapsado/expandido del sidebar.
- **`ViewportService`** — breakpoints para el layout responsive.

### Manejo de estados
- Loading states durante cargas.
- Error states con mensajes específicos.
- Empty states cuando no hay datos.
- Confirmaciones de acciones exitosas.

---

## 🛡️ 11. Seguridad

### Lo que existe realmente
- **Autenticación**: JWT con refresh token, gestionado por `auth.guard.ts` y `auth.interceptor.ts`.
- **Manejo de expiración**: el interceptor distingue `INVALID_TOKEN` de `TOKEN_EXPIRED` para decidir si intenta refrescar el token o redirige a login.
- **Almacenamiento del token**: `sessionStorage` (se pierde al cerrar la pestaña/navegador).
- **Validación de inputs** en los formularios del frontend.
- **HTTPS** en el despliegue.

### No implementado actualmente
- 2FA.
- Mensajes o lógica de "sesión única por usuario".
- Bloqueo de cuenta visible desde el frontend (el backend registra intentos fallidos en `users.failed_login_attempts` / `locked_until`, pero no hay una pantalla dedicada a esto).
- Checkbox "Recordarme" en el login.

---

Este documento describe las pantallas y funcionalidades del frontend tal como existen en el código actual, sin detallar la estructura interna de la base de datos (ver [DATABASE_ARQUITECTURE.md](DATABASE_ARQUITECTURE.md)).
