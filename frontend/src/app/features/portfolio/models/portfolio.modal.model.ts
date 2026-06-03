export interface BuyData {
  ticker: string;
  quantity: number;
}

export interface SellData {
  symbol: string;
  quantity: number;
}

export interface PortfolioPosition {
  symbol: string;
  quantity: number;
  avgPrice: number;
  currentPrice: number;
}