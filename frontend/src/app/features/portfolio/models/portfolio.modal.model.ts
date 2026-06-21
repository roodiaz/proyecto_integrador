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
  /**
   * En modo venta, si se indica, la operación queda fija sobre este portfolio
   * (p. ej. al vender desde la pantalla Portfolio, donde ya estás parado en uno) y
   * el modal no busca entre el resto ni muestra el selector. Si se omite, busca el
   * activo en todos los portfolios del usuario y siempre muestra el selector con
   * los resultados (aunque sea uno solo), como al vender desde Mercado o el FAB Operar.
   */
  portfolioId?: number;
}

export type PortfolioModalResult =
  | { mode: 'buy'; data: BuyData }
  | { mode: 'sell'; data: SellData };