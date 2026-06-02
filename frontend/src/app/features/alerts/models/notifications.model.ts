
export interface NotificationList {
  id: number;
  alertId: string;
  message: string;
  price: number;
  priceChange: number;
  timestamp: Date;
  isRead: boolean;
  readAt?: Date;
  triggered: boolean;
}

export interface UnreadCountResponse {
    count: number;
}