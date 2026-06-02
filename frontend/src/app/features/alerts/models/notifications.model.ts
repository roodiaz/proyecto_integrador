
export interface NotificationHistory {
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

export const mockNotificationHistory: NotificationHistory[] = [
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
  },
  {
    id: '4',
    alertId: '4',
    price: 820.00,
    priceChange: 2.5,
    message: 'TSLA ha superado los $800',
    timestamp: new Date(Date.now() - 7200000), // 2 hours ago
    isRead: false,
    triggered: true
  },
  {
    id: '5',
    alertId: '5',
    price: 125.50,
    priceChange: -3.2,
    message: 'AMZN ha caído más del 3%',
    timestamp: new Date(Date.now() - 10800000), // 3 hours ago
    isRead: true,
    triggered: true
  },
  {
    id: '6',
    alertId: '6',
    price: 345.00,
    priceChange: -1.4,
    message: 'META está por debajo de $350',
    timestamp: new Date(Date.now() - 14400000), // 4 hours ago
    isRead: false,
    triggered: true
  },
  {
    id: '7',
    alertId: '7',
    price: 520.00,
    priceChange: 4.0,
    message: 'NVDA ha superado los $500',
    timestamp: new Date(Date.now() - 18000000), // 5 hours ago
    isRead: true,
    triggered: true
  },
  {
    id: '8',
    alertId: '8',
    price: 380.25,
    priceChange: 4.2,
    message: 'NFLX ha subido más del 4%',
    timestamp: new Date(Date.now() - 21600000), // 6 hours ago
    isRead: false,
    triggered: true
  },
  {
    id: '9',
    alertId: '9',
    price: 118.75,
    priceChange: -1.0,
    message: 'DIS ha bajado de $120',
    timestamp: new Date(Date.now() - 25200000), // 7 hours ago
    isRead: true,
    triggered: true
  },
  {
    id: '10',
    alertId: '10',
    price: 102.50,
    priceChange: 2.5,
    message: 'BABA ha alcanzado $100',
    timestamp: new Date(Date.now() - 28800000), // 8 hours ago
    isRead: false,
    triggered: true
  },
  {
    id: '11',
    alertId: '1',
    price: 148.00,
    priceChange: -2.0,
    message: 'AAPL sigue cayendo',
    timestamp: new Date(Date.now() - 32400000), // 9 hours ago
    isRead: true,
    triggered: true
  },
  {
    id: '12',
    alertId: '2',
    price: 325.00,
    priceChange: 3.8,
    message: 'MSFT continúa subiendo',
    timestamp: new Date(Date.now() - 36000000), // 10 hours ago
    isRead: false,
    triggered: true
  }
];