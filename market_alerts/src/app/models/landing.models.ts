export interface Stock {
  symbol: string;
  name: string;
  price: number;
  change: number;
  changePercent: number;
  volume: number;
  sector: string;
}

export interface MarketIndex {
  name: string;
  value: number;
  change: number;
  pointsChange: number;
  components: number;
}

export interface MarketNews {
  title: string;
  source: string;
  timeAgo: string;
  snippet: string;
  sentiment: number;
  tickers: string[];
}

// Datos de ejemplo
export const SAMPLE_STOCKS: Stock[] = [
  { 
    symbol: 'AAPL', 
    name: 'Apple Inc', 
    price: 185.59, 
    change: 1.23, 
    changePercent: 0.67, 
    volume: 58342901,
    sector: 'Tecnología'
  },
  { 
    symbol: 'MSFT', 
    name: 'Microsoft', 
    price: 415.27, 
    change: -2.15, 
    changePercent: -0.52, 
    volume: 25678321,
    sector: 'Tecnología'
  },
  { 
    symbol: 'AMZN', 
    name: 'Amazon.com', 
    price: 174.99, 
    change: 3.42, 
    changePercent: 1.99, 
    volume: 42356789,
    sector: 'Consumo discrecional'
  },
  { 
    symbol: 'TSLA', 
    name: 'Tesla Inc', 
    price: 174.95, 
    change: -5.67, 
    changePercent: -3.14, 
    volume: 125678432,
    sector: 'Automóviles'
  },
  { 
    symbol: 'MELI', 
    name: 'MercadoLibre', 
    price: 1765.50, 
    change: 12.34, 
    changePercent: 0.70, 
    volume: 8765432,
    sector: 'Comercio electrónico'
  }
];

export const SAMPLE_INDICES: MarketIndex[] = [
  { 
    name: 'S&P 500', 
    value: 4975.23, 
    change: 0.87, 
    pointsChange: 42.87,
    components: 505
  },
  { 
    name: 'DOW JONES', 
    value: 38654.42, 
    change: -0.23, 
    pointsChange: -89.14,
    components: 30
  },
  { 
    name: 'NASDAQ', 
    value: 15628.04, 
    change: 1.56, 
    pointsChange: 240.32,
    components: 2500
  },
  { 
    name: 'MERVAL', 
    value: 1250.75, 
    change: 2.34, 
    pointsChange: 28.65,
    components: 25
  },
  { 
    name: 'IBOVESPA', 
    value: 128546.32, 
    change: 0.45, 
    pointsChange: 578.21,
    components: 85
  }
];

export const SAMPLE_NEWS: MarketNews[] = [
  {
    title: 'Apple anuncia resultados récord en el último trimestre',
    source: 'Bloomberg',
    timeAgo: 'Hace 1 hora',
    snippet: 'Las acciones de Apple suben un 3% en el after-market tras superar las expectativas de ingresos...',
    sentiment: 1,
    tickers: ['AAPL', 'QQQ']
  },
  {
    title: 'Tesla reduce precios en China mientras aumenta la competencia',
    source: 'Reuters',
    timeAgo: 'Hace 3 horas',
    snippet: 'La compañía de vehículos eléctricos reduce los precios en un 6% en China, lo que presiona los márgenes...',
    sentiment: -1,
    tickers: ['TSLA', 'NIO', 'XPEV']
  },
  {
    title: 'La Fed mantiene tasas estables, pero advierte sobre inflación',
    source: 'Financial Times',
    timeAgo: 'Ayer',
    snippet: 'El comité de mercado abierto mantuvo las tasas en el rango actual, pero señaló que podrían ser necesarios más aumentos...',
    sentiment: 0,
    tickers: ['SPY', 'DIA', 'QQQ']
  }
];
