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

// Tipos de condiciones disponibles (label = translation key)
export const ALERT_CONDITIONS = [
  { value: '>',  label: 'ALERTS.CONDITIONS.GT',  icon: '📈' },
  { value: '>=', label: 'ALERTS.CONDITIONS.GTE', icon: '📊' },
  { value: '=',  label: 'ALERTS.CONDITIONS.EQ',  icon: '⚖️' },
  { value: '<=', label: 'ALERTS.CONDITIONS.LTE', icon: '📉' },
  { value: '<',  label: 'ALERTS.CONDITIONS.LT',  icon: '📉' },
  { value: '%>', label: 'ALERTS.CONDITIONS.PCT_UP',   icon: '🚀' },
  { value: '%<', label: 'ALERTS.CONDITIONS.PCT_DOWN', icon: '📉' }
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
