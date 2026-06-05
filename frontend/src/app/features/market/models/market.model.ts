export interface MarketIndex {
  symbol: string;
  name: string;
  value: number;
  previousClose: number;
  change: number;
  changePercent: number;
  trend: 'up' | 'down';
}

export interface MarketStatus {
  isOpen: boolean;
  statusText: string;
  marketTime: string;
  timeZone: string;
  openTime: string;
  closeTime: string;
}

export interface MarketOverview {
  marketStatus: MarketStatus;
  indices: MarketIndex[];
}