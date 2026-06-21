import { Component, OnInit, HostListener, AfterViewInit, OnDestroy, effect } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MaterialModule } from '../../../../shared/material.module';
import { InfoTooltipComponent } from '../../../../shared/components/info-tooltip/info-tooltip.component';
import { DashboardService } from '../../services/dashboard.service';
import { finalize, Subscription } from 'rxjs';
import {
  DashboardTopCards,
  DashboardPerformanceChart,
  DashboardLatestTransaction,
  DashboardPortfolioDistribution,
  DashboardPortfolioComposition
} from '../../models/dashboard.models';
import { UserPortfolio } from '../../../portfolio/models/portfolio.model';
import Chart from 'chart.js/auto';
import { ThemeService } from '../../../../core/services/theme.service';
import { LanguageService } from '../../../../core/services/language.service';
import { ActivePortfolioService } from '../../../../core/services/active-portfolio.service';
import { ViewportService } from '../../../../core/services/viewport.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MaterialModule,
    InfoTooltipComponent,
    TranslateModule
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard implements OnInit, AfterViewInit, OnDestroy {

  // ── Estado interno ─────────────────────────────────────────────────────────
  private chart: Chart | null = null;
  private compositionChart: Chart | null = null;
  private resizeObserver: any;

  // ── Controles del gráfico de performance ──────────────────────────────────
  selectedChartType: 'line' | 'bar' = 'line';
  selectedPeriod: '1W' | '1M' | '3M' | '1Y' = '1M';

  /** Benchmarks disponibles para comparar contra el portfolio en el gráfico de performance. */
  readonly compareOptions: { key: 'sp500' | 'nasdaq'; label: string }[] = [
    { key: 'sp500', label: 'S&P 500' },
    { key: 'nasdaq', label: 'NASDAQ' }
  ];

  /**
   * Benchmarks activos en el gráfico. En desktop se muestran los 2 por defecto
   * (comportamiento histórico); en mobile arranca vacío — solo "Portfolio" — para
   * no saturar el gráfico, y el usuario suma comparación a propósito con un chip.
   */
  compareSymbols = new Set<'sp500' | 'nasdaq'>(['sp500', 'nasdaq']);

  // ── Filtro de fecha para composición ──────────────────────────────────────
  selectedCompositionDate: string = this.getTodayString();

  // ── Datos del dashboard ────────────────────────────────────────────────────
  topCards: DashboardTopCards | null = null;
  performanceChart: DashboardPerformanceChart | null = null;
  latestTransactions: DashboardLatestTransaction[] = [];
  portfolioDistribution: DashboardPortfolioDistribution[] = [];
  portfolioComposition: DashboardPortfolioComposition | null = null;

  // ── Estados de carga ───────────────────────────────────────────────────────
  loadingTopCards = false;
  loadingPerformanceChart = false;
  loadingLatestTransactions = false;
  loadingPortfolioDistribution = false;
  loadingPortfolioComposition = false;

  private readonly subscriptions = new Subscription();
  activeId: number | null = null;
  portfolios: UserPortfolio[] = [];

  constructor(
    private dashboardService: DashboardService,
    private activePortfolioService: ActivePortfolioService,
    private themeService: ThemeService,
    private languageService: LanguageService,
    public viewportService: ViewportService
  ) {
    if (this.viewportService.isMobile()) {
      this.compareSymbols.clear();
      this.selectedPeriod = '1W';
    }

    effect(() => {
      this.themeService.currentTheme();
      if (this.performanceChart?.data?.length) {
        this.destroyChart();
        this.renderChartWhenReady();
      }
      if (this.portfolioComposition) {
        this.destroyCompositionChart();
        this.renderCompositionChartWhenReady();
      }
    });
  }

  // ── Ciclo de vida ──────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.subscriptions.add(
      this.activePortfolioService.portfolios$.subscribe(portfolios => {
        this.portfolios = portfolios;
      })
    );

    this.subscriptions.add(
      this.activePortfolioService.activeId$.subscribe(activeId => {
        const changed = this.activeId !== null && this.activeId !== activeId;
        this.activeId = activeId;
        if (changed) this.loadAll();
      })
    );

    if (this.activeId !== null) {
      this.loadAll();
    } else {
      this.activePortfolioService.loadPortfolios().subscribe(() => {
        if (this.activeId !== null) this.loadAll();
      });
    }
  }

  onPortfolioChange(portfolioId: number): void {
    if (portfolioId === this.activeId) return;
    this.activePortfolioService.setActive(portfolioId);
  }

  ngAfterViewInit(): void {
    this.setupResizeObserver();
    this.renderChartWhenReady();
  }

  ngOnDestroy(): void {
    this.destroyChart();
    this.destroyCompositionChart();
    this.subscriptions.unsubscribe();

    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
    }
  }

  /** Vuelve a cargar toda la información del dashboard para el portfolio activo. */
  private loadAll(): void {
    this.loadTopCards();
    this.loadPerformanceChart();
    this.loadLatestTransactions();
    this.loadPortfolioDistribution();
    this.loadPortfolioComposition(this.selectedCompositionDate);
  }

  @HostListener('window:resize')
  onResize(): void {
    if (this.chart) this.chart.resize();
    if (this.compositionChart) this.compositionChart.resize();
  }

  // ── Getters ────────────────────────────────────────────────────────────────

  /**
   * Indica si hay suficientes puntos de datos para renderizar el gráfico de performance.
   */
  get hasEnoughChartData(): boolean {
    return !!this.performanceChart?.data && this.performanceChart.data.length > 0;
  }

  /** Indica si el snapshot de composición tiene datos para graficar. */
  get hasCompositionData(): boolean {
    return !!this.portfolioComposition &&
      (this.portfolioComposition.availableBalance > 0 || this.portfolioComposition.investedValue > 0);
  }

  get compositionAvailablePercent(): number {
    if (!this.hasCompositionData) return 0;
    const total = this.portfolioComposition!.totalValue;
    if (total <= 0) return 0;
    return Math.round((this.portfolioComposition!.availableBalance / total) * 1000) / 10;
  }

  get compositionInvestedPercent(): number {
    if (!this.hasCompositionData) return 0;
    const total = this.portfolioComposition!.totalValue;
    if (total <= 0) return 0;
    return Math.round((this.portfolioComposition!.investedValue / total) * 1000) / 10;
  }

  // ── Carga de datos ─────────────────────────────────────────────────────────

  /** Carga las tarjetas superiores con métricas globales del portafolio. */
  loadTopCards(): void {
    this.loadingTopCards = true;

    this.dashboardService.getTopCards(this.activeId!)
      .pipe(finalize(() => this.loadingTopCards = false))
      .subscribe({
        next: response => {
          this.topCards = (response.success && response.data) ? response.data : null;
        },
        error: () => { this.topCards = null; }
      });
  }

  /**
   * Carga los datos del gráfico de rendimiento para el período seleccionado.
   */
  loadPerformanceChart(): void {
    this.loadingPerformanceChart = true;
    this.destroyChart();

    this.dashboardService.getPerformanceChart(this.activeId!, this.selectedPeriod).subscribe({
      next: response => {
        this.loadingPerformanceChart = false;

        if (!response.success || !response.data) {
          this.performanceChart = null;
          return;
        }

        this.performanceChart = response.data;
        this.renderChartWhenReady();
      },
      error: () => {
        this.performanceChart = null;
        this.loadingPerformanceChart = false;
        this.destroyChart();
      }
    });
  }

  /** Carga las últimas transacciones realizadas en el portafolio. */
  loadLatestTransactions(): void {
    this.loadingLatestTransactions = true;

    this.dashboardService.getLatestTransactions(this.activeId!)
      .pipe(finalize(() => this.loadingLatestTransactions = false))
      .subscribe({
        next: response => {
          this.latestTransactions = (response.success && response.data) ? response.data : [];
        },
        error: () => { this.latestTransactions = []; }
      });
  }

  /** Carga la distribución del portafolio por sector. */
  loadPortfolioDistribution(): void {
    this.loadingPortfolioDistribution = true;

    this.dashboardService.getPortfolioDistribution(this.activeId!)
      .pipe(finalize(() => this.loadingPortfolioDistribution = false))
      .subscribe({
        next: response => {
          this.portfolioDistribution = (response.success && response.data) ? response.data : [];
        },
        error: () => { this.portfolioDistribution = []; }
      });
  }

  /** Carga la composición histórica del portfolio para la fecha seleccionada. */
  loadPortfolioComposition(date: string): void {
    this.loadingPortfolioComposition = true;
    this.destroyCompositionChart();

    this.dashboardService.getPortfolioComposition(this.activeId!, date)
      .pipe(finalize(() => this.loadingPortfolioComposition = false))
      .subscribe({
        next: response => {
          this.portfolioComposition = (response.success && response.data) ? response.data : null;
          this.renderCompositionChartWhenReady();
        },
        error: () => { this.portfolioComposition = null; }
      });
  }

  // ── Controles ──────────────────────────────────────────────────────────────

  onChartTypeChange(): void {
    this.destroyChart();
    this.renderChartWhenReady();
  }

  onPeriodChange(): void {
    this.loadPerformanceChart();
  }

  /**
   * Suma o quita un benchmark de la comparación mostrada en el gráfico de performance
   * (chips de mobile).
   * @param key Benchmark a alternar (`'sp500'` o `'nasdaq'`).
   */
  toggleCompareSymbol(key: 'sp500' | 'nasdaq'): void {
    if (this.compareSymbols.has(key)) {
      this.compareSymbols.delete(key);
    } else {
      this.compareSymbols.add(key);
    }
    this.destroyChart();
    this.renderChartWhenReady();
  }

  onCompositionDateChange(): void {
    if (this.selectedCompositionDate) {
      this.loadPortfolioComposition(this.selectedCompositionDate);
    }
  }

  // ── Helpers de presentación ────────────────────────────────────────────────

  /**
   * Formatea un número como moneda en dólares.
   */
  formatCurrency(value: number | null | undefined): string {
    return new Intl.NumberFormat('es-AR', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(value ?? 0);
  }

  /**
   * Formatea un número como porcentaje con signo explícito.
   */
  formatPercent(value: number | null | undefined): string {
    const number = value ?? 0;
    const sign = number > 0 ? '+' : '';
    return `${sign}${number.toFixed(2)}%`;
  }

  getBalanceClass(): string {
    const value = this.topCards?.totalValue ?? 0;
    if (value > 10000) return 'positive';
    if (value < 10000) return 'negative';
    return 'neutral';
  }

  getProfitClass(): string {
    const value = this.topCards?.todayProfit ?? 0;
    if (value > 0) return 'positive';
    if (value < 0) return 'negative';
    return 'neutral';
  }

  getChartVariationClass(): string {
    const value = this.performanceChart?.variationPercent ?? 0;
    if (value > 0) return 'positive';
    if (value < 0) return 'negative';
    return 'neutral';
  }

  getTransactionClass(type: string): string {
    const value = type?.toUpperCase();
    if (value.includes('COMPRA')) return 'buy';
    if (value.includes('VENTA')) return 'sell';
    return '';
  }

  getAllocationWidth(value: number): string {
    return `${Math.min(Math.max(value ?? 0, 0), 100)}%`;
  }

  /**
   * Convierte una fecha ISO a una cadena relativa legible en español.
   */
  formatRelativeDate(value: string): string {
    if (!value) return '';

    const date = new Date(value);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMinutes = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMinutes / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMinutes < 1) return this.languageService.instant('DASHBOARD.TIME_JUST_NOW');
    if (diffMinutes < 60) return this.languageService.instant('DASHBOARD.TIME_MINUTES_AGO', { minutes: diffMinutes });
    if (diffHours < 24) return this.languageService.instant('DASHBOARD.TIME_HOURS_AGO', { hours: diffHours });
    if (diffDays === 1) return this.languageService.instant('DASHBOARD.TIME_YESTERDAY');
    if (diffDays < 7) return this.languageService.instant('DASHBOARD.TIME_DAYS_AGO', { days: diffDays });

    return date.toLocaleDateString(this.languageService.current === 'en' ? 'en-US' : 'es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' });
  }

  // ── Gráfico de performance (privado) ──────────────────────────────────────

  private setupResizeObserver(): void {
    const chartContainer = document.querySelector('.chart-container');
    if (chartContainer && 'ResizeObserver' in window) {
      this.resizeObserver = new ResizeObserver(() => {
        if (this.chart) this.chart.resize();
      });
      this.resizeObserver.observe(chartContainer);
    }
  }

  private createChart(canvas: HTMLCanvasElement): void {
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    if (this.chart) this.chart.destroy();

    canvas.width = canvas.offsetWidth;
    canvas.height = 400;

    const data = this.generateChartData();
    const config = this.getChartConfiguration(data);

    this.chart = new Chart(ctx, config);
  }

  private generateChartData() {
    const chartPoints = this.performanceChart?.data ?? [];
    const labels = chartPoints.map(x => x.label);

    const datasets = [
      {
        label: 'Portfolio',
        data: chartPoints.map(x => Number((x.portfolio - 100).toFixed(2))),
        borderColor: '#4a90e2',
        backgroundColor: this.selectedChartType === 'line' ? 'rgba(74, 144, 226, 0.1)' : '#4a90e2',
        borderWidth: 2,
        tension: 0.4,
        fill: this.selectedChartType === 'line'
      }
    ];

    if (this.compareSymbols.has('sp500')) {
      datasets.push({
        label: 'S&P 500',
        data: chartPoints.map(x => Number((x.sp500 - 100).toFixed(2))),
        borderColor: '#10b981',
        backgroundColor: this.selectedChartType === 'line' ? 'rgba(16, 185, 129, 0.1)' : '#10b981',
        borderWidth: 2,
        tension: 0.4,
        fill: this.selectedChartType === 'line'
      });
    }

    if (this.compareSymbols.has('nasdaq')) {
      datasets.push({
        label: 'NASDAQ',
        data: chartPoints.map(x => Number((x.nasdaq - 100).toFixed(2))),
        borderColor: '#f59e0b',
        backgroundColor: this.selectedChartType === 'line' ? 'rgba(245, 158, 11, 0.1)' : '#f59e0b',
        borderWidth: 2,
        tension: 0.4,
        fill: this.selectedChartType === 'line'
      });
    }

    return { labels, datasets, chartPoints };
  }

  private getChartConfiguration(data: any): any {
    const t = this.themeService.getChartTheme();

    const config = {
      type: this.selectedChartType,
      data: data,
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: true,
            position: 'top' as any,
            labels: {
              color: t.legendColor,
              font: { size: 12, weight: '500' },
              usePointStyle: true,
              padding: 20
            }
          },
          tooltip: {
            mode: 'index' as any,
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
                const pt = data.chartPoints?.[context.dataIndex];
                const pct = context.parsed.y;
                const sign = pct > 0 ? '+' : '';
                const fmtUsd = (v: number) => `$${v.toLocaleString('es-ES', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
                const fmtPct = (v: number) => `${v > 0 ? '+' : ''}${v.toFixed(2)}%`;
                const label = context.dataset.label || '';
                if (!pt) return `${label}: ${fmtPct(pct)}`;
                switch (label) {
                  case 'Portfolio': return `${label}: ${fmtUsd(pt.portfolioValue)} (${fmtPct(pct)})`;
                  case 'S&P 500': return `${label}: ${fmtUsd(pt.sp500Value)} (${fmtPct(pct)})`;
                  case 'NASDAQ': return `${label}: ${fmtUsd(pt.nasdaqValue)} (${fmtPct(pct)})`;
                  default: return `${label}: ${fmtPct(pct)}`;
                }
              }
            }
          }
        },
        scales: {
          x: {
            grid: { color: t.gridColor, borderColor: t.gridBorderColor },
            ticks: { color: t.textColor, font: { size: 11 } }
          },
          y: {
            grid: { color: t.gridColor, borderColor: t.gridBorderColor },
            ticks: {
              color: t.textColor,
              font: { size: 11 },
              callback: (value: any) => {
                const number = Number(value);
                const sign = number > 0 ? '+' : '';
                return `${sign}${number.toFixed(2)}%`;
              }
            }
          }
        },
        interaction: { mode: 'index' as any, intersect: false }
      }
    };

    return config;
  }

  private renderChartWhenReady(): void {
    if (!this.performanceChart?.data?.length) return;

    setTimeout(() => {
      const canvas = document.getElementById('mainChart') as HTMLCanvasElement;

      if (!canvas) {
        setTimeout(() => this.renderChartWhenReady(), 50);
        return;
      }

      this.createChart(canvas);
    }, 0);
  }

  private destroyChart(): void {
    if (!this.chart) return;
    this.chart.destroy();
    this.chart = null;
  }

  // ── Gráfico de composición (privado) ──────────────────────────────────────

  private createCompositionChart(canvas: HTMLCanvasElement): void {
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    if (this.compositionChart) this.compositionChart.destroy();

    const t = this.themeService.getChartTheme();
    const available = this.portfolioComposition?.availableBalance ?? 0;
    const invested  = this.portfolioComposition?.investedValue ?? 0;

    const availableLabel = this.languageService.instant('DASHBOARD.COMPOSITION_AVAILABLE');
    const investedLabel  = this.languageService.instant('DASHBOARD.COMPOSITION_INVESTED');

    this.compositionChart = new Chart(ctx, {
      type: 'doughnut',
      data: {
        labels: [availableLabel, investedLabel],
        datasets: [{
          data: [available, invested],
          backgroundColor: ['rgba(74, 225, 118, 0.75)', 'rgba(74, 144, 226, 0.75)'],
          borderColor:     ['#4AE176', '#4a90e2'],
          borderWidth: 2,
          hoverOffset: 6
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        cutout: '62%',
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: t.tooltipBg,
            titleColor: t.tooltipText,
            bodyColor: t.tooltipText,
            borderColor: t.tooltipBorderColor,
            borderWidth: 1,
            padding: 12,
            callbacks: {
              label: (context: any) => {
                const value = context.parsed;
                const total = (context.dataset.data as number[]).reduce((a: number, b: number) => a + b, 0);
                const pct   = total > 0 ? ((value / total) * 100).toFixed(1) : '0.0';
                return ` ${new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'USD', minimumFractionDigits: 2 }).format(value)} (${pct}%)`;
              }
            }
          }
        }
      }
    });
  }

  private renderCompositionChartWhenReady(): void {
    if (!this.hasCompositionData) return;

    setTimeout(() => {
      const canvas = document.getElementById('compositionChart') as HTMLCanvasElement;

      if (!canvas) {
        setTimeout(() => this.renderCompositionChartWhenReady(), 50);
        return;
      }

      this.createCompositionChart(canvas);
    }, 0);
  }

  private destroyCompositionChart(): void {
    if (!this.compositionChart) return;
    this.compositionChart.destroy();
    this.compositionChart = null;
  }

  // ── Utilidades ─────────────────────────────────────────────────────────────

  private getTodayString(): string {
    return new Date().toISOString().split('T')[0];
  }
}
