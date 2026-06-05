import { AfterViewInit, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MaterialModule } from '../../../../shared/material.module';
import Chart from 'chart.js/auto';
import { CandlestickController, CandlestickElement, OhlcController, OhlcElement } from 'chartjs-chart-financial';
import 'chartjs-adapter-date-fns';
import { MarketService } from '../../services/market.service';
import {
  MarketIndex,
  MarketStatus,
  MarketAsset
} from '../../models/market.model';

Chart.register(CandlestickController, CandlestickElement, OhlcController, OhlcElement);

interface MarketNews {
  source: string;
  time: string;
  title: string;
  summary: string;
  url: string;
}

@Component({
  selector: 'app-market',
  standalone: true,
  imports: [CommonModule, FormsModule, MaterialModule],
  templateUrl: './market.html',
  styleUrl: './market.css'
})
export class Market implements OnInit, AfterViewInit, OnDestroy {
  selectedSymbol = 'NVDA';
  selectedTimeframe = '1m';
  selectedChartType: 'line' | 'bar' | 'candlestick' = 'line';
  marketData: MarketAsset[] = [];
  trendingStocks: MarketAsset[] = [];
  marketNews: MarketNews[] = [];
  activeNewsIndex = 0;
  currentTime = '';
  private chart: Chart | null = null;
  private clockInterval: any;

  // cards superiores
  marketIndices: MarketIndex[] = [];
  marketStatus: MarketStatus | null = null;
  loadingIndices = false;
  loadingOverview = false;
  emptyIndexCards = [{ name: 'S&P 500' }, { name: 'NASDAQ' }, { name: 'Dow Jones' }];
  emptyStatLabels = ['Open', 'Volume', 'Day High', 'Day Low', 'Avg Vol', 'Mkt Cap', 'P/E Ratio', 'Div Yield'];

  selectedAsset: MarketAsset | null = null;
  loadingAsset = false;
  assetErrorMessage = '';

  constructor(
    private router: Router,
    private marketService: MarketService
  ) { }

  ngOnInit(): void {
    this.loadMarketData();
    this.loadMarketOverview();
    this.loadNews();
    this.updateClock();
    this.selectedAsset = this.marketData.find(x => x.symbol === this.selectedSymbol) ?? this.marketData[0];
    this.trendingStocks = [...this.marketData].sort((a, b) => Math.abs(b.changePercent!) - Math.abs(a.changePercent!)).slice(0, 6);
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
    return this.marketNews[this.activeNewsIndex] ?? this.marketNews[0];
  }

  private loadMarketData(): void {
    this.marketData = [];
    this.trendingStocks = [];
    this.selectedAsset = null;
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
    this.marketNews = [
      { source: 'REUTERS', time: '5m ago', title: 'NVIDIA Blackwell demand grows as AI spending continues to accelerate.', summary: 'Demand for next-generation AI chips remains strong, with major cloud providers increasing infrastructure investment and analysts watching semiconductor leaders closely.', url: 'https://finance.yahoo.com' },
      { source: 'MARKET WATCH', time: '12m ago', title: 'Technology stocks lead the session while investors wait for new inflation data.', summary: 'Large-cap technology names pushed indexes higher as traders balanced earnings expectations with upcoming macroeconomic indicators.', url: 'https://finance.yahoo.com' },
      { source: 'INVESTLAB', time: '18m ago', title: 'Market sentiment remains positive across growth stocks.', summary: 'Momentum continues in selected growth names, although volatility remains elevated in high-beta assets.', url: 'https://finance.yahoo.com' }
    ];
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
    const canvas = document.getElementById('marketTerminalChart') as HTMLCanvasElement;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    if (this.chart) this.chart.destroy();

    const data = this.generateMarketChartData();
    const config = this.getMarketChartConfiguration(data);

    this.chart = new Chart(ctx, config);
  }

  private getChartData(): { labels: string[]; asset: number[]; sp500: number[] } {
    const base = this.selectedAsset!.price;
    if (this.selectedTimeframe === '1d') return { labels: ['09:30', '10:00', '10:30', '11:00', '11:30', '12:00', '12:30', '13:00', '13:30', '14:00', '14:30', '15:00'], asset: [base - 22, base - 18, base - 25, base - 10, base - 6, base - 8, base + 4, base + 1, base + 2, base + 14, base + 20, base + 27], sp500: [5200, 5204, 5198, 5205, 5208, 5210, 5213, 5216, 5220, 5225, 5230, 5241] };
    if (this.selectedTimeframe === '1w') return { labels: ['Lun', 'Mar', 'Mie', 'Jue', 'Vie'], asset: [base - 45, base - 20, base - 30, base + 10, base + 24], sp500: [5160, 5180, 5172, 5210, 5241] };
    if (this.selectedTimeframe === '3m') return {
      labels: ['Abr 01', 'Abr 15', 'May 01', 'May 15', 'Jun 01', 'Jun 15', 'Jun 30'],
      asset: [base - 105, base - 82, base - 64, base - 38, base - 20, base + 12, base + 35],
      sp500: [4980, 5030, 5080, 5110, 5160, 5205, 5241]
    };

    if (this.selectedTimeframe === '6m') return {
      labels: ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun'],
      asset: [base - 155, base - 132, base - 118, base - 86, base - 44, base + 20],
      sp500: [4750, 4860, 4940, 5030, 5140, 5241]
    };
    if (this.selectedTimeframe === '1y') return { labels: ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'], asset: [base - 180, base - 150, base - 165, base - 110, base - 90, base - 60, base - 20, base + 10, base - 5, base + 35, base + 70, base + 105], sp500: [4600, 4680, 4725, 4800, 4890, 4950, 5030, 5100, 5060, 5150, 5200, 5241] };
    return { labels: ['01 Jun', '05 Jun', '09 Jun', '13 Jun', '17 Jun', '21 Jun', '25 Jun', '29 Jun'], asset: [base - 70, base - 80, base - 35, base - 20, base - 28, base + 5, base + 35, base + 62], sp500: [5120, 5130, 5110, 5150, 5168, 5190, 5210, 5241] };
  }

  selectAsset(symbol: string): void {
    this.selectedSymbol = symbol;
    this.searchAsset();
  }

  changeTimeframe(timeframe: string): void {
    this.selectedTimeframe = timeframe;
    this.setupChart();
  }

  nextNews(): void {
    this.activeNewsIndex = this.activeNewsIndex === this.marketNews.length - 1 ? 0 : this.activeNewsIndex + 1;
  }

  previousNews(): void {
    this.activeNewsIndex = this.activeNewsIndex === 0 ? this.marketNews.length - 1 : this.activeNewsIndex - 1;
  }

  addToWatchlist(symbol: string): void {
    console.log('Agregar a favoritos:', symbol);
  }

  createAlert(symbol: string): void {
    console.log('Crear alerta:', symbol);
  }

  buyAsset(symbol: string): void {
    this.router.navigate(['/portfolio'], { queryParams: { action: 'buy', symbol } });
  }

  sellAsset(symbol: string): void {
    this.router.navigate(['/portfolio'], { queryParams: { action: 'sell', symbol } });
  }

  openNews(url: string): void {
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
      return;
    }

    this.loadingAsset = true;
    this.assetErrorMessage = '';

    this.marketService.getAssetDetail(symbol).subscribe({
      next: response => {
        this.loadingAsset = false;

        if (!response.success || !response.data) {
          this.selectedAsset = null;
          this.assetErrorMessage = response.message || 'No se encontró información para el activo';
          return;
        }

        this.selectedAsset = response.data;
        this.selectedSymbol = response.data.symbol;
        this.assetErrorMessage = '';
        this.setupChart();
      },
      error: error => {
        console.error('Error al obtener detalle del activo', error);
        this.loadingAsset = false;
        this.selectedAsset = null;
        this.assetErrorMessage = 'No se pudo obtener la información del activo';
      }
    });
  }

  changeChartType(type: 'line' | 'bar' | 'candlestick'): void {
    this.selectedChartType = type;
    this.setupChart();
  }

  private generateMarketChartData(): any {
    if (this.selectedChartType === 'candlestick') return this.generateCandlestickChartData();

    const points = this.getMarketPerformancePoints();
    const labels = points.map(x => x.label);

    const datasets = [
      {
        label: this.selectedAsset!.symbol,
        data: points.map(x => x.asset),
        borderColor: '#4a90e2',
        backgroundColor: this.selectedChartType === 'line' ? 'rgba(74, 144, 226, 0.12)' : '#4a90e2',
        borderWidth: 2,
        tension: 0.4,
        fill: this.selectedChartType === 'line'
      },
      {
        label: 'S&P 500',
        data: points.map(x => x.sp500),
        borderColor: '#10b981',
        backgroundColor: this.selectedChartType === 'line' ? 'rgba(16, 185, 129, 0.10)' : '#10b981',
        borderWidth: 2,
        tension: 0.4,
        fill: this.selectedChartType === 'line'
      },
      {
        label: 'NASDAQ',
        data: points.map(x => x.nasdaq),
        borderColor: '#f59e0b',
        backgroundColor: this.selectedChartType === 'line' ? 'rgba(245, 158, 11, 0.10)' : '#f59e0b',
        borderWidth: 2,
        tension: 0.4,
        fill: this.selectedChartType === 'line'
      },
      {
        label: 'Dow Jones',
        data: points.map(x => x.dowjones),
        borderColor: '#a78bfa',
        backgroundColor: this.selectedChartType === 'line' ? 'rgba(167, 139, 250, 0.10)' : '#a78bfa',
        borderWidth: 2,
        tension: 0.4,
        fill: this.selectedChartType === 'line'
      }
    ];

    return { labels, datasets };
  }

  private getMarketPerformancePoints(): any[] {
    const multiplier = this.selectedAsset!.changePercent! >= 0 ? 1 : -1;

    if (this.selectedTimeframe === '1d') {
      return [
        { label: '09:30', asset: 0, sp500: 0.10, nasdaq: -0.05, dowjones: 0.02 },
        { label: '10:00', asset: 0.25 * multiplier, sp500: 0.18, nasdaq: 0.08, dowjones: 0.10 },
        { label: '10:30', asset: 0.55 * multiplier, sp500: 0.22, nasdaq: 0.15, dowjones: 0.18 },
        { label: '11:00', asset: 0.90 * multiplier, sp500: 0.28, nasdaq: 0.22, dowjones: 0.21 },
        { label: '11:30', asset: 1.20 * multiplier, sp500: 0.34, nasdaq: 0.30, dowjones: 0.25 },
        { label: '12:00', asset: 1.55 * multiplier, sp500: 0.39, nasdaq: 0.38, dowjones: 0.28 },
        { label: '12:30', asset: 1.83 * multiplier, sp500: 0.45, nasdaq: 0.42, dowjones: 0.31 }
      ];
    }

    if (this.selectedTimeframe === '1w') {
      return [
        { label: 'Lun', asset: 0, sp500: 0.4, nasdaq: 0.6, dowjones: 0.2 },
        { label: 'Mar', asset: 1.4 * multiplier, sp500: 0.9, nasdaq: 1.1, dowjones: 0.5 },
        { label: 'Mie', asset: 2.1 * multiplier, sp500: 1.2, nasdaq: 1.7, dowjones: 0.9 },
        { label: 'Jue', asset: 3.4 * multiplier, sp500: 1.6, nasdaq: 2.0, dowjones: 1.1 },
        { label: 'Vie', asset: 4.2 * multiplier, sp500: 2.0, nasdaq: 2.5, dowjones: 1.4 }
      ];
    }

    if (this.selectedTimeframe === '3m') {
      return [
        { label: 'Abr', asset: 0, sp500: 1.1, nasdaq: 1.5, dowjones: 0.8 },
        { label: 'Abr 15', asset: 3.2 * multiplier, sp500: 1.8, nasdaq: 2.3, dowjones: 1.2 },
        { label: 'May', asset: 5.9 * multiplier, sp500: 2.6, nasdaq: 3.1, dowjones: 1.8 },
        { label: 'May 15', asset: 8.5 * multiplier, sp500: 3.2, nasdaq: 4.2, dowjones: 2.4 },
        { label: 'Jun', asset: 11.2 * multiplier, sp500: 4.0, nasdaq: 5.6, dowjones: 3.1 },
        { label: 'Jun 15', asset: 13.1 * multiplier, sp500: 4.5, nasdaq: 6.3, dowjones: 3.6 }
      ];
    }

    if (this.selectedTimeframe === '6m') {
      return [
        { label: 'Ene', asset: 0, sp500: 1.2, nasdaq: 2.0, dowjones: 0.9 },
        { label: 'Feb', asset: 4.5 * multiplier, sp500: 2.1, nasdaq: 3.8, dowjones: 1.6 },
        { label: 'Mar', asset: 6.8 * multiplier, sp500: 3.3, nasdaq: 5.1, dowjones: 2.7 },
        { label: 'Abr', asset: 10.4 * multiplier, sp500: 4.6, nasdaq: 6.8, dowjones: 3.4 },
        { label: 'May', asset: 14.8 * multiplier, sp500: 5.2, nasdaq: 7.6, dowjones: 4.2 },
        { label: 'Jun', asset: 18.5 * multiplier, sp500: 6.1, nasdaq: 8.4, dowjones: 5.0 }
      ];
    }

    if (this.selectedTimeframe === '1y') {
      return [
        { label: 'Ene', asset: 0, sp500: 2.0, nasdaq: 3.5, dowjones: 1.4 },
        { label: 'Feb', asset: 5.0 * multiplier, sp500: 3.1, nasdaq: 4.6, dowjones: 2.0 },
        { label: 'Mar', asset: 8.5 * multiplier, sp500: 4.2, nasdaq: 6.2, dowjones: 3.1 },
        { label: 'Abr', asset: 14.0 * multiplier, sp500: 6.0, nasdaq: 8.4, dowjones: 4.2 },
        { label: 'May', asset: 20.5 * multiplier, sp500: 7.1, nasdaq: 10.2, dowjones: 5.3 },
        { label: 'Jun', asset: 26.0 * multiplier, sp500: 8.0, nasdaq: 12.0, dowjones: 6.1 },
        { label: 'Jul', asset: 31.2 * multiplier, sp500: 9.2, nasdaq: 14.1, dowjones: 7.0 },
        { label: 'Ago', asset: 34.7 * multiplier, sp500: 10.1, nasdaq: 15.8, dowjones: 7.6 }
      ];
    }

    return [
      { label: '28/05', asset: 0, sp500: 4.2, nasdaq: 6.3, dowjones: 3.8 },
      { label: '29/05', asset: 2.6 * multiplier, sp500: 4.4, nasdaq: 6.5, dowjones: 3.9 },
      { label: '01/06', asset: 10.0 * multiplier, sp500: 4.7, nasdaq: 7.0, dowjones: 4.1 },
      { label: '02/06', asset: 12.6 * multiplier, sp500: 4.8, nasdaq: 7.0, dowjones: 4.2 },
      { label: '03/06', asset: 14.3 * multiplier, sp500: 4.0, nasdaq: 6.0, dowjones: 3.7 }
    ];
  }

  private generateCandlestickChartData(): any {
    const base = this.selectedAsset!.price;
    const candles = [
      { x: new Date('2026-06-01').getTime(), o: base - 32, h: base - 10, l: base - 42, c: base - 18 },
      { x: new Date('2026-06-02').getTime(), o: base - 18, h: base + 4, l: base - 28, c: base - 6 },
      { x: new Date('2026-06-03').getTime(), o: base - 6, h: base + 18, l: base - 12, c: base + 12 },
      { x: new Date('2026-06-04').getTime(), o: base + 12, h: base + 30, l: base + 2, c: base + 22 },
      { x: new Date('2026-06-05').getTime(), o: base + 22, h: base + 35, l: base + 10, c: base + 18 }
    ];

    return {
      datasets: [
        {
          label: this.selectedAsset!.symbol,
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
  
  getStockChangePercent(stock: MarketAsset): number {
    return stock.changePercent ?? 0;
  }
}