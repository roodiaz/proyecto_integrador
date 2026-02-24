CREATE DATABASE MarketAlerts;
GO

USE MarketAlerts;
GO

-- Tabla que almacena los datos de los usuarios registrados,
-- incluyendo credenciales, fecha de creación, último cambio de contraseña,
-- estado de cuenta activo/inactivo y saldo virtual para la simulación.
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    PasswordChangedAt DATETIME NULL,   
    IsActive BIT NOT NULL,
    Balance DECIMAL(18,2) NOT NULL DEFAULT 10000.00,
	PlanId INT NOT NULL
);

-- Tabla que guarda las reglas de alertas configuradas por cada usuario,
-- con el ticker, operador de comparación y valor umbral.
-- Se usa para notificar al usuario cuando se cumple la condición.
CREATE TABLE Alerts (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Ticker NVARCHAR(20) NOT NULL,
    Operator NVARCHAR(2) NOT NULL,
    Value DECIMAL(18,4) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Alerts_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- Tabla que almacena las notificaciones generadas cuando una alerta se dispara,
-- guardando el mensaje, el usuario destino, fecha de creación y si fue leída.
CREATE TABLE Notifications (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AlertId INT NOT NULL,
    UserId INT NOT NULL,
    Message NVARCHAR(500) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    IsRead BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Notifications_Alerts FOREIGN KEY (AlertId) REFERENCES Alerts(Id),
    CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- Tabla que registra las operaciones de compra y venta simuladas del usuario,
-- con cantidad, precio y fechas de compra y venta, y un indicador si la posición está abierta.
CREATE TABLE Portfolio (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Ticker NVARCHAR(20) NOT NULL,
    Quantity DECIMAL(18,6) NOT NULL,
    BuyPrice DECIMAL(18,4) NOT NULL,
    BuyDate DATETIME NOT NULL DEFAULT GETDATE(),
    SellPrice DECIMAL(18,4) NULL,
    SellDate DATETIME NULL,
    IsOpen BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Portfolio_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- Tabla que almacena los datos históricos de precios (OHLC + volumen)
-- para cada ticker en diferentes timestamps, usada para graficar evolución y cálculos.
CREATE TABLE PriceHistory (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Ticker NVARCHAR(20) NOT NULL,
    Timestamp DATETIME NOT NULL,
    [Open] DECIMAL(18,4) NOT NULL,
    High DECIMAL(18,4) NOT NULL,
    Low DECIMAL(18,4) NOT NULL,
    [Close] DECIMAL(18,4) NOT NULL,
    Volume BIGINT NOT NULL
);

-- Tabla que lleva el registro de todas las transacciones del usuario,
-- incluyendo compras, ventas y disparo de alertas, con balance actualizado.
CREATE TABLE TransactionsLog (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Type NVARCHAR(10) NOT NULL CHECK (Type IN ('buy', 'sell', 'alert-trigger')),
    Ticker NVARCHAR(20) NOT NULL,
    Amount DECIMAL(18,4) NOT NULL,
    BalanceAfter DECIMAL(18,4) NOT NULL,
    Date DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_TransactionsLog_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- Tabla que guarda los tickers favoritos que el usuario desea seguir,
-- para mostrar en la lista de seguimiento con actualización en vivo.
CREATE TABLE Favorites (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Ticker NVARCHAR(20) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Favorites_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT UQ_Favorites_User_Ticker UNIQUE (UserId, Ticker)
);

-- Tabla para almacenar configuraciones y preferencias personalizadas
-- de cada usuario, tales como intervalo de actualización, tipo de notificación,
-- tema de la UI y moneda preferida.
CREATE TABLE UserSettings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    SettingKey NVARCHAR(100) NOT NULL,
    SettingValue NVARCHAR(500) NOT NULL,
    CONSTRAINT FK_UserSettings_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT UQ_UserSettings_User_SettingKey UNIQUE (UserId, SettingKey)
);


CREATE TABLE Plans (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    MaxDailySearch INT NULL,
    MaxAlerts INT NULL,
    MaxFavorites INT NULL,
    MaxMonthlyOperations INT NULL,
    HistoricalLimitDays INT NULL,
    HasAdvancedCharts BIT NOT NULL,
    HasEmailNotifications BIT NOT NULL,
    InitialBalance DECIMAL(18,2) NOT NULL
);
