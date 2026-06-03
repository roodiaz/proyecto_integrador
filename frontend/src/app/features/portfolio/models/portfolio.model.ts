export interface Position {

  symbol: string;
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
  initialBalance: number;
  currentBalance: number;
  profitLoss: number;
  profitLossPercent: number;
  totalOperations: number;
  maxOperations: number;
  lastMarketCloseDate: string;
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
  status?: string;
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
  days?: number | null;
  orderBy?: string;
}

export interface PortfolioTransaction {
  operationDate: string;
  symbol: string;
  type: number;
  quantity: number;
  buyPrice: number;
  total: number;
}

export interface PagedTransactions {
  data: PortfolioTransaction[];
  total: number;
}