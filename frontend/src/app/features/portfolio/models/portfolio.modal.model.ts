export interface BuyData {
  ticker: string;
  quantity: number;
  portfolioId: number;
}

export interface SellData {
  symbol: string;
  quantity: number;
  portfolioId: number;
}

export interface PortfolioPosition {
  symbol: string;
  quantity: number;
  avgPrice: number;
  currentPrice: number;
}

export interface PortfolioModalData {
  mode: 'buy' | 'sell';
  symbol?: string;
}

export type PortfolioModalResult =
  | { mode: 'buy'; data: BuyData }
  | { mode: 'sell'; data: SellData };