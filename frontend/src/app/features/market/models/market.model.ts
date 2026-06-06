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

export interface MarketAsset {
  symbol: string;
  name: string;
  exchange: string;
  price: number;
  change: number | null;
  changePercent: number | null;
  open: number | null;
  volume: number | null;
  avgVolume: number | null;
  dayHigh: number | null;
  dayLow: number | null;
  marketCap: number | null;
  peRatio: number | null;
  dividendYield: number | null;
  sector: string | null;
}

export interface MarketMover {
  symbol: string;
  name: string;
  price: number;
  change: number;
  changePercent: number;
  volume: number | null;
  exchange: string | null;
  sector: string | null;
}

export interface MarketNews {
  id: string;
  title: string;
  source: string;
  url: string;
  publishedAt: string | null;
  time: string;
  summary: string;
  relatedTickers: string[];
}

export interface MarketHistoryPoint {
  date: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

export interface MarketHistorySeries {
  symbol: string;
  name: string;
  points: MarketHistoryPoint[];
}

export interface MarketAssetHistory {
  symbol: string;
  range: string;
  series: MarketHistorySeries;
}

export interface MarketComparisonHistory {
  range: string;
  series: MarketHistorySeries[];
}

