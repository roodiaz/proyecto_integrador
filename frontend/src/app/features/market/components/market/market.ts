import { AfterViewInit, Component, OnDestroy, OnInit, effect } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MaterialModule } from '../../../../shared/material.module';
import { InfoTooltipComponent } from '../../../../shared/components/info-tooltip/info-tooltip.component';
import Chart from 'chart.js/auto';
import { CandlestickController, CandlestickElement, OhlcController, OhlcElement } from 'chartjs-chart-financial';
import 'chartjs-adapter-date-fns';
import { MarketService } from '../../services/market.service';
import { ThemeService } from '../../../../core/services/theme.service';
import { WatchlistService } from '../../../watchlist/services/watchlist.service';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { LanguageService } from '../../../../core/services/language.service';
import { MatDialog } from '@angular/material/dialog';
import { PortfolioModal } from '../../../portfolio/components/portfolio-modal/portfolio-modal';
import { PortfolioService } from '../../../portfolio/services/portfolio.service';
import { BuyData, SellData, PortfolioModalResult } from '../../../portfolio/models/portfolio.modal.model';

import {
  MarketIndex,
  MarketAsset,
  MarketMover,
  MarketNews,
  MarketAssetHistory,
  MarketComparisonHistory,
  MarketHistoryPoint
} from '../../models/market.model';

Chart.register(CandlestickController, CandlestickElement, OhlcController, OhlcElement);

/**
 * Pantalla principal del mercado.
 *
 * Muestra un panorama general (índices y estado del mercado), permite buscar y analizar
 * un activo en particular (precio, métricas, gráfico comparativo contra los principales
 * índices), comprar/vender el activo y gestionarlo como favorito, y exhibe listas de
 * tendencias, ganadores/perdedores del día y noticias del mercado.
 */
@Component({
  selector: 'app-market',
  standalone: true,
  imports: [CommonModule, FormsModule, MaterialModule, InfoTooltipComponent, TranslateModule],
  templateUrl: './market.html',
  styleUrl: './market.css'
})
export class Market implements OnInit, AfterViewInit, OnDestroy {

  marketIndices: MarketIndex[] = [];
  loadingIndices = false;
  loadingOverview = false;

  /** Tarjetas placeholder que se muestran cuando todavía no hay índices disponibles. */
  emptyIndexCards = [{ name: 'S&P 500' }, { name: 'NASDAQ' }, { name: 'Dow Jones' }];

  /** Claves de traducción para los tooltips de los índices. */
  readonly indexTooltips: Record<string, string> = {
    'S&P 500':   'MARKET.TOOLTIP_INDEX_SP500',
    'NASDAQ':    'MARKET.TOOLTIP_INDEX_NASDAQ',
    'Dow Jones': 'MARKET.TOOLTIP_INDEX_DOW'
  };

  // ── Activo seleccionado ──
  selectedSymbol = localStorage.getItem('lastMarketSymbol') || 'AAPL';
  selectedAsset: MarketAsset | null = null;
  loadingAsset = false;
  assetErrorMessage = '';
  isFavorite = false;
  favoriteLoading = false;
  hasPositionForSelectedAsset = false;
  loadingPositionStatus = false;

  /** Etiquetas placeholder que se muestran cuando todavía no hay un activo seleccionado. */
  emptyStatLabels = ['Open', 'Volume', 'Day High', 'Day Low', 'Avg Vol', 'Mkt Cap', 'P/E Ratio', 'Div Yield'];

  /** Claves de traducción para los tooltips de las métricas del activo. */
  readonly statTooltips: Record<string, string> = {
    'Open':      'MARKET.TOOLTIP_STAT_OPEN',
    'Volume':    'MARKET.TOOLTIP_STAT_VOLUME',
    'Day High':  'MARKET.TOOLTIP_STAT_DAY_HIGH',
    'Day Low':   'MARKET.TOOLTIP_STAT_DAY_LOW',
    'Avg Vol':   'MARKET.TOOLTIP_STAT_AVG_VOL',
    'Mkt Cap':   'MARKET.TOOLTIP_STAT_MKT_CAP',
    'P/E Ratio': 'MARKET.TOOLTIP_STAT_PE_RATIO',
    'Div Yield': 'MARKET.TOOLTIP_STAT_DIV_YIELD'
  };

  // ── Gráfico comparativo ──
  selectedTimeframe = '1m';
  selectedChartType: 'line' | 'bar' | 'candlestick' = 'line';
  assetHistory: MarketAssetHistory | null = null;
  comparisonHistory: MarketComparisonHistory | null = null;
  loadingChart = false;
  private lastComparisonRange = '';
  private chart: Chart | null = null;

  // ── Listas del mercado (tendencias, ganadores y perdedores) ──
  loadingTrending = false;
  loadingGainers = false;
  loadingLosers = false;
  trendingStocks: MarketMover[] = [];
  dayGainers: MarketMover[] = [];
  dayLosers: MarketMover[] = [];

  // ── Noticias ──
  activeNewsIndex = 0;
  loadingNews = false;
  marketNews: MarketNews[] = [];

  constructor(
    private marketService: MarketService,
    private watchlistService: WatchlistService,
    private portfolioService: PortfolioService,
    private snackBarService: SnackBarService,
    private languageService: LanguageService,
    private dialog: MatDialog,
    private themeService: ThemeService,
  ) {
    effect(() => {
      this.themeService.currentTheme();
      if (this.chart) {
        this.setupChart();
      }
    });
  }

  // ── Ciclo de vida ──

  /**
   * Inicializa la pantalla: limpia el estado del activo, carga el panorama del mercado,
   * las listas (tendencias/ganadores/perdedores), las noticias y el histórico comparativo,
   * busca el último activo consultado y arranca el reloj del estado del mercado.
   */
  ngOnInit(): void {
    this.loadMarketData();
    this.loadMarketOverview();
    this.loadMarketLists();
    this.loadNews();
    this.loadComparisonHistory();
    this.searchAsset();
  }

  /**
   * Una vez que la vista está lista, arma el gráfico comparativo en el siguiente ciclo
   * de detección de cambios (para asegurar que el `<canvas>` ya esté en el DOM).
   */
  ngAfterViewInit(): void {
    setTimeout(() => this.setupChart(), 0);
  }

  /** Destruye el gráfico al salir de la pantalla. */
  ngOnDestroy(): void {
    if (this.chart) this.chart.destroy();
  }

  // ── Getters ──

  /**
   * Noticia actualmente mostrada en el carrusel de "Market Intelligence".
   * @returns La noticia activa según `activeNewsIndex`, o una noticia vacía si todavía no hay datos.
   */
  get activeNews(): MarketNews {
    return this.marketNews[this.activeNewsIndex] ?? {
      id: '',
      title: '',
      source: '',
      url: '',
      publishedAt: null,
      time: '',
      summary: '',
      relatedTickers: []
    };
  }

  /**
   * Texto explicativo del gráfico comparativo, según el tipo de gráfico seleccionado.
   * @returns Descripción del gráfico de velas o del gráfico de comparación contra el mercado.
   */
  get chartTooltipText(): string {
    if (this.selectedChartType === 'candlestick')
      return 'MARKET.TOOLTIP_CHART_CANDLESTICK';
    return 'MARKET.TOOLTIP_CHART_COMPARE';
  }

  // ── Carga de datos ──

  /** Reinicia el estado del activo y de las tendencias antes de cargar la información inicial. */
  private loadMarketData(): void {
    this.trendingStocks = [];
    this.selectedAsset = null;
    this.hasPositionForSelectedAsset = false;
  }

  /**
   * Obtiene el panorama general del mercado (estado e índices principales) y actualiza
   * los indicadores de carga correspondientes.
   */
  private loadMarketOverview(): void {
    this.loadingOverview = true;
    this.loadingIndices = true;

    this.marketService.getMarketOverview().subscribe({
      next: response => {
        this.marketIndices = response.success && response.data ? (response.data.indices ?? []) : [];
        this.loadingOverview = false;
        this.loadingIndices = false;
      },
      error: error => {
        console.error('Error al obtener panorama de mercado', error);
        this.marketIndices = [];
        this.loadingOverview = false;
        this.loadingIndices = false;
      }
    });
  }

  /** Dispara la carga de las tres listas inferiores: tendencias, ganadores y perdedores del día. */
  private loadMarketLists(): void {
    this.loadTrending();
    this.loadGainers();
    this.loadLosers();
  }

  /** Carga los activos con mayor actividad de trading del momento. */
  private loadTrending(): void {
    this.loadingTrending = true;

    this.marketService.getTrending().subscribe({
      next: response => {
        this.trendingStocks = response.success ? response.data ?? [] : [];
        this.loadingTrending = false;
      },
      error: error => {
        console.error('Error al obtener tendencias', error);
        this.trendingStocks = [];
        this.loadingTrending = false;
      }
    });
  }

  /** Carga los activos que más subieron de precio durante el día. */
  private loadGainers(): void {
    this.loadingGainers = true;

    this.marketService.getGainers().subscribe({
      next: response => {
        this.dayGainers = response.success ? response.data ?? [] : [];
        this.loadingGainers = false;
      },
      error: error => {
        console.error('Error al obtener ganadores', error);
        this.dayGainers = [];
        this.loadingGainers = false;
      }
    });
  }

  /** Carga los activos que más bajaron de precio durante el día. */
  private loadLosers(): void {
    this.loadingLosers = true;

    this.marketService.getLosers().subscribe({
      next: response => {
        this.dayLosers = response.success ? response.data ?? [] : [];
        this.loadingLosers = false;
      },
      error: error => {
        console.error('Error al obtener perdedores', error);
        this.dayLosers = [];
        this.loadingLosers = false;
      }
    });
  }

  /** Carga las noticias del mercado y reinicia el carrusel de "Market Intelligence". */
  private loadNews(): void {
    this.loadingNews = true;

    this.marketService.getMarketNews().subscribe({
      next: response => {
        this.marketNews = response.success ? response.data ?? [] : [];
        this.activeNewsIndex = 0;
        this.loadingNews = false;
      },
      error: error => {
        console.error('Error al obtener noticias del mercado', error);
        this.marketNews = [];
        this.activeNewsIndex = 0;
        this.loadingNews = false;
      }
    });
  }

  /**
   * Carga el histórico comparativo (activo vs. principales índices) para el rango de
   * tiempo actualmente seleccionado. Si ya se cuenta con datos para ese mismo rango,
   * evita la nueva petición y simplemente vuelve a armar el gráfico.
   */
  private loadComparisonHistory(): void {
    if (this.lastComparisonRange === this.selectedTimeframe && this.comparisonHistory?.series?.length) {
      this.setupChart();
      return;
    }

    this.loadingChart = true;

    this.marketService.getComparisonHistory(this.selectedTimeframe).subscribe({
      next: response => {
        this.comparisonHistory = response.success ? response.data ?? null : null;
        this.lastComparisonRange = this.selectedTimeframe;
        this.loadingChart = false;
        this.setupChart();
      },
      error: error => {
        console.error('Error al obtener histórico de comparación', error);
        this.comparisonHistory = null;
        this.loadingChart = false;
        this.setupChart();
      }
    });
  }

  /**
   * Carga el histórico de precios del activo seleccionado para el rango de tiempo actual
   * y vuelve a armar el gráfico al finalizar. Si no hay un símbolo válido, limpia el
   * histórico existente.
   */
  private loadAssetHistory(): void {
    const symbol = this.selectedSymbol.trim().toUpperCase();

    if (!symbol) {
      this.assetHistory = null;
      this.setupChart();
      return;
    }

    this.loadingChart = true;

    this.marketService.getAssetHistory(symbol, this.selectedTimeframe).subscribe({
      next: response => {
        this.assetHistory = response.success ? response.data ?? null : null;
        this.loadingChart = false;
        this.setupChart();
      },
      error: error => {
        console.error('Error al obtener histórico del activo', error);
        this.assetHistory = null;
        this.loadingChart = false;
        this.setupChart();
      }
    });
  }

  /**
   * Consulta si el usuario tiene una posición abierta (cantidad > 0) para el símbolo dado,
   * para habilitar o deshabilitar el botón de venta.
   * @param symbol Símbolo del activo a consultar.
   */
  loadPositionStatus(symbol: string): void {
    if (!symbol) {
      this.hasPositionForSelectedAsset = false;
      return;
    }

    this.loadingPositionStatus = true;

    this.portfolioService.getPosition(symbol).subscribe({
      next: response => {
        this.loadingPositionStatus = false;
        this.hasPositionForSelectedAsset = !!(response.success && response.data && response.data.quantity > 0);
      },
      error: () => {
        this.loadingPositionStatus = false;
        this.hasPositionForSelectedAsset = false;
      }
    });
  }

  /**
   * Consulta si el símbolo dado está en la lista de favoritos del usuario y actualiza
   * el estado del botón de favorito.
   * @param symbol Símbolo del activo a consultar.
   */
  loadFavoriteStatus(symbol: string): void {
    if (!symbol) return;

    this.watchlistService.existsFavorite(symbol).subscribe({
      next: res => {
        if (res.success) this.isFavorite = res.data!;
      },
      error: err => {
        console.error('Error consultando favorito', err);
        this.isFavorite = false;
      }
    });
  }

  // ── Acciones del usuario ──

  /**
   * Busca el activo cuyo símbolo está cargado en `selectedSymbol`, actualiza el activo
   * seleccionado y dispara la carga de su histórico, estado de favorito y de posición.
   * Si el símbolo está vacío, muestra un mensaje pidiendo que se ingrese uno.
   */
  searchAsset(): void {
    const symbol = this.selectedSymbol.trim().toUpperCase();

    if (!symbol) {
      this.assetErrorMessage = this.languageService.instant('MARKET.ERRORS.SYMBOL_REQUIRED');
      this.selectedAsset = null;
      this.hasPositionForSelectedAsset = false;
      return;
    }

    this.selectedSymbol = symbol;
    this.loadingAsset = true;
    this.assetErrorMessage = '';

    this.marketService.getAssetDetail(symbol).subscribe({
      next: response => {
        this.loadingAsset = false;

        if (!response.success || !response.data) {
          this.selectedAsset = null;
          this.hasPositionForSelectedAsset = false;
          this.assetErrorMessage = response.message || this.languageService.instant('MARKET.ERRORS.ASSET_NOT_FOUND');
          return;
        }

        this.selectedAsset = response.data;
        this.selectedSymbol = response.data.symbol;
        localStorage.setItem('lastMarketSymbol', response.data.symbol);
        this.loadAssetHistory();
        this.assetErrorMessage = '';
        this.loadFavoriteStatus(this.selectedSymbol);
        this.loadPositionStatus(this.selectedSymbol);
      },
      error: error => {
        console.error('Error al obtener detalle del activo', error);
        this.loadingAsset = false;
        this.selectedAsset = null;
        this.hasPositionForSelectedAsset = false;
        this.assetErrorMessage = this.languageService.instant('MARKET.ERRORS.ASSET_FETCH_FAILED');
      }
    });
  }

  /**
   * Selecciona un nuevo activo (por ejemplo, al hacer clic en un ítem de una lista) y
   * dispara la búsqueda correspondiente si el símbolo cambió.
   * @param symbol Símbolo del activo a seleccionar.
   */
  selectAsset(symbol: string): void {
    const normalizedSymbol = symbol.trim().toUpperCase();

    if (!normalizedSymbol || normalizedSymbol === this.selectedSymbol)
      return;

    this.selectedSymbol = normalizedSymbol;
    this.searchAsset();
  }

  /**
   * Cambia el rango de tiempo del gráfico comparativo y vuelve a cargar tanto el
   * histórico comparativo como el del activo seleccionado para ese nuevo rango.
   * @param timeframe Nuevo rango de tiempo (por ejemplo `'1d'`, `'1w'`, `'1m'`, `'1y'`).
   */
  changeTimeframe(timeframe: string): void {
    if (timeframe === this.selectedTimeframe) return;

    this.selectedTimeframe = timeframe;
    this.loadComparisonHistory();
    this.loadAssetHistory();
  }

  /**
   * Cambia el tipo de gráfico (línea, barras o velas) y vuelve a armar el gráfico
   * con los datos ya disponibles.
   * @param type Nuevo tipo de gráfico.
   */
  changeChartType(type: 'line' | 'bar' | 'candlestick'): void {
    this.selectedChartType = type;
    this.setupChart();
  }

  /**
   * Agrega o quita el activo de la lista de favoritos del usuario, optimizando la
   * actualización visual y revirtiéndola si la petición falla.
   * @param symbol Símbolo del activo a marcar/desmarcar como favorito.
   */
  toggleFavorite(symbol: string): void {
    if (!symbol || this.favoriteLoading) return;

    this.favoriteLoading = true;

    const wasFavorite = this.isFavorite;
    const request = wasFavorite ? this.watchlistService.removeFavorite(symbol) : this.watchlistService.addFavorite(symbol);

    request.subscribe({
      next: res => {
        this.favoriteLoading = false;

        if (!res.success) {
          this.snackBarService.error(res.message || this.languageService.instant('MARKET.ERRORS.WATCHLIST_UPDATE_FAILED'));
          return;
        }

        this.isFavorite = !wasFavorite;

        if (this.isFavorite)
          this.snackBarService.success(this.languageService.instant('MARKET.WATCHLIST_ADDED', { symbol }));
        else
          this.snackBarService.info(this.languageService.instant('MARKET.WATCHLIST_REMOVED', { symbol }));
      },
      error: err => {
        this.favoriteLoading = false;
        console.error('Error actualizando favorito', err);
        this.snackBarService.error(this.languageService.instant('MARKET.ERRORS.WATCHLIST_UPDATE_FAILED'));
      }
    });
  }

  /**
   * Abre el modal de compra para el activo indicado (o el seleccionado actualmente)
   * y, si la operación se confirma, ejecuta la compra.
   * @param symbol Símbolo del activo a comprar. Si no se especifica, se usa `selectedSymbol`.
   */
  buyAsset(symbol?: string): void {
    const finalSymbol = symbol || this.selectedSymbol;

    if (!finalSymbol) {
      this.snackBarService.error(this.languageService.instant('MARKET.ERRORS.SELECT_ASSET_FIRST'));
      return;
    }

    const dialogRef = this.dialog.open(PortfolioModal, {
      width: '560px',
      maxWidth: '95vw',
      autoFocus: false,
      restoreFocus: false,
      backdropClass: 'blur-backdrop',
      panelClass: 'portfolio-dialog-panel',
      data: {
        mode: 'buy',
        symbol: finalSymbol
      }
    });

    dialogRef.afterClosed().subscribe((result?: PortfolioModalResult) => {
      if (!result || result.mode !== 'buy') return;
      this.onBuyComplete(result.data);
    });
  }

  /**
   * Abre el modal de venta para el activo indicado (o el seleccionado actualmente)
   * y, si la operación se confirma, ejecuta la venta de la posición.
   * @param symbol Símbolo del activo a vender. Si no se especifica, se usa `selectedSymbol`.
   */
  sellAsset(symbol?: string): void {
    const finalSymbol = symbol || this.selectedSymbol;

    if (!finalSymbol) {
      this.snackBarService.error(this.languageService.instant('MARKET.ERRORS.SELECT_ASSET_FIRST'));
      return;
    }

    const dialogRef = this.dialog.open(PortfolioModal, {
      width: '560px',
      maxWidth: '95vw',
      autoFocus: false,
      restoreFocus: false,
      backdropClass: 'blur-backdrop',
      panelClass: 'portfolio-dialog-panel',
      data: {
        mode: 'sell',
        symbol: finalSymbol
      }
    });

    dialogRef.afterClosed().subscribe((result?: PortfolioModalResult) => {
      if (!result || result.mode !== 'sell') return;
      this.sellPosition(result.data);
    });
  }

  /**
   * Confirma la compra del activo a través del servicio de portfolio y notifica
   * el resultado al usuario mediante el snackbar.
   * @param data Datos de la compra confirmados en el modal (símbolo y cantidad).
   */
  onBuyComplete(data: BuyData): void {
    this.portfolioService.buyAsset(data.ticker, data.quantity).subscribe({
      next: response => {
        this.snackBarService.fromResponse(
          response.success,
          response.code,
          response.message || this.languageService.instant(response.success ? 'MARKET.BUY_SUCCESS' : 'MARKET.ERRORS.BUY_ERROR')
        );
      },
      error: error => {
        console.error('Error al realizar la compra', error);
        this.snackBarService.fromResponse(false, error?.error?.code, error?.error?.message || this.languageService.instant('MARKET.ERRORS.BUY_ERROR'));
      }
    });
  }

  /**
   * Confirma la venta de la posición a través del servicio de portfolio y notifica
   * el resultado al usuario mediante el snackbar.
   * @param data Datos de la venta confirmados en el modal (símbolo y cantidad).
   */
  sellPosition(data: SellData): void {
    this.portfolioService.sell(data).subscribe({
      next: response => {
        this.snackBarService.fromResponse(
          response.success,
          response.code,
          response.message || this.languageService.instant(response.success ? 'MARKET.SELL_SUCCESS' : 'MARKET.ERRORS.SELL_ERROR')
        );
      },
      error: error => {
        console.error('Error al vender activo', error);
        this.snackBarService.fromResponse(false, error?.error?.code, error?.error?.message || this.languageService.instant('MARKET.ERRORS.SELL_ERROR'));
      }
    });
  }

  /**
   * Carga de manera diferida (lazy) el formulario de creación de alertas y lo abre
   * en un modal para el activo indicado (o el seleccionado actualmente).
   * @param symbol Símbolo del activo para el cual crear la alerta. Si no se especifica, se usa `selectedSymbol`.
   */
  async openCreateAlert(symbol?: string): Promise<void> {
    const finalSymbol = symbol || this.selectedSymbol;

    if (!finalSymbol) {
      this.snackBarService.error(this.languageService.instant('MARKET.ERRORS.SELECT_ASSET_FIRST'));
      return;
    }

    try {
      const module = await import('../../../alerts/components/create-alert/create-alert');
      const ModalComponent = module.CreateAlertComponent;

      const dialogRef = this.dialog.open(ModalComponent, {
        width: '600px',
        backdropClass: 'blur-backdrop',
        data: {
          isEditing: false,
          alert: { symbol: finalSymbol }
        }
      });

      const result = await dialogRef.afterClosed().toPromise();

      if (result)
        this.snackBarService.success(this.languageService.instant('MARKET.ALERT_CREATED'));
    }
    catch (error) {
      console.error('Error al abrir el modal de crear alerta', error);
      this.snackBarService.error(this.languageService.instant('MARKET.ERRORS.ALERT_FORM_FAILED'));
    }
  }

  /** Avanza a la siguiente noticia del carrusel de "Market Intelligence". */
  nextNews(): void {
    if (this.marketNews.length === 0) return;
    this.activeNewsIndex = (this.activeNewsIndex + 1) % this.marketNews.length;
  }

  /** Retrocede a la noticia anterior del carrusel de "Market Intelligence". */
  previousNews(): void {
    if (this.marketNews.length === 0) return;
    this.activeNewsIndex = this.activeNewsIndex === 0 ? this.marketNews.length - 1 : this.activeNewsIndex - 1;
  }

  /**
   * Abre la URL de la noticia en una nueva pestaña.
   * @param url Dirección de la noticia a abrir.
   */
  openNews(url: string): void {
    if (!url) return;
    window.open(url, '_blank');
  }

  // ── Helpers de presentación ──

  /**
   * Texto explicativo del índice indicado, usado en los tooltips informativos.
   * @param name Nombre del índice (por ejemplo `'S&P 500'`).
   * @returns Descripción del índice o un texto genérico si no hay uno específico.
   */
  getIndexTooltip(name: string): string {
    return this.indexTooltips[name] ?? 'MARKET.TOOLTIP_INDEX_DEFAULT';
  }

  /**
   * Variación absoluta de un índice respecto al cierre anterior.
   * @param index Índice del cual calcular la variación.
   * @returns La variación informada por el servicio o, en su defecto, la diferencia entre el valor actual y el cierre previo.
   */
  getIndexChange(index: MarketIndex): number {
    return index.change ?? ((index.value ?? 0) - (index.previousClose ?? 0));
  }

  /** @returns El porcentaje de variación del activo seleccionado, o `0` si no hay uno. */
  getAssetChangePercent(): number {
    return this.selectedAsset?.changePercent ?? 0;
  }

  /**
   * Porcentaje de variación de un activo de una lista (tendencias, ganadores o perdedores).
   * @param stock Activo del cual obtener el porcentaje de variación.
   */
  getStockChangePercent(stock: MarketMover): number {
    return stock.changePercent ?? 0;
  }

  /**
   * Da formato a un número con dos decimales, en notación inglesa (con coma como separador de miles).
   * @param value Valor numérico a formatear.
   */
  formatNumber(value: number): string {
    return value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  /**
   * Da formato a una capitalización de mercado, abreviando en billones (T), miles de
   * millones (B) o millones (M) según corresponda.
   * @param value Capitalización de mercado en dólares.
   */
  formatMarketCap(value: number): string {
    if (value >= 1000000000000) return `$${(value / 1000000000000).toFixed(2)}T`;
    if (value >= 1000000000) return `$${(value / 1000000000).toFixed(1)}B`;
    if (value >= 1000000) return `$${(value / 1000000).toFixed(1)}M`;
    return `$${value.toLocaleString()}`;
  }

  /**
   * Da formato a un volumen de operaciones, abreviando en millones (M) o miles (K) según corresponda.
   * @param value Cantidad de acciones negociadas.
   */
  formatVolume(value: number): string {
    if (value >= 1000000) return `${(value / 1000000).toFixed(1)}M`;
    if (value >= 1000) return `${(value / 1000).toFixed(1)}K`;
    return value.toString();
  }

  /**
   * Da formato a un valor monetario que puede no estar disponible.
   * @param value Valor a formatear, o `null`/`undefined` si no hay dato.
   * @returns El valor con el símbolo `$` y dos decimales, o `'--'` si no hay dato.
   */
  formatNullableCurrency(value: number | null | undefined): string {
    return value == null ? '--' : `$${value.toFixed(2)}`;
  }

  /**
   * Da formato a un valor numérico que puede no estar disponible.
   * @param value Valor a formatear, o `null`/`undefined` si no hay dato.
   * @returns El valor como texto, o `'--'` si no hay dato.
   */
  formatNullableNumber(value: number | null | undefined): string {
    return value == null ? '--' : value.toString();
  }

  /**
   * Da formato a un porcentaje que puede no estar disponible.
   * @param value Valor a formatear, o `null`/`undefined` si no hay dato.
   * @returns El valor con dos decimales seguido de `%`, o `'--'` si no hay dato.
   */
  formatNullablePercent(value: number | null | undefined): string {
    return value == null ? '--' : `${value.toFixed(2)}%`;
  }

  // ── Gráfico (privados) ──

  /**
   * Da formato a la fecha de un punto del histórico según el rango de tiempo seleccionado,
   * para usarla como etiqueta del eje horizontal del gráfico.
   * @param date Fecha del punto, en formato ISO.
   * @returns La fecha formateada (hora, día de la semana, día y mes, o mes y año, según el rango).
   */
  private formatChartLabel(date: string): string {
    const value = new Date(date);

    if (this.selectedTimeframe === '1d')
      return value.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });

    if (this.selectedTimeframe === '1w')
      return value.toLocaleDateString('es-AR', { weekday: 'short' });

    if (this.selectedTimeframe === '1m' || this.selectedTimeframe === '3m')
      return value.toLocaleDateString('es-AR', { day: '2-digit', month: 'short' });

    return value.toLocaleDateString('es-AR', { month: 'short', year: '2-digit' });
  }

  /**
   * Alinea los puntos de una serie comparativa con las etiquetas del activo principal,
   * para que ambas series queden sincronizadas en el mismo eje horizontal.
   * @param points Puntos del histórico de la serie comparativa.
   * @param labels Etiquetas del eje horizontal generadas a partir del activo principal.
   * @returns Un arreglo con el precio de cierre correspondiente a cada etiqueta (o `null` si no hay dato para esa fecha).
   */
  private alignSeriesToLabels(points: MarketHistoryPoint[], labels: string[]): number[] {
    const map = new Map(points.map(x => [this.formatChartLabel(x.date), x.close]));
    return labels.map(label => map.get(label) ?? null) as number[];
  }

  /**
   * Color de línea/barra asignado a una serie comparativa según su símbolo.
   * @param symbol Símbolo de la serie (por ejemplo `'^GSPC'` para el S&P 500).
   */
  private getSeriesColor(symbol: string): string {
    return symbol === '^GSPC' ? '#10b981' : symbol === '^IXIC' ? '#f59e0b' : '#a78bfa';
  }

  /**
   * Color de relleno (con transparencia) asignado a una serie comparativa según su símbolo,
   * usado en el gráfico de tipo línea.
   * @param symbol Símbolo de la serie (por ejemplo `'^GSPC'` para el S&P 500).
   */
  private getSeriesBackgroundColor(symbol: string): string {
    return symbol === '^GSPC' ? 'rgba(16, 185, 129, 0.10)' : symbol === '^IXIC' ? 'rgba(245, 158, 11, 0.10)' : 'rgba(167, 139, 250, 0.10)';
  }

  /**
   * Construye los datos (etiquetas y datasets) del gráfico comparativo según el tipo
   * de gráfico seleccionado: para velas delega en `generateCandlestickChartData`, y
   * para línea/barras arma el dataset del activo principal junto con uno por cada
   * serie del histórico comparativo.
   * @returns Objeto `{ labels, datasets }` listo para pasarle a Chart.js, o `{ datasets }` en el caso de velas.
   */
  private generateMarketChartData(): any {
    if (this.selectedChartType === 'candlestick') return this.generateCandlestickChartData();

    const labels = this.assetHistory?.series.points.map(x => this.formatChartLabel(x.date)) ?? [];

    const datasets: any[] = [
      {
        label: this.assetHistory?.series.symbol ?? this.selectedSymbol,
        data: this.assetHistory?.series.points.map(x => x.close) ?? [],
        borderColor: '#4a90e2',
        backgroundColor: this.selectedChartType === 'line' ? 'rgba(74, 144, 226, 0.12)' : '#4a90e2',
        borderWidth: 2,
        tension: 0.4,
        fill: this.selectedChartType === 'line'
      }
    ];

    this.comparisonHistory?.series.forEach(series => {
      datasets.push({
        label: series.name || series.symbol,
        data: this.alignSeriesToLabels(series.points, labels),
        borderColor: this.getSeriesColor(series.symbol),
        backgroundColor: this.selectedChartType === 'line' ? this.getSeriesBackgroundColor(series.symbol) : this.getSeriesColor(series.symbol),
        borderWidth: 2,
        tension: 0.4,
        fill: this.selectedChartType === 'line'
      });
    });

    return { labels, datasets };
  }

  /**
   * Construye el dataset de velas (OHLC) a partir del histórico del activo seleccionado.
   * @returns Objeto `{ datasets }` con un único dataset de velas, listo para pasarle a Chart.js.
   */
  private generateCandlestickChartData(): any {
    const candles = this.assetHistory?.series.points.map(x => ({
      x: new Date(x.date).getTime(),
      o: x.open,
      h: x.high,
      l: x.low,
      c: x.close
    })) ?? [];

    return {
      datasets: [
        {
          label: this.assetHistory?.series.symbol ?? this.selectedSymbol,
          data: candles
        }
      ]
    };
  }

  /**
   * Arma la configuración completa de Chart.js (tipo, datos, escalas, leyenda y tooltips)
   * según el tipo de gráfico seleccionado.
   * @param data Datos generados por `generateMarketChartData`.
   * @returns Configuración lista para instanciar un `Chart` de Chart.js.
   */
  private getMarketChartConfiguration(data: any): any {
    const t = this.themeService.getChartTheme();

    if (this.selectedChartType === 'candlestick') {
      return {
        type: 'candlestick',
        data,
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: {
              display: true,
              position: 'top',
              labels: {
                color: t.legendColor,
                font: { size: 12, weight: '500' },
                usePointStyle: true,
                padding: 20
              }
            },
            tooltip: {
              mode: 'index',
              intersect: false,
              backgroundColor: t.tooltipBg,
              titleColor: t.tooltipText,
              bodyColor: t.tooltipText,
              borderColor: t.tooltipBorderColor,
              borderWidth: 1,
              padding: 12
            }
          },
          scales: {
            x: {
              type: 'time',
              time: { unit: 'day' },
              grid: { color: t.gridColor },
              ticks: { color: t.textColor, font: { size: 11 } }
            },
            y: {
              grid: { color: t.gridColor },
              ticks: {
                color: t.textColor,
                font: { size: 11 },
                callback: (value: any) => `$${Number(value).toFixed(0)}`
              }
            }
          }
        }
      };
    }

    return {
      type: this.selectedChartType,
      data,
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: true,
            position: 'top',
            labels: {
              color: t.legendColor,
              font: { size: 12, weight: '500' },
              usePointStyle: true,
              padding: 20
            }
          },
          tooltip: {
            mode: 'index',
            intersect: false,
            backgroundColor: t.tooltipBg,
            titleColor: t.tooltipText,
            bodyColor: t.tooltipText,
            borderColor: t.tooltipBorderColor,
            borderWidth: 1,
            padding: 12,
            displayColors: true,
            callbacks: {
              label: (context: any) => {
                const label = context.dataset.label ? `${context.dataset.label}: ` : '';
                const value = context.parsed.y;
                const sign = value > 0 ? '+' : '';
                return `${label}${sign}${value.toFixed(2)}%`;
              }
            }
          }
        },
        scales: {
          x: {
            grid: { color: t.gridColor },
            ticks: { color: t.textColor, font: { size: 11 } }
          },
          y: {
            grid: { color: t.gridColor },
            ticks: {
              color: t.textColor,
              font: { size: 11 },
              callback: (value: any) => {
                const number = Number(value);
                const sign = number > 0 ? '+' : '';
                return `${sign}${number}%`;
              }
            }
          }
        },
        interaction: {
          mode: 'index',
          intersect: false
        }
      }
    };
  }

  /**
   * Crea (o recrea) el gráfico comparativo en el `<canvas>` del template, usando los
   * datos y la configuración generados a partir del activo y del histórico actuales.
   * No hace nada si todavía no hay activo/histórico seleccionado o si el `<canvas>` no está disponible.
   */
  private setupChart(): void {
    if (!this.selectedAsset || !this.assetHistory)
      return;

    const canvas = document.getElementById('marketTerminalChart') as HTMLCanvasElement;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    if (this.chart) this.chart.destroy();

    const data = this.generateMarketChartData();
    const config = this.getMarketChartConfiguration(data);

    this.chart = new Chart(ctx, config);
  }

}
