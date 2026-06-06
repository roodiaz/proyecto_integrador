import { Component, OnInit, HostListener, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MaterialModule } from '../../../../shared/material.module';
import { InfoTooltipComponent } from '../../../../shared/components/info-tooltip/info-tooltip.component';
import { DashboardService } from '../../services/dashboard.service';
import { finalize } from 'rxjs';
import { OnDestroy } from '@angular/core';
import {
  DashboardTopCards,
  DashboardPerformanceChart,
  DashboardLatestTransaction,
  DashboardRecentNotification,
  DashboardPortfolioDistribution
} from '../../models/dashboard.models';

// Importación de Chart.js con fallback
import Chart from 'chart.js/auto';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MaterialModule,
    InfoTooltipComponent
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard implements OnInit, AfterViewInit, OnDestroy {
  private viewInitialized = false;
  private chart: Chart | null = null;
  private resizeObserver: any;

  selectedChartType: 'line' | 'bar' = 'line';
  selectedPeriod: '1W' | '1M' | '3M' | '1Y' = '1M';
  topCards: DashboardTopCards | null = null;
  performanceChart: DashboardPerformanceChart | null = null;
  loadingTopCards = false;
  loadingPerformanceChart = false;

  latestTransactions: DashboardLatestTransaction[] = [];
  loadingLatestTransactions = false;

  recentNotifications: DashboardRecentNotification[] = [];
  loadingRecentNotifications = false;

  portfolioDistribution: DashboardPortfolioDistribution[] = [];
  loadingPortfolioDistribution = false;

  constructor(
    private dashboardService: DashboardService
  ) { }

  ngOnInit() {
    this.loadTopCards();
    this.loadPerformanceChart();
    this.loadLatestTransactions();
    this.loadRecentNotifications()
    this.loadPortfolioDistribution();
  }

  ngAfterViewInit(): void {
    this.viewInitialized = true;
    this.setupResizeObserver();
    this.renderChartWhenReady();
  }

  ngOnDestroy(): void {
    if (this.chart) {
      this.chart.destroy();
      this.chart = null;
    }

    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
    }
  }

  @HostListener('window:resize')
  onResize() {
    if (this.chart) {
      this.chart.resize();
    }
  }

  private setupChart() {
    const canvas = document.getElementById('mainChart') as HTMLCanvasElement;
    if (canvas) {
      this.createChart(canvas);
    }
  }

  private setupResizeObserver() {
    const chartContainer = document.querySelector('.chart-container');
    if (chartContainer && 'ResizeObserver' in window) {
      this.resizeObserver = new ResizeObserver(() => {
        if (this.chart) {
          this.chart.resize();
        }
      });
      this.resizeObserver.observe(chartContainer);
    }
  }

  private createChart(canvas: HTMLCanvasElement) {
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Destruir gráfico existente
    if (this.chart) {
      this.chart.destroy();
    }

    // Asegurar que el canvas tenga el tamaño correcto
    canvas.width = canvas.offsetWidth;
    canvas.height = 400;

    // Generar datos según el período seleccionado
    const data = this.generateChartData();
    const config = this.getChartConfiguration(data);

    this.chart = new Chart(ctx, config);
  }

  loadTopCards(): void {
    this.loadingTopCards = true;

    this.dashboardService.getTopCards()
      .pipe(finalize(() => this.loadingTopCards = false))
      .subscribe({
        next: response => {
          if (!response.success || !response.data) {
            this.topCards = null;
            return;
          }

          this.topCards = response.data;
        },
        error: error => {
          console.error('Error cargando cards del dashboard', error);
          this.topCards = null;
        }
      });
  }

  loadLatestTransactions(): void {
    this.loadingLatestTransactions = true;

    this.dashboardService.getLatestTransactions()
      .pipe(finalize(() => this.loadingLatestTransactions = false))
      .subscribe({
        next: response => {
          if (!response.success || !response.data) {
            this.latestTransactions = [];
            return;
          }

          this.latestTransactions = response.data;
        },
        error: error => {
          console.error('Error cargando últimas operaciones', error);
          this.latestTransactions = [];
        }
      });
  }

  getTransactionClass(type: string): string {
    const value = type?.toUpperCase();
    if (value.includes('COMPRA')) return 'buy';
    if (value.includes('VENTA')) return 'sell';
    return '';
  }

  loadPortfolioDistribution(): void {
    this.loadingPortfolioDistribution = true;

    this.dashboardService.getPortfolioDistribution()
      .pipe(finalize(() => this.loadingPortfolioDistribution = false))
      .subscribe({
        next: response => {
          if (!response.success || !response.data) {
            this.portfolioDistribution = [];
            return;
          }

          this.portfolioDistribution = response.data;
        },
        error: error => {
          console.error('Error cargando distribución del portfolio', error);
          this.portfolioDistribution = [];
        }
      });
  }

  loadPerformanceChart(): void {
    this.loadingPerformanceChart = true;
    this.destroyChart();

    this.dashboardService.getPerformanceChart(this.selectedPeriod).subscribe({
      next: response => {
        if (!response.success || !response.data) {
          this.performanceChart = null;
          this.loadingPerformanceChart = false;
          return;
        }

        this.performanceChart = response.data;
        this.loadingPerformanceChart = false;

        this.renderChartWhenReady();
      },
      error: error => {
        console.error('Error cargando gráfico del dashboard', error);
        this.performanceChart = null;
        this.loadingPerformanceChart = false;
        this.destroyChart();
      }
    });
  }

  formatCurrency(value: number | null | undefined): string {
    return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'USD', minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value ?? 0);
  }

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
      },
      {
        label: 'S&P 500',
        data: chartPoints.map(x => Number((x.sp500 - 100).toFixed(2))),
        borderColor: '#10b981',
        backgroundColor: this.selectedChartType === 'line' ? 'rgba(16, 185, 129, 0.1)' : '#10b981',
        borderWidth: 2,
        tension: 0.4,
        fill: this.selectedChartType === 'line'
      },
      {
        label: 'NASDAQ',
        data: chartPoints.map(x => Number((x.nasdaq - 100).toFixed(2))),
        borderColor: '#f59e0b',
        backgroundColor: this.selectedChartType === 'line' ? 'rgba(245, 158, 11, 0.1)' : '#f59e0b',
        borderWidth: 2,
        tension: 0.4,
        fill: this.selectedChartType === 'line'
      }
    ];

    return { labels, datasets };
  }

  private getChartConfiguration(data: any): any {
    const baseConfig = {
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
              color: '#ffffff',
              font: {
                size: 12,
                weight: '500'
              },
              usePointStyle: true,
              padding: 20
            }
          },
          tooltip: {
            mode: 'index' as any,
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
                let label = context.dataset.label || '';
                if (label) {
                  label += ': ';
                }
                if (context.parsed.y !== null) {
                  label += new Intl.NumberFormat('es-ES', {
                    style: 'currency',
                    currency: 'USD',
                    minimumFractionDigits: 0,
                    maximumFractionDigits: 0
                  }).format(context.parsed.y);
                }
                return label;
              }
            }
          }
        },
        scales: {
          x: {
            grid: {
              color: 'rgba(255, 255, 255, 0.1)',
              borderColor: 'rgba(255, 255, 255, 0.2)'
            },
            ticks: {
              color: 'rgba(255, 255, 255, 0.7)',
              font: {
                size: 11
              }
            }
          },
          y: {
            grid: {
              color: 'rgba(255, 255, 255, 0.1)',
              borderColor: 'rgba(255, 255, 255, 0.2)'
            },
            ticks: {
              color: 'rgba(255, 255, 255, 0.7)',
              font: {
                size: 11
              },
              callback: (value: any) => {
                const number = Number(value);
                const sign = number > 0 ? '+' : '';
                return `${sign}${number}%`;
              }
            }
          }
        },
        interaction: {
          mode: 'index' as any,
          intersect: false
        }
      }
    };

    // Configuración específica para gráfico de barras
    if (this.selectedChartType === 'bar') {
      baseConfig.options.plugins.tooltip.callbacks = {
        label: (context: any) => {
          let label = context.dataset.label || '';
          if (label) {
            label += ': ';
          }
          if (context.parsed.y !== null) {
            const value = context.parsed.y;
            const sign = value > 0 ? '+' : '';
            label += `${sign}${value.toFixed(2)}%`;
          }
          return label;
        }
      };
    }

    return baseConfig;
  }

  getChartVariationClass(): string {
    const value = this.performanceChart?.variationPercent ?? 0;
    if (value > 0) return 'positive';
    if (value < 0) return 'negative';
    return 'neutral';
  }

  onChartTypeChange(): void {
    this.destroyChart();
    this.renderChartWhenReady();
  }

  onPeriodChange() {
    this.loadPerformanceChart();
  }

  get hasEnoughChartData(): boolean {
    return !!this.performanceChart?.data && this.performanceChart.data.length > 0;
  }

  loadRecentNotifications(): void {
    this.loadingRecentNotifications = true;

    this.dashboardService.getRecentNotifications()
      .pipe(finalize(() => this.loadingRecentNotifications = false))
      .subscribe({
        next: response => {
          if (!response.success || !response.data) {
            this.recentNotifications = [];
            return;
          }

          this.recentNotifications = response.data;
        },
        error: error => {
          console.error('Error cargando notificaciones recientes', error);
          this.recentNotifications = [];
        }
      });
  }

  getNotificationSymbol(message: string): string {
    return message?.trim()?.split(' ')[0] ?? '';
  }

  getNotificationMessage(message: string): string {
    const parts = message?.trim()?.split(' ') ?? [];
    return parts.length > 1 ? parts.slice(1).join(' ') : message;
  }

  getNotificationClass(notification: DashboardRecentNotification): string {
    const message = notification.message?.toLowerCase() ?? '';

    if (message.includes('subió') || message.includes('superó') || message.includes('alcanzó')) return 'success';
    if (message.includes('cayó') || message.includes('bajó') || message.includes('debajo')) return 'danger';

    return 'warning';
  }

  formatRelativeDate(value: string): string {
    if (!value) return '';

    const date = new Date(value);
    const now = new Date();

    const diffMs = now.getTime() - date.getTime();
    const diffMinutes = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMinutes / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMinutes < 1) return 'Recién';
    if (diffMinutes < 60) return `Hace ${diffMinutes} min`;
    if (diffHours < 24) return `Hace ${diffHours} h`;
    if (diffDays === 1) return 'Ayer';
    if (diffDays < 7) return `Hace ${diffDays} días`;

    return date.toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' });
  }

  getAllocationWidth(value: number): string {
    return `${Math.min(Math.max(value ?? 0, 0), 100)}%`;
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
}


