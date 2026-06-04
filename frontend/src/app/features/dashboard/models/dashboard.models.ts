export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

export interface DashboardTopCards {
    totalValue: number;
    todayProfit: number;
    todayProfitPercent: number;
    activeAssets: number;
    activeAlerts: number;
}