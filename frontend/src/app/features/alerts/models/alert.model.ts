export interface Alert {
  id: number;
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

export interface CreateAlertDto {
  symbol: string;
  condition: string;
  price?: number;
  percentChange?: number;
  isActive: boolean;
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

export interface AlertDto {
  id: number;
  symbol: string;
  conditionType: number; // 1: price, 2: percent change
  operator: number; // 1: >, 2: <, 3: >=, 4: <=, 5: =
  value: number;
  isActive: boolean;
  createdAt: string;
  lastTriggered?: string;
}

export interface AlertSearchResponseDto {
  data: AlertDto[];
  total: number;
}

export interface AlertFilterDto {
  search?: string;
  isActive?: boolean;
  createdFrom?: string;
  createdTo?: string;
  page: number;
  pageSize: number;
}

export interface AlertStatsDto {
  active: number;
  paused: number;
  triggeredToday: number;
  totalUsed: number;
  limitAlerts: number;
}

export interface UpdateAlertDto extends CreateAlertDto {
  id: number;
}
