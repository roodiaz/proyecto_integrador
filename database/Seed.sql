USE MarketAlerts;
GO

/* =========================================
   SEED DATA - MARKET ALERTS
========================================= */

-- =========================
-- 1. Plans
-- =========================
INSERT INTO Plans
(Name, MaxDailySearch, MaxAlerts, MaxFavorites, MaxMonthlyOperations, HistoricalLimitDays, HasAdvancedCharts, HasEmailNotifications, InitialBalance)
VALUES
('Free', 5, 1, 3, 10, 30, 0, 0, 1000),
('Premium', NULL, NULL, NULL, NULL, NULL, 1, 1, 10000);


-- =========================
-- 2. Users
-- =========================
INSERT INTO Users (Username, Email, PasswordHash, IsActive, Balance, PlanId)
VALUES
('rocio_free', 'rocio.free@mail.com', 'HASH123', 1, 1000,1),
('rocio_premium', 'rocio.premium@mail.com', 'HASH456', 1, 10000,2);


-- =========================
-- 3. User Settings
-- =========================
INSERT INTO UserSettings (UserId, SettingKey, SettingValue)
VALUES
(1, 'theme', 'light'),
(1, 'currency', 'USD'),
(1, 'alert_interval', '5'),

(2, 'theme', 'dark'),
(2, 'currency', 'USD'),
(2, 'alert_interval', '1');


-- =========================
-- 4. Favorites
-- =========================
INSERT INTO Favorites (UserId, Ticker)
VALUES
(1, 'AAPL'),
(1, 'MSFT'),
(2, 'AAPL'),
(2, 'TSLA'),
(2, 'BTC-USD');


-- =========================
-- 5. Portfolio (posiciones abiertas)
-- =========================
INSERT INTO Portfolio (UserId, Ticker, Quantity, BuyPrice)
VALUES
(1, 'AAPL', 2, 150.00),
(2, 'TSLA', 5, 200.00),
(2, 'BTC-USD', 0.25, 30000.00);


-- =========================
-- 6. TransactionsLog
-- =========================
INSERT INTO TransactionsLog (UserId, Type, Ticker, Amount, BalanceAfter)
VALUES
(1, 'buy', 'AAPL', 300.00, 700.00),
(2, 'buy', 'TSLA', 1000.00, 9000.00),
(2, 'buy', 'BTC-USD', 7500.00, 1500.00);


-- =========================
-- 7. Alerts
-- =========================
INSERT INTO Alerts (UserId, Ticker, Operator, Value)
VALUES
(1, 'AAPL', '<', 140.00),
(2, 'TSLA', '>', 250.00);


-- =========================
-- 8. Notifications (simular alerta disparada)
-- =========================
INSERT INTO Notifications (AlertId, UserId, Message)
VALUES
(1, 1, 'AAPL bajó por debajo de 140 USD'),
(2, 2, 'TSLA superó los 250 USD');
