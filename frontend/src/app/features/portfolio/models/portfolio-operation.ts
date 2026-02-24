export interface PortfolioOperation {
  id: string;
  ticker: string;
  quantity: number;
  buyPrice: number;
  currentPrice: number;
  variationPercent: number;
  profitLoss: number;
  profitLossPercent: number;
  operationDate: Date;
  isOpen: boolean;
}
