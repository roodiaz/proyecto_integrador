import { Component, OnInit, HostListener, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MaterialModule } from '../../../../shared/material.module';
import { InfoTooltipComponent } from '../../../../shared/components/info-tooltip/info-tooltip.component';
import { DashboardService } from '../../services/dashboard.service';
import { finalize } from 'rxjs';
import {
  DashboardTopCards,
  DashboardPerformanceChart,
  DashboardLatestTransaction,
  DashboardRecentNotification,
  DashboardPortfolioDistribution
} from '../../models/dashboard.models';
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

  // ── Estado interno ─────────────────────────────────────────────────────────
  private chart: Chart | null = null;
  private resizeObserver: any;

  // ── Controles del gráfico ──────────────────────────────────────────────────
  selectedChartType: 'line' | 'bar' = 'line';
  selectedPeriod: '1W' | '1M' | '3M' | '1Y' = '1M';

  // ── Datos del dashboard ────────────────────────────────────────────────────
  topCards: DashboardTopCards | null = null;
  performanceChart: DashboardPerformanceChart | null = null;
  latestTransactions: DashboardLatestTransaction[] = [];
  recentNotifications: DashboardRecentNotification[] = [];
  portfolioDistribution: DashboardPortfolioDistribution[] = [];

  // ── Estados de carga ───────────────────────────────────────────────────────
  loadingTopCards = false;
  loadingPerformanceChart = false;
  loadingLatestTransactions = false;
  loadingRecentNotifications = false;
  loadingPortfolioDistribution = false;

  constructor(private dashboardService: DashboardService) { }

  // ── Ciclo de vida ──────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.loadTopCards();
    this.loadPerformanceChart();
    this.loadLatestTransactions();
    this.loadRecentNotifications();
    this.loadPortfolioDistribution();
  }

  ngAfterViewInit(): void {
    this.setupResizeObserver();
    this.renderChartWhenReady();
  }

  ngOnDestroy(): void {
    this.destroyChart();

    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
    }
  }

  /** Redimensiona el gráfico cuando cambia el tamaño de la ventana. */
  @HostListener('window:resize')
  onResize(): void {
    if (this.chart) {
      this.chart.resize();
    }
  }

  // ── Getters ────────────────────────────────────────────────────────────────

  /**
   * Indica si hay suficientes puntos de datos para renderizar el gráfico.
   * @returns `true` si el array de datos del gráfico tiene al menos un punto.
   */
  get hasEnoughChartData(): boolean {
    return !!this.performanceChart?.data && this.performanceChart.data.length > 0;
  }

  // ── Carga de datos ─────────────────────────────────────────────────────────

  /** Carga las tarjetas superiores con métricas globales del portafolio. */
  loadTopCards(): void {
    this.loadingTopCards = true;

    this.dashboardService.getTopCards()
      .pipe(finalize(() => this.loadingTopCards = false))
      .subscribe({
        next: response => {
          this.topCards = (response.success && response.data) ? response.data : null;
        },
        error: error => {
          console.error('Error cargando cards del dashboard', error);
          this.topCards = null;
        }
      });
  }

  /**
   * Carga los datos del gráfico de rendimiento para el período seleccionado.
   * Destruye el gráfico actual antes de iniciar la carga para evitar instancias duplicadas.
   */
  loadPerformanceChart(): void {
    this.loadingPerformanceChart = true;
    this.destroyChart();

    this.dashboardService.getPerformanceChart(this.selectedPeriod).subscribe({
      next: response => {
        this.loadingPerformanceChart = false;

        if (!response.success || !response.data) {
          this.performanceChart = null;
          return;
        }

        this.performanceChart = response.data;
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

  /** Carga las últimas transacciones realizadas en el portafolio. */
  loadLatestTransactions(): void {
    this.loadingLatestTransactions = true;

    this.dashboardService.getLatestTransactions()
      .pipe(finalize(() => this.loadingLatestTransactions = false))
      .subscribe({
        next: response => {
          this.latestTransactions = (response.success && response.data) ? response.data : [];
        },
        error: error => {
          console.error('Error cargando últimas operaciones', error);
          this.latestTransactions = [];
        }
      });
  }

  /** Carga la distribución del portafolio por instrumento. */
  loadPortfolioDistribution(): void {
    this.loadingPortfolioDistribution = true;

    this.dashboardService.getPortfolioDistribution()
      .pipe(finalize(() => this.loadingPortfolioDistribution = false))
      .subscribe({
        next: response => {
          this.portfolioDistribution = (response.success && response.data) ? response.data : [];
        },
        error: error => {
          console.error('Error cargando distribución del portfolio', error);
          this.portfolioDistribution = [];
        }
      });
  }

  /** Carga las notificaciones de alertas más recientes para el panel de resumen. */
  loadRecentNotifications(): void {
    this.loadingRecentNotifications = true;

    this.dashboardService.getRecentNotifications()
      .pipe(finalize(() => this.loadingRecentNotifications = false))
      .subscribe({
        next: response => {
          this.recentNotifications = (response.success && response.data) ? response.data : [];
        },
        error: error => {
          console.error('Error cargando notificaciones recientes', error);
          this.recentNotifications = [];
        }
      });
  }

  // ── Controles del gráfico ──────────────────────────────────────────────────

  /** Destruye el gráfico actual y lo recrea con el tipo de visualización seleccionado. */
  onChartTypeChange(): void {
    this.destroyChart();
    this.renderChartWhenReady();
  }

  /** Recarga los datos del gráfico para el período recién seleccionado. */
  onPeriodChange(): void {
    this.loadPerformanceChart();
  }

  // ── Helpers de presentación ────────────────────────────────────────────────

  /**
   * Formatea un número como moneda en dólares con separadores para `es-AR`.
   * @param value Valor numérico a formatear. `null`/`undefined` se tratan como `0`.
   * @returns Cadena con formato `$ 1.234,56`.
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
   * @param value Valor porcentual. `null`/`undefined` se tratan como `0`.
   * @returns Cadena con formato `+1.23%` o `-1.23%`.
   */
  formatPercent(value: number | null | undefined): string {
    const number = value ?? 0;
    const sign = number > 0 ? '+' : '';
    return `${sign}${number.toFixed(2)}%`;
  }

  /**
   * Devuelve la clase CSS para colorear la tarjeta de saldo total según si el
   * valor del portafolio es mayor, igual o menor al capital inicial (USD 10.000).
   * @returns `'positive'`, `'negative'` o `'neutral'`.
   */
  getBalanceClass(): string {
    const value = this.topCards?.totalValue ?? 0;
    if (value > 10000) return 'positive';
    if (value < 10000) return 'negative';
    return 'neutral';
  }

  /**
   * Devuelve la clase CSS para colorear la tarjeta de ganancia del día
   * según si el valor es positivo, negativo o neutro.
   * @returns `'positive'`, `'negative'` o `'neutral'`.
   */
  getProfitClass(): string {
    const value = this.topCards?.todayProfit ?? 0;
    if (value > 0) return 'positive';
    if (value < 0) return 'negative';
    return 'neutral';
  }

  /**
   * Devuelve la clase CSS para el badge de variación del gráfico de rendimiento.
   * @returns `'positive'`, `'negative'` o `'neutral'`.
   */
  getChartVariationClass(): string {
    const value = this.performanceChart?.variationPercent ?? 0;
    if (value > 0) return 'positive';
    if (value < 0) return 'negative';
    return 'neutral';
  }

  /**
   * Mapea el tipo de transacción al nombre de clase CSS para el badge de operación.
   * @param type Cadena con el tipo de operación (ej. `'COMPRA'`, `'VENTA'`).
   * @returns `'buy'`, `'sell'` o cadena vacía si no coincide.
   */
  getTransactionClass(type: string): string {
    const value = type?.toUpperCase();
    if (value.includes('COMPRA')) return 'buy';
    if (value.includes('VENTA')) return 'sell';
    return '';
  }

  /**
   * Clasifica una notificación según el contenido del mensaje para aplicar
   * el color de borde correspondiente al item del panel de alertas.
   * @param notification Objeto de notificación reciente del dashboard.
   * @returns `'success'`, `'danger'` o `'warning'`.
   */
  getNotificationClass(notification: DashboardRecentNotification): string {
    const message = notification.message?.toLowerCase() ?? '';

    if (message.includes('subió') || message.includes('superó') || message.includes('alcanzó')) return 'success';
    if (message.includes('cayó') || message.includes('bajó') || message.includes('debajo')) return 'danger';

    return 'warning';
  }

  /**
   * Extrae el símbolo del instrumento (primera palabra del mensaje de notificación).
   * @param message Texto completo del mensaje (ej. `'AAPL superó el precio objetivo'`).
   * @returns Primera palabra del mensaje, o cadena vacía si el mensaje es nulo.
   */
  getNotificationSymbol(message: string): string {
    return message?.trim()?.split(' ')[0] ?? '';
  }

  /**
   * Extrae el cuerpo del mensaje de notificación sin el símbolo inicial.
   * @param message Texto completo del mensaje.
   * @returns Todas las palabras del mensaje excepto la primera.
   */
  getNotificationMessage(message: string): string {
    const parts = message?.trim()?.split(' ') ?? [];
    return parts.length > 1 ? parts.slice(1).join(' ') : message;
  }

  /**
   * Convierte una fecha ISO a una cadena relativa legible en español
   * (ej. `'Hace 5 min'`, `'Ayer'`, `'Hace 3 días'`).
   * @param value Cadena de fecha en formato ISO 8601.
   * @returns Fecha relativa como texto o fecha formateada `DD/MM/AAAA` si supera una semana.
   */
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

  /**
   * Convierte un porcentaje de asignación al ancho CSS de la barra de progreso,
   * limitado entre 0 % y 100 %.
   * @param value Porcentaje de asignación del instrumento (0-100).
   * @returns Cadena CSS con formato `'42%'`.
   */
  getAllocationWidth(value: number): string {
    return `${Math.min(Math.max(value ?? 0, 0), 100)}%`;
  }

  // ── Gráfico (privado) ──────────────────────────────────────────────────────

  /**
   * Registra un `ResizeObserver` sobre `.chart-container` para redimensionar el
   * gráfico automáticamente cuando el contenedor cambia de tamaño.
   */
  private setupResizeObserver(): void {
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

  /**
   * Inicializa o reemplaza la instancia de `Chart.js` sobre el canvas dado.
   * Destruye cualquier gráfico previo antes de crear uno nuevo para evitar
   * fugas de memoria.
   * @param canvas Elemento `<canvas>` sobre el que se renderizará el gráfico.
   */
  private createChart(canvas: HTMLCanvasElement): void {
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    if (this.chart) {
      this.chart.destroy();
    }

    canvas.width = canvas.offsetWidth;
    canvas.height = 400;

    const data = this.generateChartData();
    const config = this.getChartConfiguration(data);

    this.chart = new Chart(ctx, config);
  }

  /**
   * Construye los datasets de Chart.js a partir de los puntos de
   * `performanceChart.data` para los tres índices: Portfolio, S&P 500 y NASDAQ.
   * @returns Objeto `{ labels, datasets }` listo para pasarle al constructor de `Chart`.
   */
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

  /**
   * Genera la configuración completa de Chart.js para el tipo de gráfico activo.
   * En modo `'bar'` el tooltip muestra variación porcentual; en modo `'line'`
   * muestra el valor en dólares.
   * @param data Datasets generados por `generateChartData()`.
   * @returns Objeto de configuración listo para pasarle al constructor de `Chart`.
   */
  private getChartConfiguration(data: any): any {
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
              color: '#ffffff',
              font: { size: 12, weight: '500' },
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
                if (label) label += ': ';
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
            ticks: { color: 'rgba(255, 255, 255, 0.7)', font: { size: 11 } }
          },
          y: {
            grid: {
              color: 'rgba(255, 255, 255, 0.1)',
              borderColor: 'rgba(255, 255, 255, 0.2)'
            },
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
        interaction: { mode: 'index' as any, intersect: false }
      }
    };

    if (this.selectedChartType === 'bar') {
      config.options.plugins.tooltip.callbacks = {
        label: (context: any) => {
          let label = context.dataset.label || '';
          if (label) label += ': ';
          if (context.parsed.y !== null) {
            const value = context.parsed.y;
            const sign = value > 0 ? '+' : '';
            label += `${sign}${value.toFixed(2)}%`;
          }
          return label;
        }
      };
    }

    return config;
  }

  /**
   * Espera a que el elemento `#mainChart` esté disponible en el DOM antes de
   * crear el gráfico. Si el canvas no existe aún, reintenta tras 50 ms.
   * No hace nada si no hay datos de rendimiento cargados.
   */
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

  /** Destruye la instancia activa de Chart.js y limpia la referencia. */
  private destroyChart(): void {
    if (!this.chart) return;

    this.chart.destroy();
    this.chart = null;
  }
}
