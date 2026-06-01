-- ============================================
-- SEED DATA - INVESTLAB
-- ============================================

-- ============================================
-- USERS
-- ============================================
INSERT INTO users (username, email, password_hash, balance, last_login_at)
VALUES 
('rocio', 'rocio@test.com', 'AQAAAAIAAYagAAAAEO3uYFTiou5DX2NopjDBfMpRYhJGXHFhD/9KFZGcOSI075QAkQQzFYqdGec9K4EB2w==', 10000, NOW()),
('juan', 'juan@test.com', 'AQAAAAIAAYagAAAAEO3uYFTiou5DX2NopjDBfMpRYhJGXHFhD/9KFZGcOSI075QAkQQzFYqdGec9K4EB2w==', 10000, NOW());

-- ============================================
-- USER SETTINGS
-- ============================================
INSERT INTO user_settings (user_id, currency, email_notifications, max_alerts, max_favorites, max_operations_per_day, max_daily_searches)
VALUES
(1, 'USD', TRUE, 0, 0, 0, 0),
(2, 'USD', TRUE, 0, 0, 0, 0);

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

-- ============================================
-- FAVORITES (WATCHLIST)
-- ============================================
INSERT INTO favorites (user_id, asset_id)
VALUES
(1, 1),
(1, 2),
(1, 3),
(2, 4),
(2, 5);

-- ============================================
-- PORTFOLIO (posición actual)
-- ============================================
INSERT INTO portfolio (user_id, asset_id, quantity, avg_price)
VALUES
(1, 1, 10, 170.00), -- AAPL
(1, 2, 5, 220.00),  -- TSLA
(2, 3, 8, 2800.00); -- GOOGL

-- ============================================
-- TRANSACTIONS (histórico)
-- ============================================
INSERT INTO transactions (user_id, asset_id, type, quantity, price, total)
VALUES
(1, 1, 1, 10, 170.00, 1700.00), -- BUY
(1, 2, 1, 5, 220.00, 1100.00),  -- BUY
(2, 3, 1, 8, 2800.00, 22400.00),
(1, 1, 2, 2, 180.00, 360.00);   -- SELL

-- ============================================
-- ALERTS
-- ============================================
INSERT INTO alerts (user_id, asset_id, condition_type, operator, value)
VALUES
(1, 1, 1, 2, 160.00), -- PRICE (1), < (2) → AAPL baja
(1, 2, 1, 1, 250.00), -- PRICE (1), > (1) → TSLA sube
(2, 3, 1, 1, 3000.00); -- PRICE (1), > (1)

-- ============================================
-- NOTIFICATIONS
-- ============================================
INSERT INTO notifications (alert_id, user_id, message, price)
VALUES
(1, 1, 'AAPL bajó de 160 USD', 158.00),
(2, 1, 'TSLA superó 250 USD', 255.00);

-- ============================================
-- TEMP CREDENTIALS (reset password)
-- ============================================
INSERT INTO user_temp_credentials (user_id, temp_password_hash, expires_at)
VALUES
(1, 'TEMP_HASH_123', NOW() + INTERVAL '15 minutes'),
(2, 'TEMP_HASH_456', NOW() + INTERVAL '15 minutes');