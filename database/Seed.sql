-- ============================================
-- SEED DATA - INVESTLAB
-- ============================================
-- Datos reales exportados del entorno de desarrollo para levantar un entorno nuevo desde
-- cero con toda su información: portfolios, tenencias, operaciones, favoritos,
-- alertas y notificaciones.

-- ============================================
-- USERS
-- ============================================
INSERT INTO users (
    id, username, email, password_hash, created_at, password_changed_at, last_login_at,
    is_active, phone, profile_image_url, update_at, birth_date, failed_login_attempts, locked_until
)
VALUES (
    1,
    'Rocio Diaz',
    'roabigail.diaz@gmail.com',
    'AQAAAAIAAYagAAAAELmaAR2CAXunUc1mWrm0FlFn9VuqQLARQ4bbOS1bk7jhIARS4mq36kTFpGG4g8tLrQ==',
    '2026-05-31 20:25:10.605373-03',
    '2026-06-09 18:42:28.534008-03',
    '2026-06-11 21:41:43.345047-03',
    TRUE,
    '1155294225',
    '/images/1850f2ce-7fb0-44d7-bca5-c30aa55c07d2.jpg',
    '2026-06-09 21:26:17.62513-03',
    '1995-03-09 06:00:00-03',
    0,
    NULL
);

-- Para que el próximo usuario registrado no choque contra el id 1.
SELECT setval(pg_get_serial_sequence('users', 'id'), (SELECT MAX(id) FROM users));

-- ============================================
-- USER SETTINGS
-- ============================================
INSERT INTO user_settings (user_id, currency, email_notifications, alerts_used, favorites_used, operations_used_today, searches_used_today, theme, language)
VALUES
(1, 'USD', TRUE, 3, 6, 9, 0, 'Dark', 'es');

-- ============================================
-- USER PORTFOLIOS
-- ============================================
-- id 1 = portfolio original ("Mi Portfolio"), id 2 = segundo portfolio activo.
INSERT INTO user_portfolios (id, user_id, name, initial_balance, current_balance, is_active, created_at, updated_at)
VALUES
(1, 1, 'Mi Portfolio', 10000.00, 5445.45, FALSE, '2026-05-31 20:25:10.605373-03', '2026-06-18 10:39:48.186102-03'),
(2, 1, 'Mi Portfolio 1', 10000.00, 9230.31, TRUE, '2026-06-18 10:08:03.650165-03', '2026-06-18 10:40:47.073664-03');

SELECT setval(pg_get_serial_sequence('user_portfolios', 'id'), (SELECT MAX(id) FROM user_portfolios));

-- ============================================
-- PORTFOLIO HOLDINGS (posiciones actuales)
-- ============================================
INSERT INTO portfolio_holdings (user_id, portfolio_id, asset_id, quantity, avg_price)
VALUES
(1, 1, (SELECT id FROM assets WHERE symbol = 'V'),    3.000000, 319.6700),
(1, 1, (SELECT id FROM assets WHERE symbol = 'COO'),  2.000000, 66.7900),
(1, 1, (SELECT id FROM assets WHERE symbol = 'WMT'),  2.000000, 119.8300),
(1, 1, (SELECT id FROM assets WHERE symbol = 'JPM'),  4.000000, 311.8975),
(1, 1, (SELECT id FROM assets WHERE symbol = 'UBER'), 7.000000, 70.3086),
(1, 1, (SELECT id FROM assets WHERE symbol = 'AAPL'), 1.000000, 297.8767),
(1, 1, (SELECT id FROM assets WHERE symbol = 'MSFT'), 1.000000, 378.9100),
(1, 2, (SELECT id FROM assets WHERE symbol = 'MSFT'), 1.000000, 378.9100),
(1, 1, (SELECT id FROM assets WHERE symbol = 'TSLA'), 2.000000, 390.0600),
(1, 2, (SELECT id FROM assets WHERE symbol = 'TSLA'), 1.000000, 390.7800);

-- ============================================
-- TRANSACTIONS (histórico de operaciones)
-- ============================================
INSERT INTO transactions (user_id, portfolio_id, asset_id, type, quantity, price, total, balance_before, balance_after, created_at)
VALUES
(1, 1, (SELECT id FROM assets WHERE symbol = 'AAPL'), 1, 3.000000, 301.5400, 904.62,  10000.00, 9095.38, '2026-06-08 18:46:01.101752-03'),
(1, 1, (SELECT id FROM assets WHERE symbol = 'UBER'), 1, 2.000000, 70.0600,  140.12,  9095.38,  8955.26, '2026-06-08 18:46:19.421151-03'),
(1, 1, (SELECT id FROM assets WHERE symbol = 'V'),    1, 3.000000, 319.6700, 959.01,  8955.26,  7996.25, '2026-06-08 18:46:32.216276-03'),
(1, 1, (SELECT id FROM assets WHERE symbol = 'COO'),  1, 2.000000, 66.7900,  133.58,  7996.25,  7862.67, '2026-06-08 18:46:54.051574-03'),
(1, 1, (SELECT id FROM assets WHERE symbol = 'JPM'),  1, 2.000000, 311.1100, 622.22,  7862.67,  7240.45, '2026-06-08 18:47:15.198091-03'),
(1, 1, (SELECT id FROM assets WHERE symbol = 'WMT'),  1, 2.000000, 119.8300, 239.66,  7240.45,  7000.79, '2026-06-08 18:47:25.52795-03'),
(1, 1, (SELECT id FROM assets WHERE symbol = 'UBER'), 1, 3.000000, 70.4268,  211.28,  7000.79,  6789.51, '2026-06-09 16:00:02.196872-03'),
(1, 1, (SELECT id FROM assets WHERE symbol = 'JPM'),  1, 2.000000, 312.6850, 625.37,  6789.51,  6164.14, '2026-06-09 16:25:15.78012-03'),
(1, 1, (SELECT id FROM assets WHERE symbol = 'AAPL'), 2, 1.000000, 290.5500, 290.55,  6164.14,  6454.69, '2026-06-09 17:49:27.203248-03'),
(1, 1, (SELECT id FROM assets WHERE symbol = 'AAPL'), 1, 1.000000, 290.5500, 290.55,  6454.69,  6164.14, '2026-06-09 17:49:45.720328-03'),
(1, 1, (SELECT id FROM assets WHERE symbol = 'UBER'), 1, 2.000000, 70.3800,  140.76,  6164.14,  6023.38, '2026-06-09 18:24:02.276069-03'),
(1, 1, (SELECT id FROM assets WHERE symbol = 'AAPL'), 2, 1.000000, 290.5500, 290.55,  6023.38,  6313.93, '2026-06-09 18:24:43.640013-03'),
(1, 1, (SELECT id FROM assets WHERE symbol = 'AAPL'), 2, 1.000000, 290.5500, 290.55,  6313.93,  6604.48, '2026-06-09 21:19:34.55782-03'),
(1, 1, (SELECT id FROM assets WHERE symbol = 'MSFT'), 1, 1.000000, 378.9100, 378.91,  6604.48,  6225.57, '2026-06-18 10:26:08.375376-03'),
(1, 2, (SELECT id FROM assets WHERE symbol = 'MSFT'), 1, 1.000000, 378.9100, 378.91,  10000.00, 9621.09, '2026-06-18 10:26:32.768866-03'),
(1, 1, (SELECT id FROM assets WHERE symbol = 'TSLA'), 1, 2.000000, 390.0600, 780.12,  6225.57,  5445.45, '2026-06-18 10:39:48.186117-03'),
(1, 2, (SELECT id FROM assets WHERE symbol = 'TSLA'), 1, 1.000000, 390.7800, 390.78,  9621.09,  9230.31, '2026-06-18 10:40:47.073679-03');

-- ============================================
-- FAVORITES (WATCHLIST)
-- ============================================
INSERT INTO favorites (user_id, asset_id, created_at)
VALUES
(1, (SELECT id FROM assets WHERE symbol = 'MSFT'), '2026-06-04 19:53:17.443142-03'),
(1, (SELECT id FROM assets WHERE symbol = 'NVDA'), '2026-06-04 19:53:19.005331-03'),
(1, (SELECT id FROM assets WHERE symbol = 'TSLA'), '2026-06-04 19:53:20.780408-03'),
(1, (SELECT id FROM assets WHERE symbol = 'GOOGL'), '2026-06-04 19:53:37.148954-03'),
(1, (SELECT id FROM assets WHERE symbol = 'META'), '2026-06-05 17:57:12.433386-03'),
(1, (SELECT id FROM assets WHERE symbol = 'UBER'), '2026-06-09 21:18:00.926003-03');

-- ============================================
-- ALERTS
-- ============================================
-- condition_type: 1 = PRICE / 2 = PERCENTAGE — operator: 1 = > / 2 = < / 3 = >= / 4 = <= / 5 = =
INSERT INTO alerts (id, user_id, asset_id, condition_type, operator, value, is_active, created_at, last_triggered)
VALUES
(1, 1, (SELECT id FROM assets WHERE symbol = 'AAPL'), 1, 1, 10.0000,   TRUE, '2026-06-01 21:47:25.09678-03',  '2026-06-18 09:27:44.356412-03'),
(2, 1, (SELECT id FROM assets WHERE symbol = 'MSFT'), 1, 1, 250.0000,  TRUE, '2026-06-01 21:47:25.09678-03',  '2026-06-18 09:27:41.489283-03'),
(3, 1, (SELECT id FROM assets WHERE symbol = 'NVDA'), 1, 1, 3000.0000, TRUE, '2026-06-01 21:47:25.09678-03',  '2026-06-18 10:34:55.207413-03'),
(4, 1, (SELECT id FROM assets WHERE symbol = 'UBER'), 1, 1, 100.0000,  TRUE, '2026-06-02 18:07:16.390499-03', '2026-06-18 11:04:01.410839-03'),
(5, 1, (SELECT id FROM assets WHERE symbol = 'GOOGL'), 1, 2, 200.0000, TRUE, '2026-06-02 18:11:55.037942-03', NULL),
(6, 1, (SELECT id FROM assets WHERE symbol = 'TSLA'), 2, 1, 50.0000,   TRUE, '2026-06-09 16:27:18.250146-03', NULL);

SELECT setval(pg_get_serial_sequence('alerts', 'id'), (SELECT MAX(id) FROM alerts));

-- ============================================
-- NOTIFICATIONS
-- ============================================
INSERT INTO notifications (alert_id, user_id, message, price, created_at, is_read)
VALUES
(2, 1, 'TSLA superó 250 USD',             255.0000, '2026-06-01 21:47:46.421149-03', TRUE),
(1, 1, 'AAPL bajó de 160 USD',            158.0000, '2026-06-06 20:38:57.762993-03', TRUE),
(2, 1, 'TSLA superó 250 USD',             255.0000, '2026-06-06 20:38:57.762993-03', TRUE),
(2, 1, 'MSFT ha superado los $250,0000',  416.6700, '2026-06-07 20:18:28.920984-03', TRUE),
(2, 1, 'MSFT ha superado los $250,0000',  411.7400, '2026-06-08 21:29:40.395597-03', FALSE),
(2, 1, 'MSFT ha superado los $250,0000',  403.4100, '2026-06-09 21:02:37.722788-03', FALSE),
(1, 1, 'AAPL ha superado los $10,0000',   290.5500, '2026-06-09 21:02:41.901268-03', FALSE),
(2, 1, 'MSFT ha superado los $250,0000',  390.3400, '2026-06-11 21:40:08.477162-03', FALSE),
(1, 1, 'AAPL ha superado los $10,0000',   295.6300, '2026-06-11 21:40:14.253703-03', FALSE),
(2, 1, 'MSFT ha superado los $250,0000',  390.7400, '2026-06-12 21:03:16.340453-03', FALSE),
(1, 1, 'AAPL ha superado los $10,0000',   291.1300, '2026-06-12 21:03:19.288386-03', FALSE),
(2, 1, 'MSFT ha superado los $250,0000',  378.9100, '2026-06-18 09:27:41.430739-03', FALSE),
(1, 1, 'AAPL ha superado los $10,0000',   295.9500, '2026-06-18 09:27:44.354849-03', FALSE),
(3, 1, 'NVDA ha superado los $3000,0000', 207.0000, '2026-06-18 10:34:53.04863-03',  FALSE),
(4, 1, 'UBER ha superado los $100,0000',  71.5000,  '2026-06-18 11:03:59.64116-03',  FALSE);

-- NOTA: no se incluyen refresh_tokens ni user_temp_credentials del usuario original:
-- son artefactos de sesión/verificación ya usados o revocados en el entorno de
-- origen, sin ningún valor para un entorno nuevo.
