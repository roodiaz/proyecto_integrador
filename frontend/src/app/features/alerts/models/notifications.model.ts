
export interface Notification {
  id: number;
  alertId: number;
  message: string;
  price: number;
  isRead: boolean;
  createdAt: Date;
}

export interface UnreadCountResponse {
    count: number;
}