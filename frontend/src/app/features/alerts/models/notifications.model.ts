
export interface Notification {
  id: number;
  alertId: number;
  message: string;
  price: number;
  isRead: boolean;
  createdAt: Date;
  alertSymbol: string | null;
  alertCondition: string | null;
}

export interface UnreadCountResponse {
    count: number;
}