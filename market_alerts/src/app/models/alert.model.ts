export interface Alert {
  id: string;
  symbol: string;         // Símbolo de la acción (ej: 'AAPL')
  condition: '>' | '>=' | '=' | '<=' | '<' | '%>' | '%<';
  price?: number;         // Precio objetivo (para condiciones >, >=, =, <=, <)
  percentChange?: number; // Cambio porcentual (para condiciones %> y %<)
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
  lastTriggered?: Date;
  lastNotified?: Date;
  userId: string;         // ID del usuario que creó la alerta
}

export interface AlertHistory {
  id: string;
  alertId: string;
  message: string;
  price: number;
  priceChange: number;
  timestamp: Date;
  isRead: boolean;
  readAt?: Date;
  triggered: boolean;
}

// Tipos de condiciones disponibles
export const ALERT_CONDITIONS = [
  { value: '>', label: 'Precio mayor que' },
  { value: '>=', label: 'Precio mayor o igual que' },
  { value: '=', label: 'Precio igual a' },
  { value: '<=', label: 'Precio menor o igual que' },
  { value: '<', label: 'Precio menor que' },
  { value: '%>', label: 'Subida mayor a %' },
  { value: '%<', label: 'Bajada mayor a %' }
];

// Ejemplo de datos de alerta para pruebas
export const mockAlerts: Alert[] = [
  {
    id: '1',
    symbol: 'AAPL',
    condition: '<',
    price: 150,
    isActive: true,
    createdAt: new Date(),
    updatedAt: new Date(),
    userId: 'user1'
  },
  {
    id: '2',
    symbol: 'MSFT',
    condition: '%>',
    percentChange: 5,
    updatedAt: new Date(),
    isActive: true,
    createdAt: new Date(),
    userId: 'user1'
  },
  {
    id: '3',
    symbol: 'GOOGL',
    condition: '>=',
    price: 2800,
    updatedAt: new Date(),
    isActive: true,
    createdAt: new Date(),
    userId: 'user1'
  }
];

export const mockAlertHistory: AlertHistory[] = [
  {
    id: '1',
    alertId: '1',
    price: 149.50,
    priceChange: -1.5,
    message: 'AAPL ha bajado por debajo de $150.00',
    timestamp: new Date(),
    isRead: false,
    triggered: true
  },
  {
    id: '2',
    alertId: '2',
    price: 320.75,
    priceChange: 2.3,
    message: 'MSFT ha subido un 5% en las últimas 24h',
    timestamp: new Date(Date.now() - 3600000), // 1 hour ago
    isRead: false,
    triggered: true
  },
  {
    id: '3',
    alertId: '3',
    price: 2850.25,
    priceChange: 1.8,
    message: 'GOOGL ha alcanzado el precio objetivo',
    timestamp: new Date(Date.now() - 86400000), // 1 day ago
    isRead: true,
    triggered: true
  }
];
