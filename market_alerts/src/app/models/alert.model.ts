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
  symbol: string;
  price: number;
  condition: string;
  message: string;
  timestamp: Date;
  isRead: boolean;
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
    symbol: 'AAPL',
    price: 149.50,
    condition: 'below',
    message: 'AAPL ha bajado por debajo de $150.00',
    timestamp: new Date(),
    isRead: false
  }
];
