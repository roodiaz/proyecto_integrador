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

export interface DashboardLatestTransaction {
  assetSymbol: string;
  type: string;
  total: number;
}

export interface DashboardRecentNotification {
  message: string;
  price: number;
  createdAt: string;
  isRead: boolean;
}

export interface DashboardPortfolioDistribution {
  sector: string;
  value: number;
  percentage: number;
}