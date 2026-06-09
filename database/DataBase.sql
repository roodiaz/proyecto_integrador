-- ============================================
-- BASE DE DATOS: INVESTLAB
-- ============================================

-- ============================================
-- USERS
-- ============================================
-- Almacena la información de los usuarios registrados,
-- incluyendo credenciales, estado de cuenta, balance virtual
-- y fecha del último inicio de sesión.
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL,
    email VARCHAR(100) NOT NULL UNIQUE,
	phone VARCHAR(20) NOT NULL,
	birth_date TIMESTAMP WITH TIME ZONE DEFAULT,
	profile_image_url TEXT,
    password_hash VARCHAR(256) NOT NULL DEFAULT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
	update_at TIMESTAMP WITH TIME ZONE DEFAULT,
    password_changed_at TIMESTAMP WITH TIME ZONE DEFAULT, 
    last_login_at TIMESTAMP WITH TIME ZONE, -- Último login del usuario
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    balance DECIMAL(18,2) NOT NULL DEFAULT 10000.00
);

-- ============================================
-- USER TEMP CREDENTIALS
-- ============================================
-- Guarda contraseñas temporales para:
-- ✔ recuperación de contraseña
-- ✔ activación de cuenta
-- Incluye expiración (ej: 15 minutos) y control de uso.
CREATE TABLE user_temp_credentials (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL,
    temp_password_hash VARCHAR(256) NOT NULL,
    pending_email VARCHAR(100),
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL ,
    is_used BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_temp_user FOREIGN KEY (user_id) REFERENCES users(id)
);

-- ============================================
-- ASSETS
-- ============================================
-- Catálogo de activos financieros (acciones, ETFs, etc.).
-- Evita duplicar tickers en múltiples tablas.
CREATE TABLE assets (
    id SERIAL PRIMARY KEY,
    symbol VARCHAR(20) UNIQUE NOT NULL, -- Ej: AAPL
    name VARCHAR(100),
    sector VARCHAR(50),
	history_loaded BOOLEAN NOT NULL DEFAULT FALSE,
	last_market_update_at TIMESTAMP WITH TIME ZONE
);

-- ============================================
-- PORTFOLIO
-- ============================================
-- Representa la posición actual del usuario en cada activo,
-- incluyendo cantidad y precio promedio de compra.
CREATE TABLE portfolio (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL,
    asset_id INT NOT NULL,
    quantity DECIMAL(18,6) NOT NULL,
    avg_price DECIMAL(18,4) NOT NULL,

    CONSTRAINT fk_portfolio_user FOREIGN KEY (user_id) REFERENCES users(id),
    CONSTRAINT fk_portfolio_asset FOREIGN KEY (asset_id) REFERENCES assets(id),
    CONSTRAINT uq_user_asset UNIQUE (user_id, asset_id)
);

-- ============================================
-- TRANSACTIONS
-- ============================================
-- Registra el historial completo de operaciones de compra y venta
-- realizadas por los usuarios.
CREATE TABLE transactions (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL,
    asset_id INT NOT NULL,
    type SMALLINT NOT NULL,  -- 1 = BUY / 2 = SELL
    quantity DECIMAL(18,6) NOT NULL,
    price DECIMAL(18,4) NOT NULL,
    total DECIMAL(18,2) NOT NULL,
    balance_before DECIMAL(18,2) NOT NULL DEFAULT 0,
    balance_after  DECIMAL(18,2) NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_transactions_user FOREIGN KEY (user_id) REFERENCES users(id),
    CONSTRAINT fk_transactions_asset FOREIGN KEY (asset_id) REFERENCES assets(id),

    CONSTRAINT chk_transactions_type CHECK (type IN (1, 2))
);

-- ============================================
-- FAVORITES
-- ============================================
-- Lista de activos marcados como favoritos por el usuario
-- para seguimiento rápido (watchlist).
CREATE TABLE favorites (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL,
    asset_id INT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_fav_user FOREIGN KEY (user_id) REFERENCES users(id),
    CONSTRAINT fk_fav_asset FOREIGN KEY (asset_id) REFERENCES assets(id),
    CONSTRAINT uq_fav UNIQUE (user_id, asset_id)
);

-- ============================================
-- ALERTS
-- ============================================
-- Define las reglas de alertas configuradas por el usuario,
-- por ejemplo:
-- ✔ Precio menor a X
-- ✔ Subida mayor a %
CREATE TABLE alerts (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL,
    asset_id INT NOT NULL,
    condition_type SMALLINT NOT NULL, -- 1 = PRICE / 2 = PERCENTAGE
    operator SMALLINT NOT NULL,
	-- 1 = >
	-- 2 = <
	-- 3 = >=
	-- 4 = <=
	-- 5 = =
    value DECIMAL(18,4) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    last_triggered TIMESTAMP WITH TIME ZONE,

    CONSTRAINT fk_alert_user FOREIGN KEY (user_id) REFERENCES users(id),
    CONSTRAINT fk_alert_asset FOREIGN KEY (asset_id) REFERENCES assets(id)
);

-- ============================================
-- NOTIFICATIONS
-- ============================================
-- Almacena las notificaciones generadas cuando una alerta se dispara,
-- incluyendo mensaje, precio al momento del disparo y estado de lectura.
CREATE TABLE notifications (
    id SERIAL PRIMARY KEY,
    alert_id INT NOT NULL,
    user_id INT NOT NULL,
    message VARCHAR(500),
    price DECIMAL(18,4) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_read BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT fk_notif_alert FOREIGN KEY (alert_id) REFERENCES alerts(id),
    CONSTRAINT fk_notif_user FOREIGN KEY (user_id) REFERENCES users(id)
);

-- ============================================
-- USER SETTINGS
-- ============================================
-- Contiene las preferencias del usuario, como:
-- ✔ moneda principal
-- ✔ activación de notificaciones por email
CREATE TABLE user_settings (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL UNIQUE,
    currency VARCHAR(10) DEFAULT 'USD',
    email_notifications BOOLEAN DEFAULT TRUE,
	alerts_used INT NOT NULL,
    favorites_used INT NOT NULL,
    operations_used_today INT NOT NULL,
    searches_used_today INT NOT NULL,
    theme VARCHAR(10) DEFAULT 'Dark',
	language VARCHAR(10) DEFAULT 'es',
	
    CONSTRAINT fk_settings_user FOREIGN KEY (user_id) REFERENCES users(id)
);

-- ============================================
-- REFRESH TOKENS
-- ============================================
-- Almacena los refresh tokens emitidos por usuario
-- para renovar el access token sin necesidad de login.
-- Se invalidan al usarse, revocarse o expirar.
CREATE TABLE refresh_tokens (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL,
    token VARCHAR(512) NOT NULL UNIQUE,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_refresh_user FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE INDEX idx_portfolio_user ON portfolio(user_id);
CREATE INDEX idx_transactions_user ON transactions(user_id);
CREATE INDEX idx_alerts_user ON alerts(user_id);
CREATE INDEX idx_notifications_user ON notifications(user_id);
CREATE INDEX idx_favorites_user ON favorites(user_id);
CREATE UNIQUE INDEX uq_temp_active ON user_temp_credentials(user_id) WHERE is_used = FALSE;
CREATE INDEX idx_refresh_tokens_user ON refresh_tokens(user_id);
CREATE INDEX idx_refresh_tokens_token ON refresh_tokens(token);

-- ============================================
-- ASSETS
-- ============================================
INSERT INTO assets (symbol, name, sector)
VALUES
('AAPL', 'Apple Inc.', 'Technology'),
('MSFT', 'Microsoft Corporation', 'Technology'),
('NVDA', 'NVIDIA Corporation', 'Technology'),
('GOOGL', 'Alphabet Inc.', 'Technology'),
('META', 'Meta Platforms Inc.', 'Technology'),
('AMZN', 'Amazon.com Inc.', 'Consumer Discretionary'),
('TSLA', 'Tesla Inc.', 'Automotive'),
('AMD', 'Advanced Micro Devices Inc.', 'Technology'),
('INTC', 'Intel Corporation', 'Technology'),
('NFLX', 'Netflix Inc.', 'Communication Services'),
('MELI', 'MercadoLibre Inc.', 'E-commerce'),
('KO', 'The Coca-Cola Company', 'Consumer Staples'),
('PEP', 'PepsiCo Inc.', 'Consumer Staples'),
('JPM', 'JPMorgan Chase & Co.', 'Financial Services'),
('V', 'Visa Inc.', 'Financial Services'),
('WMT', 'Walmart Inc.', 'Retail'),
('DIS', 'The Walt Disney Company', 'Entertainment'),
('BA', 'The Boeing Company', 'Aerospace'),
('XOM', 'Exxon Mobil Corporation', 'Energy'),
('JNJ', 'Johnson & Johnson', 'Healthcare');

INSERT INTO assets (symbol, name, sector)
VALUES
('^GSPC', 'S&P 500', 'Index'),
('^IXIC', 'NASDAQ Composite', 'Index');
('^DJI', 'Dow Jones Industrial Average', 'Index');