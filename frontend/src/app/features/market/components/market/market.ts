import { AfterViewInit, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MaterialModule } from '../../../../shared/material.module';
import Chart from 'chart.js/auto';
import { CandlestickController, CandlestickElement, OhlcController, OhlcElement } from 'chartjs-chart-financial';
import 'chartjs-adapter-date-fns';
import { MarketService } from '../../services/market.service';
import { WatchlistService } from '../../../watchlist/services/watchlist.service';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { MatDialog } from '@angular/material/dialog';
import { PortfolioModal } from '../../../portfolio/components/portfolio-modal/portfolio-modal';
import { PortfolioService } from '../../../portfolio/services/portfolio.service';
import { BuyData, SellData, PortfolioModalResult } from '../../../portfolio/models/portfolio.modal.model';

import {
  MarketIndex,
  MarketStatus,
  MarketAsset,
  MarketMover,
  MarketNews,
  MarketAssetHistory,
  MarketComparisonHistory,
  MarketHistoryPoint,
  MarketHistorySeries
} from '../../models/market.model';

Chart.register(CandlestickController, CandlestickElement, OhlcController, OhlcElement);

@Component({
  selector: 'app-market',
  standalone: true,
  imports: [CommonModule, FormsModule, MaterialModule],
  templateUrl: './market.html',
  styleUrl: './market.css'
})
export class Market implements OnInit, AfterViewInit, OnDestroy {

  // grafico
  selectedSymbol = localStorage.getItem('lastMarketSymbol') || 'AAPL';
  selectedTimeframe = '1m';
  selectedChartType: 'line' | 'bar' | 'candlestick' = 'line';
  marketData: MarketAsset[] = [];
  currentTime = '';
  assetHistory: MarketAssetHistory | null = null;
  comparisonHistory: MarketComparisonHistory | null = null;
  loadingChart = false;
  lastComparisonRange = '';
  private chart: Chart | null = null;
  private clockInterval: any;

  // cards superiores
  marketIndices: MarketIndex[] = [];
  marketStatus: MarketStatus | null = null;
  loadingIndices = false;
  loadingOverview = false;
  emptyIndexCards = [{ name: 'S&P 500' }, { name: 'NASDAQ' }, { name: 'Dow Jones' }];
  emptyStatLabels = ['Open', 'Volume', 'Day High', 'Day Low', 'Avg Vol', 'Mkt Cap', 'P/E Ratio', 'Div Yield'];
  hasPositionForSelectedAsset = false;
  loadingPositionStatus = false;

  // opciones card
  isFavorite = false;
  favoriteLoading = false;

  selectedAsset: MarketAsset | null = null;
  loadingAsset = false;
  assetErrorMessage = '';

  // cards inferiores
  loadingTrending = false;
  loadingGainers = false;
  loadingLosers = false;
  trendingStocks: MarketMover[] = [];
  dayGainers: MarketMover[] = [];
  dayLosers: MarketMover[] = [];

  activeNewsIndex = 0;
  loadingNews = false;
  marketNews: MarketNews[] = [];

  constructor(
    private router: Router,
    private marketService: MarketService,
    private watchlistService: WatchlistService,
    private portfolioService: PortfolioService,
    private snackBarService: SnackBarService,
    private dialog: MatDialog
  ) { }

  ngOnInit(): void {
    this.loadMarketData();
    this.loadMarketOverview();
    this.loadMarketLists();
    this.loadNews();
    this.updateClock();
    this.loadComparisonHistory();
    this.searchAsset();

    this.clockInterval = setInterval(() => this.updateClock(), 1000);
  }

  ngAfterViewInit(): void {
    setTimeout(() => this.setupChart(), 0);
  }

  ngOnDestroy(): void {
    if (this.chart) this.chart.destroy();
    if (this.clockInterval) clearInterval(this.clockInterval);
  }

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

  private loadMarketLists(): void {
    this.loadTrending();
    this.loadGainers();
    this.loadLosers();
  }

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

  private loadMarketData(): void {
    this.marketData = [];
    this.trendingStocks = [];
    this.selectedAsset = null;
    this.hasPositionForSelectedAsset = false;
  }

  private loadMarketOverview(): void {
    this.loadingOverview = true;
    this.loadingIndices = true;

    this.marketService.getMarketOverview().subscribe({
      next: response => {
        if (!response.success || !response.data) {
          this.marketStatus = null;
          this.marketIndices = [];
          this.loadingOverview = false;
          this.loadingIndices = false;
          return;
        }

        this.marketStatus = response.data.marketStatus;
        this.marketIndices = response.data.indices ?? [];
        this.loadingOverview = false;
        this.loadingIndices = false;
      },
      error: error => {
        console.error('Error al obtener panorama de mercado', error);
        this.marketStatus = null;
        this.marketIndices = [];
        this.loadingOverview = false;
        this.loadingIndices = false;
      }
    });
  }

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

  buyAsset(symbol?: string): void {
    const finalSymbol = symbol || this.selectedSymbol;

    if (!finalSymbol) {
      this.snackBarService.error('Primero seleccioná un activo');
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

  sellAsset(symbol?: string): void {
    const finalSymbol = symbol || this.selectedSymbol;

    if (!finalSymbol) {
      this.snackBarService.error('Primero seleccioná un activo');
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

  onBuyComplete(data: BuyData): void {
    this.portfolioService.buyAsset(data.ticker, data.quantity).subscribe({
      next: response => {
        if (!response.success) {
          this.snackBarService.info(response.message || 'No se pudo realizar la compra');
          return;
        }

        this.snackBarService.success(response.message || 'Compra realizada correctamente');
      },
      error: error => {
        console.error('Error al realizar la compra', error);
        this.snackBarService.error(error?.error?.message || 'Error al realizar la compra');
      }
    });
  }

  sellPosition(data: SellData): void {
    this.portfolioService.sell(data).subscribe({
      next: response => {
        if (!response.success) {
          this.snackBarService.info(response.message || 'No se pudo realizar la venta');
          return;
        }

        this.snackBarService.success(response.message || 'Venta realizada correctamente');
      },
      error: error => {
        console.error('Error al vender activo', error);
        this.snackBarService.error(error?.error?.message || 'Error al vender activo');
      }
    });
  }

  toggleFavorite(symbol: string): void {
    if (!symbol || this.favoriteLoading) return;

    this.favoriteLoading = true;

    const wasFavorite = this.isFavorite;
    const request = wasFavorite ? this.watchlistService.removeFavorite(symbol) : this.watchlistService.addFavorite(symbol);

    request.subscribe({
      next: res => {
        this.favoriteLoading = false;

        if (!res.success) {
          this.snackBarService.error(res.message || 'No se pudo actualizar favoritos');
          return;
        }

        this.isFavorite = !wasFavorite;

        if (this.isFavorite)
          this.snackBarService.success(`${symbol} agregado a favoritos`);
        else
          this.snackBarService.info(`${symbol} eliminado de favoritos`);
      },
      error: err => {
        this.favoriteLoading = false;
        console.error('Error actualizando favorito', err);
        this.snackBarService.error('No se pudo actualizar favoritos');
      }
    });
  }

  private getMarketChartConfiguration(data: any): any {
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
                color: '#ffffff',
                font: { size: 12, weight: '500' },
                usePointStyle: true,
                padding: 20
              }
            },
            tooltip: {
              mode: 'index',
              intersect: false,
              backgroundColor: 'rgba(0, 0, 0, 0.8)',
              titleColor: '#ffffff',
              bodyColor: '#ffffff',
              borderColor: '#4a90e2',
              borderWidth: 1,
              padding: 12
            }
          },
          scales: {
            x: {
              type: 'time',
              time: { unit: 'day' },
              grid: { color: 'rgba(255, 255, 255, 0.1)' },
              ticks: { color: 'rgba(255, 255, 255, 0.7)', font: { size: 11 } }
            },
            y: {
              grid: { color: 'rgba(255, 255, 255, 0.1)' },
              ticks: {
                color: 'rgba(255, 255, 255, 0.7)',
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
              color: '#ffffff',
              font: { size: 12, weight: '500' },
              usePointStyle: true,
              padding: 20
            }
          },
          tooltip: {
            mode: 'index',
            intersect: false,
            backgroundColor: 'rgba(0, 0, 0, 0.8)',
            titleColor: '#ffffff',
            bodyColor: '#ffffff',
            borderColor: '#4a90e2',
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
            grid: { color: 'rgba(255, 255, 255, 0.1)' },
            ticks: { color: 'rgba(255, 255, 255, 0.7)', font: { size: 11 } }
          },
          y: {
            grid: { color: 'rgba(255, 255, 255, 0.1)' },
            ticks: {
              color: 'rgba(255, 255, 255, 0.7)',
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

  selectAsset(symbol: string): void {
    const normalizedSymbol = symbol.trim().toUpperCase();

    if (!normalizedSymbol || normalizedSymbol === this.selectedSymbol)
      return;

    this.selectedSymbol = normalizedSymbol;
    this.searchAsset();
  }

  changeTimeframe(timeframe: string): void {
    if (timeframe === this.selectedTimeframe) return;

    this.selectedTimeframe = timeframe;
    this.loadComparisonHistory();
    this.loadAssetHistory();
  }

  nextNews(): void {
    if (this.marketNews.length === 0) return;
    this.activeNewsIndex = (this.activeNewsIndex + 1) % this.marketNews.length;
  }

  previousNews(): void {
    if (this.marketNews.length === 0) return;
    this.activeNewsIndex = this.activeNewsIndex === 0 ? this.marketNews.length - 1 : this.activeNewsIndex - 1;
  }

  openNews(url: string): void {
    if (!url) return;
    window.open(url, '_blank');
  }

  formatNumber(value: number): string {
    return value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  formatMarketCap(value: number): string {
    if (value >= 1000000000000) return `$${(value / 1000000000000).toFixed(2)}T`;
    if (value >= 1000000000) return `$${(value / 1000000000).toFixed(1)}B`;
    if (value >= 1000000) return `$${(value / 1000000).toFixed(1)}M`;
    return `$${value.toLocaleString()}`;
  }

  formatVolume(value: number): string {
    if (value >= 1000000) return `${(value / 1000000).toFixed(1)}M`;
    if (value >= 1000) return `${(value / 1000).toFixed(1)}K`;
    return value.toString();
  }

  private updateClock(): void {
    this.currentTime = new Date().toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  }

  searchAsset(): void {
    const symbol = this.selectedSymbol.trim().toUpperCase();

    if (!symbol) {
      this.assetErrorMessage = 'Ingresá un símbolo para buscar';
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
          this.assetErrorMessage = response.message || 'No se encontró información para el activo';
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
        this.assetErrorMessage = 'No se pudo obtener la información del activo';
      }
    });
  }

  changeChartType(type: 'line' | 'bar' | 'candlestick'): void {
    this.selectedChartType = type;
    this.setupChart();
  }
  async openCreateAlert(symbol?: string): Promise<void> {
    const finalSymbol = symbol || this.selectedSymbol;

    if (!finalSymbol) {
      this.snackBarService.error('Primero seleccioná un activo');
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
        this.snackBarService.success('Alerta creada correctamente');
    }
    catch (error) {
      console.error('Error al abrir el modal de crear alerta', error);
      this.snackBarService.error('No se pudo abrir el formulario de alerta');
    }
  }

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

  getMarketStatusText(): string {
    return this.marketStatus?.statusText ?? 'Estado no disponible';
  }

  getMarketStatusTime(): string {
    return this.marketStatus?.marketTime ?? this.currentTime;
  }

  isMarketOpen(): boolean {
    return this.marketStatus?.isOpen ?? false;
  }

  getIndexChange(index: MarketIndex): number {
    return index.change ?? ((index.value ?? 0) - (index.previousClose ?? 0));
  }

  formatNullableCurrency(value: number | null | undefined): string {
    return value == null ? '--' : `$${value.toFixed(2)}`;
  }

  formatNullableNumber(value: number | null | undefined): string {
    return value == null ? '--' : value.toString();
  }

  formatNullablePercent(value: number | null | undefined): string {
    return value == null ? '--' : `${value.toFixed(2)}%`;
  }

  getAssetChangePercent(): number {
    return this.selectedAsset?.changePercent ?? 0;
  }

  getStockChangePercent(stock: MarketMover): number {
    return stock.changePercent ?? 0;
  }

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

  private alignSeriesToLabels(points: MarketHistoryPoint[], labels: string[]): number[] {
    const map = new Map(points.map(x => [this.formatChartLabel(x.date), x.close]));
    return labels.map(label => map.get(label) ?? null) as number[];
  }

  private getSeriesColor(symbol: string): string {
    return symbol === '^GSPC' ? '#10b981' : symbol === '^IXIC' ? '#f59e0b' : '#a78bfa';
  }

  private getSeriesBackgroundColor(symbol: string): string {
    return symbol === '^GSPC' ? 'rgba(16, 185, 129, 0.10)' : symbol === '^IXIC' ? 'rgba(245, 158, 11, 0.10)' : 'rgba(167, 139, 250, 0.10)';
  }

}