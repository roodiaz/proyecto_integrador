export interface Position {

  symbol: string;
  sector?: string;
  quantity: number;
  averagePrice: number;
  currentPrice: number;
  variationPercent: number;
  profitLoss: number;
  isOpen: boolean;
}

export interface PortfolioItem {
    symbol: string;
    currentPrice: number;
    variationPercent: number;
    isPositive: boolean;
}

export interface PortfolioBalanceCards {
  portfolioName: string | null;
  currentBalance: number;
  totalBalance: number;
  profitLoss: number;
  profitLossPercent: number;
  realizedProfitLoss: number;
  unrealizedProfitLoss: number;
  totalOperations: number;
  maxOperations: number;
  lastMarketCloseDate: string;
}

export interface PortfolioSettings {
  portfolioName: string | null;
  initialBalance: number;
  portfolioConfigured: boolean;
}

export interface SetupPortfolioRequest {
  portfolioName: string;
  initialBalance: number;
}

export interface PortfolioPieChartItem {
  symbol: string;
  currentValue: number;
  percentage: number;
}

export interface PortfolioLineChartItem {
  date: Date;
  totalValue: number;
}

export interface OpenPositionsFilter {
  page: number;
  pageSize: number;
  symbol?: string;
  sortBy?: string;
  sortDirection?: string;
}

export interface PagedOpenPositions {
  total: number;
  items: Position[];
}

export interface TransactionFilter {
  page: number;
  pageSize: number;
  symbol?: string;
  type?: number | null;
  fromDate?: string;
  toDate?: string;
  sortBy?: string;
  sortDirection?: string;
}

export interface PortfolioTransaction {
  operationDate: string;
  symbol: string;
  sector?: string;
  type: number;
  quantity: number;
  buyPrice: number;
  total: number;
}

export interface PagedTransactions {
  data: PortfolioTransaction[];
  total: number;
}