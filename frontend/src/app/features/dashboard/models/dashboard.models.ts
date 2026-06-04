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

export interface DashboardPerformanceChartFilter {
  period: string;
}

export interface DashboardPerformanceChart {
  currentValue: number;
  variationPercent: number;
  variationText: string;
  data: DashboardPerformanceChartPoint[];
}

export interface DashboardPerformanceChartPoint {
  label: string;
  portfolio: number;
  sp500: number;
  nasdaq: number;
}