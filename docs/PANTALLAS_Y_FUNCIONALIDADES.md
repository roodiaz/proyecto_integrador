# Pantallas y Funcionalidades - Market Alerts

## 📋 Visión General
Descripción detallada de cada pantalla del sistema y qué funcionalidades contiene, sin incluir estructura de tablas de base de datos.

---

## 🔐 1. Login/Register

### Pantalla: `login-form/` y `register-form/`

#### Funcionalidades
- **Formulario de ingreso**: Email y contraseña
- **Validación en tiempo real**: Email existe, contraseña segura
- **Recuperación de contraseña**: Enviar link por email
- **Registro de nuevo usuario**: Nombre completo, email, contraseña, confirmar
- **Redirección automática**: Post-login → Dashboard, Post-register → Success

#### Elementos de UI
- Input email con validación
- Input contraseña con mostrar/ocultar
- Botón "Ingresar" con loading state
- Link "¿Olvidaste tu contraseña?"
- Checkbox "Recordarme"
- Mensajes de error específicos
- Botón de registro para nuevos usuarios

#### Estados y Validaciones
- **Formulario inválido**: Campos vacíos, email inválido, contraseña débil
- **Credenciales incorrectas**: Email no encontrado, contraseña incorrecta
- **Usuario ya existe**: Email ya registrado
- **Éxito**: Redirección con mensaje de bienvenida

---

## 👤 2. Dashboard

### Pantalla: `dashboard/`

#### Funcionalidades
- **Resumen de portafolio**: Valor total, ganancias/pérdidas del día
- **Alertas activas**: Cards con estadísticas (activas, pausadas, disparadas hoy)
- **Mercado**: Índices principales y acciones en tendencia
- **Acceso rápido**: Atajos a alertas, portafolio, mercado, configuración

#### Elementos de UI
- **Cards de resumen**: Con iconos, valores y colores según estado
- **Gráficos circulares**: Distribución de portafolio
- **Tabla de alertas**: Paginación, filtros, acciones rápidas
- **Widget de mercado**: Mini-gráficos de tendencias
- **Notificaciones**: Badges para alertas no leídas

#### Datos que muestra
- **Portafolio**: Desde PostgreSQL en tiempo real
- **Alertas**: Desde PostgreSQL (activas) + MongoDB (historial)
- **Mercado**: Desde MongoDB cache + API externa

---

## 🚨 3. Alertas

### Pantalla: `alerts/`

#### Funcionalidades
- **Gestión de alertas**: Crear, editar, eliminar, activar/desactivar
- **Historial de notificaciones**: Todas las alertas disparadas
- **Filtros avanzados**: Por símbolo, estado, condición
- **Paginación**: 8 alertas por página
- **Creación modal**: Formulario para configurar nuevas alertas

#### Elementos de UI
- **Botón "Nueva Alerta"**: Siempre visible, sin restricciones
- **Tabs**: "Mis Alertas" y "Notificaciones"
- **Cards de resumen**: Activas, Pausadas, Disparadas Hoy, Utilizadas
- **Tabla de alertas**: Columnas con símbolo, condición, estado, fechas, acciones
- **Switches**: Toggle para activar/desactivar cada alerta
- **Botones de acción**: Editar y eliminar por cada alerta
- **Modal de creación**: Campos para símbolo, condición, valor objetivo

#### Estados y Validaciones
- **Alerta activa**: Verde con check ✓
- **Alerta pausada**: Gris con icono ⏸️
- **Alerta disparada**: Rojo con icono 🔴
- **Sin alertas**: Mensaje con CTA para crear primera alerta

---

## 📊 4. Mercado

### Pantalla: `market/`

#### Funcionalidades
- **Índices de mercado**: S&P 500, NASDAQ, DOW
- **Acciones en tendencia**: Top 10 acciones más activas
- **Noticias**: Sección "Próximamente"
- **Sectores**: Sección "Próximamente"
- **Búsqueda de tickers**: Buscador con autocomplete
- **Gráficos de precios**: Sparklines para cada activo

#### Elementos de UI
- **Header con resumen**: Valores de índices con indicadores positivo/negativo
- **Tarjetas de acciones**: Logo, símbolo, precio, variación, sparkline
- **Secciones deshabilitadas**: Noticias y Sectores con mensaje "Próximamente"
- **Botón "Ver Todos"**: Deshabilitado en secciones "Próximamente"
- **Buscador flotante**: Input con búsqueda instantánea

#### Datos que muestra
- **Índices**: Desde API externa en tiempo real
- **Trending stocks**: Desde MongoDB cache
- **Historial**: Desde MongoDB (si se consulta)

---

## 💼 5. Portafolio

### Pantalla: `portfolio/`

#### Funcionalidades
- **Resumen general**: Valor total, rendimiento del día, distribución por sector
- **Lista de posiciones**: Todas las tenencias del usuario
- **Gráfico circular**: Distribución del portafolio
- **Transacciones**: Historial de compras/ventas
- **Métricas**: ROI, volatilidad, rendimiento porcentual

#### Elementos de UI
- **Tarjeta de resumen**: Valor total con indicador de ganancia/pérdida
- **Gráfico de dona**: Porcentajes por sector con colores
- **Tabla de posiciones**: Símbolo, cantidad, precio actual, valor total, ganancia/pérdida
- **Botones de acción**: Comprar, vender, ver detalles
- **Filtros**: Por sector, por rendimiento

#### Datos que muestra
- **Posiciones**: Desde PostgreSQL en tiempo real
- **Transacciones**: Desde PostgreSQL
- **Métricas**: Calculadas en tiempo real desde PostgreSQL

---

## ⭐ 6. Lista de Seguimiento

### Pantalla: `watchlist/`

#### Funcionalidades
- **Favoritos**: Lista de hasta 3 tickers favoritos
- **Agregar ticker**: Input con autocomplete
- **Eliminar ticker**: Botón por cada elemento
- **Sparklines**: Mini-gráficos de tendencia
- **Límite visual**: Indicador de cuántos favoritos se usan

#### Elementos de UI
- **Input de búsqueda**: Para agregar nuevos símbolos
- **Lista de favoritos**: Símbolo, precio actual, variación, sparkline, botón eliminar
- **Indicador de límite**: "X de 3 favoritos usados"
- **Botón "Agregar": Deshabilitado cuando llega al límite
- **Sparklines individuales**: Pequeños gráficos para cada ticker

#### Datos que muestra
- **Favoritos**: Desde PostgreSQL
- **Límite**: Configurado en el componente (máximo 3)

---

## ⚙️ 7. Configuración de Usuario

### Pantalla: `user-profile/`

#### Funcionalidades
- **Información personal**: Nombre, email, fecha nacimiento, teléfono
- **Preferencias**: Moneda, idioma, zona horaria, tema
- **Seguridad**: Cambiar contraseña, 2FA
- **Notificaciones**: Email activado/desactivado
- **Avatar**: Subir/cambiar foto de perfil

#### Elementos de UI
- **Formulario de perfil**: Campos organizados en secciones
- **Avatar interactivo**: Click para cambiar foto
- **Switches de notificaciones**: Toggle para email
- **Selector de idioma**: Dropdown con opciones
- **Selector de tema**: Claro/Oscuro
- **Botones de seguridad**: Cambiar contraseña, activar 2FA

#### Datos que muestra
- **Perfil**: Desde PostgreSQL (users + user_profiles)
- **Preferencias**: Desde PostgreSQL
- **Avatar**: URL desde PostgreSQL

---

## 🧭 8. Sidebar de Navegación

### Componente: `sidebar/`

#### Funcionalidades
- **Navegación principal**: Links a todas las secciones
- **Logo dinámico**: Cambia según estado (colapsado/expandido)
- **Indicadores**: Badges para notificaciones no leídas
- **Usuario activo**: Email del usuario logueado

#### Elementos de UI
- **Menú de navegación**: Iconos + texto para cada sección
- **Logo**: Con animación de transición
- **Toggle de colapso**: Botón para expandir/contraer
- **Badges de notificación**: Números rojos para alertas no leídas

#### Datos que muestra
- **Usuario**: Desde localStorage (token) + estado global
- **Notificaciones**: Contador desde estado global

---

## 🔄 9. Datos en Tiempo Real

### WebSocket Connections

#### Funcionalidades
- **Actualización de portafolio**: Precios en tiempo real
- **Disparo de alertas**: Notificaciones instantáneas
- **Actualización de mercado**: Nuevos datos cada minuto
- **Sincronización**: Datos consistentes entre componentes

#### Flujo de datos
- **Conexión WebSocket**: Cliente se conecta al backend
- **Suscripción a eventos**: Cliente se suscribe a actualizaciones específicas
- **Broadcast de actualizaciones**: Backend envía cambios a clientes conectados
- **Reconexión automática**: Manejo de desconexiones inesperadas

#### Datos que se transmiten
- **Precios de portafolio**: Valores actualizados cada segundo
- **Estados de alertas**: Cambios de activa/pausada/disparada
- **Datos de mercado**: Nuevos precios, volúmenes, cambios porcentuales

---

## 📱 10. Estado de la Aplicación

### Estados Globales
- **Usuario autenticado**: Token válido + datos del perfil
- **Conexión WebSocket**: Conectado/Desconectado/Reconectando
- **Sincronización**: Activa/Pausada/Error
- **Caché**: Funcional/Deshabilitado

### Manejo de Estados
- **Loading states**: Indicadores durante cargas
- **Error states**: Mensajes específicos por tipo de error
- **Empty states**: Mensajes cuando no hay datos
- **Success messages**: Confirmaciones de acciones exitosas

---

## 🛡️ 11. Seguridad

### Medidas de Seguridad
- **Autenticación**: JWT con refresh tokens
- **Autorización**: Roles y permisos por endpoint
- **Validación de inputs**: Sanitización y validación estricta
- **Rate limiting**: Límites de solicitudes por usuario/IP
- **HTTPS**: Todas las comunicaciones encriptadas
- **CORS**: Configurado para dominio específico
- **SQL Injection**: Prepared statements en PostgreSQL

### Funcionalidades de Seguridad
- **Sesión activa**: Solo una sesión por usuario
- **Timeout de sesión**: Cierre automático por inactividad
- **Bloqueo de cuenta**: Demasiados intentos fallidos
- **Auditoría**: Log de todas las acciones importantes
- **Recuperación segura**: Token de un solo uso y expiración

---

Este documento describe completamente cada pantalla y funcionalidad del sistema sin entrar en detalles técnicos de implementación de base de datos, enfocándose en qué hace cada componente y qué datos maneja.
