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

// Tipos de condiciones disponibles
export const ALERT_CONDITIONS = [
  { value: '>', label: 'Precio mayor que', icon: '📈' },
  { value: '>=', label: 'Precio mayor o igual que', icon: '📊' },
  { value: '=', label: 'Precio igual a', icon: '⚖️' },
  { value: '<=', label: 'Precio menor o igual que', icon: '📉' },
  { value: '<', label: 'Precio menor que', icon: '📉' },
  { value: '%>', label: 'Subida mayor a %', icon: '🚀' },
  { value: '%<', label: 'Bajada mayor a %', icon: '📉' }
];

// Ejemplo de datos de alerta para pruebas
export const mockAlerts: Alert[] = [
  {
    id: '1',
    symbol: 'AAPL',
    condition: '<',
    price: 150,
    updatedAt: new Date(),
    isActive: true,
    createdAt: new Date(),
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
  },
  {
    id: '4',
    symbol: 'TSLA',
    condition: '>',
    price: 800,
    updatedAt: new Date(),
    isActive: false,
    createdAt: new Date(),
    userId: 'user1'
  },
  {
    id: '5',
    symbol: 'AMZN',
    condition: '%<',
    percentChange: -3,
    updatedAt: new Date(),
    isActive: true,
    createdAt: new Date(),
    userId: 'user1'
  },
  {
    id: '6',
    symbol: 'META',
    condition: '<=',
    price: 350,
    updatedAt: new Date(),
    isActive: true,
    createdAt: new Date(),
    userId: 'user1'
  },
  {
    id: '7',
    symbol: 'NVDA',
    condition: '>',
    price: 500,
    updatedAt: new Date(),
    isActive: false,
    createdAt: new Date(),
    userId: 'user1'
  },
  {
    id: '8',
    symbol: 'NFLX',
    condition: '%>',
    percentChange: 4,
    updatedAt: new Date(),
    isActive: true,
    createdAt: new Date(),
    userId: 'user1'
  },
  {
    id: '9',
    symbol: 'DIS',
    condition: '<',
    price: 120,
    updatedAt: new Date(),
    isActive: true,
    createdAt: new Date(),
    userId: 'user1'
  },
  {
    id: '10',
    symbol: 'BABA',
    condition: '>=',
    price: 100,
    updatedAt: new Date(),
    isActive: false,
    createdAt: new Date(),
    userId: 'user1'
  }
];


