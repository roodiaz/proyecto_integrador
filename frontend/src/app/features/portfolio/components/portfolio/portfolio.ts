import { Component, OnInit, OnDestroy, AfterViewInit, effect } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize, Subscription } from 'rxjs';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { MatDialog } from '@angular/material/dialog';

import { PortfolioService } from '../../services/portfolio.service';
import { ActivePortfolioService } from '../../../../core/services/active-portfolio.service';
import { ThemeService } from '../../../../core/services/theme.service';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { LanguageService } from '../../../../core/services/language.service';
import { MaterialModule } from '../../../../shared/material.module';
import { InfoTooltipComponent } from '../../../../shared/components/info-tooltip/info-tooltip.component';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { PortfolioModal } from '../portfolio-modal/portfolio-modal';
import { PortfolioSetupDialog } from '../portfolio-setup-dialog/portfolio-setup-dialog';
import { BuyData, SellData, PortfolioModalResult } from '../../models/portfolio.modal.model';
import {
  PortfolioBalanceCards,
  PortfolioPieChartItem,
  PortfolioLineChartItem,
  TransactionFilter,
  PortfolioTransaction,
  SetupPortfolioRequest,
  UserPortfolio,
} from '../../models/portfolio.model';

/**
 * Pantalla principal del portfolio simulado.
 *
 * Muestra un resumen del estado de la cuenta (saldo inicial/actual, ganancia o pérdida
 * y operaciones del día), gráficos de distribución y evolución del portfolio, y dos
 * tablas paginadas y filtrables: las tenencias actuales (posiciones abiertas) y el
 * historial de operaciones realizadas. También permite comprar, vender y reiniciar
 * la simulación.
 */
@Component({
  selector: 'app-portfolio',
  standalone: true,
  imports: [CommonModule, FormsModule, MaterialModule, InfoTooltipComponent, TranslateModule],
  templateUrl: './portfolio.html',
  styleUrl: './portfolio.css',
})
export class Portfolio implements OnInit, OnDestroy, AfterViewInit {

  // ── Estados de carga ──
  loadingSummary = false;
  loadingCharts = false;
  loadingPositions = false;
  loadingOperations = false;

  // ── Tarjetas de resumen ──
  portfolioSummary: PortfolioBalanceCards = {
    portfolioName: null,
    currentBalance: 0,
    totalBalance: 0,
    profitLoss: 0,
    profitLossPercent: 0,
    realizedProfitLoss: 0,
    unrealizedProfitLoss: 0,
    totalOperations: 0,
    maxOperations: 0,
    lastMarketCloseDate: '',
  };

  // ── Tenencias (posiciones abiertas) ──
  positions: any[] = [];
  totalPositions: number = 0;
  emptyHoldingRows: number[] = [];
  symbolFilter: string = '';
  sortField: string | null = null;
  sortDirection: 'asc' | 'desc' | null = null;
  positionsPage: number = 1;
  positionsPageSize: number = 5;

  // ── Operaciones (historial de transacciones) ──
  operations: PortfolioTransaction[] = [];
  totalOperations: number = 0;
  emptyOperationRows: number[] = [];
  operationSymbolFilter: string = '';
  operationTypeFilter: number | null = null;
  operationFromDate: string = '';
  operationToDate: string = '';
  operationSortField: string | null = null;
  operationSortDirection: 'asc' | 'desc' | null = null;
  operationsPage: number = 1;
  operationsPageSize: number = 10;

  // ── Estado de la interfaz ──
  showOperationsFilters = false;
  downloadingHoldings = false;
  downloadingTransactions = false;

  // ── Gráficos ──
  pieChartData: PortfolioPieChartItem[] = [];
  lineChartData: PortfolioLineChartItem[] = [];
  topAssets: Array<{ ticker: string; percentage: number }> = [];
  pieChart: Chart | null = null;
  lineChart: Chart | null = null;
  selectedPeriod = '1m';
  timePeriods = [
    { value: '7d', label: '7 días' },
    { value: '1m', label: '1 mes' },
    { value: '3m', label: '3 meses' },
    { value: '6m', label: '6 meses' },
    { value: '1y', label: '1 año' },
  ];

  // ── Multi-portfolio ──
  portfolios: UserPortfolio[] = [];
  activeId: number | null = null;

  // ── Internos ──
  private chartRequestsInProgress = 0;
  private viewInitialized = false;
  private readonly subscriptions = new Subscription();

  private readonly PIE_COLORS = [
    '#A9C455', '#4DA3F5', '#FF6E40',
    '#AB47BC', '#26A69A', '#FFA726',
    '#EC407A', '#5C6BC0',
  ];
  private readonly TOP_ASSET_COLORS = ['#FF6384', '#36A2EB', '#FFCE56'];

  constructor(
    private snackBarService: SnackBarService,
    private portfolioService: PortfolioService,
    private activePortfolioService: ActivePortfolioService,
    private dialog: MatDialog,
    private themeService: ThemeService,
    private languageService: LanguageService,
  ) {
    Chart.register(...registerables);
    effect(() => {
      this.themeService.currentTheme();
      if (this.pieChartData.length) this.createPieChart();
      if (this.lineChartData.length) this.createLineChart();
    });
  }

  // ── Ciclo de vida ──

  /** Carga la lista de portfolios del usuario y, una vez resuelto el activo, toda su información. */
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
        if (changed) this.refreshPortfolio();
      })
    );

    this.activePortfolioService.loadPortfolios().subscribe({
      next: () => {
        if (this.activeId !== null) this.refreshPortfolio();
      },
      error: (err) => console.error('Portfolios error', err),
    });
  }

  /** Marca la vista como lista y dispara el renderizado de los gráficos si ya hay datos cargados. */
  ngAfterViewInit(): void {
    this.viewInitialized = true;
    this.renderChartsWhenReady();
  }

  /** Destruye los gráficos de Chart.js y cancela las suscripciones al salir de la pantalla. */
  ngOnDestroy(): void {
    this.pieChart?.destroy();
    this.lineChart?.destroy();
    this.subscriptions.unsubscribe();
  }

  // ── Computados ──

  /** @returns El índice del tab activo dentro de `portfolios`, según el portfolio activo. */
  get activeTabIndex(): number {
    const index = this.portfolios.findIndex(p => p.id === this.activeId);
    return index >= 0 ? index : 0;
  }

  /** @returns `true` si el usuario puede crear un portfolio adicional (máximo 3). */
  get canCreateMore(): boolean {
    return this.activePortfolioService.canCreateMore;
  }

  /** @returns `true` si el portfolio activo es el único del usuario (no se puede eliminar). */
  get isOnlyPortfolio(): boolean {
    return this.portfolios.length <= 1;
  }

  /** @returns La cantidad total de páginas de la tabla de tenencias, según el tamaño de página actual. */
  get totalPositionsPages(): number {
    return Math.max(1, Math.ceil(this.totalPositions / this.positionsPageSize));
  }

  /** @returns La cantidad total de páginas de la tabla de operaciones, según el tamaño de página actual. */
  get totalOperationsPages(): number {
    return Math.max(1, Math.ceil(this.totalOperations / this.operationsPageSize));
  }

  // ── Paginación: numeración de páginas ──

  /** @returns Los números de página a mostrar en el paginador de tenencias, centrados en la página actual. */
  getPositionPageNumbers(): number[] { return this.buildPageNumbers(this.positionsPage, this.totalPositionsPages); }

  /** @returns Los números de página a mostrar en el paginador de operaciones, centrados en la página actual. */
  getOperationPageNumbers(): number[] { return this.buildPageNumbers(this.operationsPage, this.totalOperationsPages); }

  /**
   * Calcula una ventana de hasta 5 números de página centrada en la página actual,
   * ajustándola para que no se salga del rango `[1, total]`.
   * @param current Página actualmente seleccionada.
   * @param total Cantidad total de páginas disponibles.
   * @returns Arreglo con los números de página a mostrar en el paginador.
   */
  private buildPageNumbers(current: number, total: number): number[] {
    const maxVisible = 5;
    let start = Math.max(1, current - Math.floor(maxVisible / 2));
    let end = Math.min(total, start + maxVisible - 1);
    if (end - start + 1 < maxVisible) start = Math.max(1, end - maxVisible + 1);
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  }

  // ── Paginación: navegación de tenencias ──

  /** Retrocede una página en la tabla de tenencias y vuelve a cargarla. */
  previousPositionsPage(): void {
    if (this.positionsPage <= 1) return;
    this.positionsPage--;
    this.loadPositions();
  }

  /** Avanza una página en la tabla de tenencias y vuelve a cargarla. */
  nextPositionsPage(): void {
    if (this.positionsPage >= this.totalPositionsPages) return;
    this.positionsPage++;
    this.loadPositions();
  }

  /**
   * Salta a una página específica de la tabla de tenencias.
   * @param page Número de página de destino.
   */
  goToPositionsPage(page: number): void {
    this.positionsPage = page;
    this.loadPositions();
  }

  /** Vuelve a la primera página de tenencias y recarga la tabla al cambiar el tamaño de página. */
  onPositionsPageSizeChange(): void {
    this.positionsPage = 1;
    this.loadPositions();
  }

  // ── Paginación: navegación de operaciones ──

  /** Retrocede una página en la tabla de operaciones y vuelve a cargarla. */
  previousOperationsPage(): void {
    if (this.operationsPage <= 1) return;
    this.operationsPage--;
    this.loadOperations();
  }

  /** Avanza una página en la tabla de operaciones y vuelve a cargarla. */
  nextOperationsPage(): void {
    if (this.operationsPage >= this.totalOperationsPages) return;
    this.operationsPage++;
    this.loadOperations();
  }

  /**
   * Salta a una página específica de la tabla de operaciones.
   * @param page Número de página de destino.
   */
  goToOperationsPage(page: number): void {
    this.operationsPage = page;
    this.loadOperations();
  }

  /** Vuelve a la primera página de operaciones y recarga la tabla al cambiar el tamaño de página. */
  onOperationsPageSizeChange(): void {
    this.operationsPage = 1;
    this.loadOperations();
  }

  // ── Manejadores de filtros ──

  /** Reinicia la paginación de tenencias a la primera página y recarga la tabla con los filtros actuales. */
  onPositionFiltersChange(): void {
    this.positionsPage = 1;
    this.loadPositions();
  }

  /**
   * Alterna el ordenamiento de la tabla de tenencias al hacer click en un encabezado de columna.
   * Ciclo: sin orden → ascendente → descendente → sin orden.
   * @param field Campo por el cual ordenar (debe coincidir con el nombre esperado por el backend).
   */
  onSortPositions(field: string): void {
    if (this.sortField !== field) {
      this.sortField = field;
      this.sortDirection = 'asc';
    } else if (this.sortDirection === 'asc') {
      this.sortDirection = 'desc';
    } else {
      this.sortField = null;
      this.sortDirection = null;
    }

    this.positionsPage = 1;
    this.loadPositions();
  }

  /**
   * Devuelve el ícono que representa el estado de ordenamiento de una columna de tenencias.
   * @param field Campo de la columna a consultar.
   */
  getSortIcon(field: string): string {
    if (this.sortField !== field) return '↕';
    return this.sortDirection === 'asc' ? '↑' : '↓';
  }

  /** Reinicia la paginación de operaciones a la primera página y recarga la tabla con los filtros actuales. */
  onOperationFiltersChange(): void {
    this.operationsPage = 1;
    this.loadOperations();
  }

  /**
   * Alterna el ordenamiento de la tabla de operaciones al hacer click en un encabezado de columna.
   * Ciclo: sin orden → ascendente → descendente → sin orden.
   * @param field Campo por el cual ordenar (debe coincidir con el nombre esperado por el backend).
   */
  onSortOperations(field: string): void {
    if (this.operationSortField !== field) {
      this.operationSortField = field;
      this.operationSortDirection = 'asc';
    } else if (this.operationSortDirection === 'asc') {
      this.operationSortDirection = 'desc';
    } else {
      this.operationSortField = null;
      this.operationSortDirection = null;
    }

    this.operationsPage = 1;
    this.loadOperations();
  }

  /**
   * Devuelve el ícono que representa el estado de ordenamiento de una columna de operaciones.
   * @param field Campo de la columna a consultar.
   */
  getOperationSortIcon(field: string): string {
    if (this.operationSortField !== field) return '↕';
    return this.operationSortDirection === 'asc' ? '↑' : '↓';
  }

  /**
   * Cambia el período seleccionado para el gráfico de evolución y vuelve a cargarlo.
   * @param event Evento de cambio del `<select>` de período.
   */
  onPeriodChange(event: Event): void {
    this.selectedPeriod = (event.target as HTMLSelectElement).value;
    this.loadLineChart();
  }

  // ── Modales de compra/venta ──

  /**
   * Abre el modal de compra y, si la operación se confirma, ejecuta la compra.
   * @param symbol Símbolo del activo a comprar (opcional, para precargarlo en el modal).
   */
  openBuyModal(symbol?: string): void {
    const dialogRef = this.dialog.open(PortfolioModal, {
      width: '560px',
      maxWidth: '95vw',
      autoFocus: false,
      restoreFocus: false,
      backdropClass: 'blur-backdrop',
      panelClass: 'portfolio-dialog-panel',
      data: { mode: 'buy', symbol, currentBalance: this.portfolioSummary.currentBalance },
    });

    dialogRef.afterClosed().subscribe((result?: PortfolioModalResult) => {
      if (result?.mode === 'buy') this.onBuyComplete(result.data);
    });
  }

  /**
   * Abre el modal de venta para el símbolo indicado y, si la operación se confirma,
   * ejecuta la venta de la posición.
   * @param symbol Símbolo del activo a vender.
   */
  openSellModal(symbol: string): void {
    const dialogRef = this.dialog.open(PortfolioModal, {
      width: '560px',
      maxWidth: '95vw',
      autoFocus: false,
      restoreFocus: false,
      backdropClass: 'blur-backdrop',
      panelClass: 'portfolio-dialog-panel',
      data: { mode: 'sell', symbol },
    });

    dialogRef.afterClosed().subscribe((result?: PortfolioModalResult) => {
      if (result?.mode === 'sell') this.sellPosition(result.data);
    });
  }

  // ── Carga de datos ──

  /** Vuelve a cargar todas las secciones del portfolio: resumen, gráficos, tenencias y operaciones. */
  refreshPortfolio(): void {
    this.loadBalanceCards();
    this.loadPieChart();
    this.loadLineChart();
    this.loadPositions();
    this.loadOperations();
  }

  /** Carga las tarjetas de resumen (saldo inicial/actual, ganancia o pérdida y operaciones del día). */
  loadBalanceCards(): void {
    this.loadingSummary = true;
    this.portfolioService.getBalanceCards(this.activeId!)
      .pipe(finalize(() => this.loadingSummary = false))
      .subscribe({
        next: (res) => {
          const data = res.data!;
          this.portfolioSummary = {
            ...data,
            realizedProfitLoss: data.realizedProfitLoss ?? 0,
            unrealizedProfitLoss: data.unrealizedProfitLoss ?? 0,
          };
        },
        error: (err) => {
          console.error('Balance cards error', err);
        },
      });
  }

  /** Carga el gráfico de distribución (torta), arma el ranking de "Top Activos" y lo renderiza. */
  loadPieChart(): void {
    this.beginChartLoading();
    this.portfolioService.getPieChart(this.activeId!)
      .pipe(finalize(() => this.endChartLoading()))
      .subscribe({
        next: (res) => {
          this.pieChartData = res.data ?? [];
          this.topAssets = this.pieChartData.slice(0, 3).map(x => ({
            ticker: x.symbol,
            percentage: x.percentage,
          }));
          this.renderChartsWhenReady();
        },
        error: (err) => {
          console.error('Pie chart error', err);
        },
      });
  }

  /** Vuelve a cargar únicamente el gráfico de evolución para el período seleccionado. */
  loadLineChart(): void {
    this.beginChartLoading();
    this.portfolioService.getLineChart(this.activeId!, this.selectedPeriod)
      .pipe(finalize(() => this.endChartLoading()))
      .subscribe({
        next: (res) => {
          this.lineChartData = res.data ?? [];
          this.renderChartsWhenReady();
        },
        error: (err) => {
          console.error('Line chart error', err);
        },
      });
  }

  /** Carga la página actual de tenencias (posiciones abiertas) según los filtros y el orden seleccionados. */
  loadPositions(): void {
    this.loadingPositions = true;
    const filter = {
      page: this.positionsPage,
      pageSize: this.positionsPageSize,
      symbol: this.symbolFilter,
      sortBy: this.sortField ?? undefined,
      sortDirection: this.sortDirection ?? undefined,
    };

    this.portfolioService.getOpenPositions(this.activeId!, filter)
      .pipe(finalize(() => this.loadingPositions = false))
      .subscribe({
        next: (res) => {
          this.positions = res.data?.items ?? [];
          this.totalPositions = res.data?.total ?? 0;
          this.updateEmptyHoldingRows();
        },
        error: (err) => {
          console.error('Positions error', err);
        },
      });
  }

  /** Carga la página actual del historial de operaciones según los filtros y el orden seleccionados. */
  loadOperations(): void {
    this.loadingOperations = true;
    const filter: TransactionFilter = {
      page: this.operationsPage,
      pageSize: this.operationsPageSize,
      symbol: this.operationSymbolFilter,
      type: this.operationTypeFilter,
      fromDate: this.operationFromDate || undefined,
      toDate: this.operationToDate || undefined,
      sortBy: this.operationSortField ?? undefined,
      sortDirection: this.operationSortDirection ?? undefined,
    };

    this.portfolioService.getTransactionHistory(this.activeId!, filter)
      .pipe(finalize(() => this.loadingOperations = false))
      .subscribe({
        next: (res) => {
          this.operations = res.data?.data ?? [];
          this.totalOperations = res.data?.total ?? 0;
          this.updateEmptyOperationRows();
        },
        error: (err) => {
          console.error('Operations error', err);
        },
      });
  }

  // ── Exportación a Excel ──

  downloadHoldings(): void {
    if (this.downloadingHoldings) return;
    this.downloadingHoldings = true;
    const filter = {
      page: 1, pageSize: 9999,
      symbol: this.symbolFilter,
      sortBy: this.sortField ?? undefined,
      sortDirection: this.sortDirection ?? undefined,
    };
    this.portfolioService.exportHoldings(this.activeId!, filter)
      .pipe(finalize(() => this.downloadingHoldings = false))
      .subscribe({
        next: (blob) => this.triggerDownload(blob, 'tenencias.xlsx'),
        error: () => this.snackBarService.error(this.languageService.instant('PORTFOLIO.ERRORS.EXPORT_HOLDINGS_FAILED')),
      });
  }

  downloadTransactions(): void {
    if (this.downloadingTransactions) return;
    this.downloadingTransactions = true;
    const filter: TransactionFilter = {
      page: 1, pageSize: 9999,
      symbol: this.operationSymbolFilter,
      type: this.operationTypeFilter,
      fromDate: this.operationFromDate || undefined,
      toDate: this.operationToDate || undefined,
      sortBy: this.operationSortField ?? undefined,
      sortDirection: this.operationSortDirection ?? undefined,
    };
    this.portfolioService.exportTransactions(this.activeId!, filter)
      .pipe(finalize(() => this.downloadingTransactions = false))
      .subscribe({
        next: (blob) => this.triggerDownload(blob, 'operaciones.xlsx'),
        error: () => this.snackBarService.error(this.languageService.instant('PORTFOLIO.ERRORS.EXPORT_TRANSACTIONS_FAILED')),
      });
  }

  private triggerDownload(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  }

  // ── Creación de gráficos ──

  /**
   * Crea (o recrea) el gráfico de torta ("Distribución") en el `<canvas>` del template,
   * a partir de `pieChartData`. No hace nada si el `<canvas>` todavía no está disponible.
   */
  createPieChart(): void {
    const canvas = document.getElementById('pieChart') as HTMLCanvasElement;
    if (!canvas) return;

    this.pieChart?.destroy();

    this.pieChart = new Chart(canvas, {
      type: 'doughnut',
      data: {
        labels: this.pieChartData.map(x => x.symbol),
        datasets: [{
          data: this.pieChartData.map(x => x.currentValue),
          backgroundColor: this.PIE_COLORS,
        }],
      },
      options: {
        cutout: '55%',
        responsive: true,
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (ctx: any) => this.languageService.instant('PORTFOLIO.CHART_VALUE_LABEL', { value: (ctx.parsed as number).toLocaleString('es-ES') }),
            },
          },
        },
      },
    });
  }

  /**
   * Crea (o recrea) el gráfico de línea ("Evolución") en el `<canvas>` del template,
   * a partir de `lineChartData`. Da formato a las fechas de los ejes y de los tooltips
   * según el período seleccionado (corto: día y mes; largo: mes y año). No hace nada
   * si el `<canvas>` todavía no está disponible.
   */
  createLineChart(): void {
    const canvas = document.getElementById('lineChart') as HTMLCanvasElement;
    if (!canvas) return;

    this.lineChart?.destroy();

    const data = this.lineChartData;
    const isShortPeriod = ['7d', '1m', '3m'].includes(this.selectedPeriod);

    const parseLocalDate = (value: string | Date): Date => {
      if (value instanceof Date) return value;
      const s = typeof value === 'string' ? value.substring(0, 10) : String(value);
      const [y, m, d] = s.split('-').map(Number);
      return new Date(y, m - 1, d);
    };

    const formatTick = (value: string | Date) => {
      const d = parseLocalDate(value);
      return isShortPeriod
        ? d.toLocaleDateString('es-ES', { day: 'numeric', month: 'short' })
        : d.toLocaleDateString('es-ES', { month: 'short', year: '2-digit' });
    };

    const formatTooltipTitle = (value: string | Date) => {
      const d = parseLocalDate(value);
      return isShortPeriod
        ? d.toLocaleDateString('es-ES', { day: 'numeric', month: 'long', year: 'numeric' })
        : d.toLocaleDateString('es-ES', { month: 'long', year: 'numeric' });
    };

    const t = this.themeService.getChartTheme();

    const values = data.map(d => d.totalValue).filter(v => v != null && isFinite(v));
    const rawMin = values.length ? Math.min(...values) : 0;
    const rawMax = values.length ? Math.max(...values) : 1;
    const range = rawMax - rawMin || rawMax * 0.1 || 1;
    const padding = range * 0.1;
    const yMin = Math.max(0, rawMin - padding);
    const yMax = rawMax + padding;

    const yTicksLimit = 6;
    const yStep = (yMax - yMin) / (yTicksLimit - 1);
    const yDecimals = yStep >= 1000 ? 0 : yStep >= 100 ? 1 : yStep >= 10 ? 2 : 3;
    const formatYTick = (n: number): string => {
      if (n >= 1000) return `$${(n / 1000).toFixed(yDecimals)}k`;
      return `$${n.toFixed(Math.min(yDecimals, 2))}`;
    };

    const config: ChartConfiguration = {
      type: 'line',
      data: {
        labels: data.map(d => formatTick(d.date)),
        datasets: [{
          label: this.languageService.instant('PORTFOLIO.PERFORMANCE'),
          data: data.map(d => d.totalValue),
          borderColor: '#4ECDC4',
          backgroundColor: 'rgba(78, 205, 196, 0.1)',
          borderWidth: 0,
          fill: true,
          tension: 0.3,
          pointBackgroundColor: '#4ECDC4',
          pointBorderColor: t.legendColor,
          pointBorderWidth: 3,
          pointRadius: 6,
          pointHoverRadius: 8,
          hoverOffset: 10,
        }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { intersect: false, mode: 'index' },
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: t.tooltipBg,
            titleColor: t.tooltipText,
            bodyColor: t.tooltipText,
            borderColor: '#4ECDC4',
            borderWidth: 2,
            padding: 16,
            displayColors: false,
            callbacks: {
              title: (ctx: any) => formatTooltipTitle(data[ctx[0].dataIndex].date),
              label: (ctx: any) => {
                const v = ctx.raw ?? ctx.parsed?.y ?? 0;
                return `Valor: $${Number(v).toLocaleString('es-ES', {
                  minimumFractionDigits: 2,
                  maximumFractionDigits: 2,
                })}`;
              },
            },
          },
        },
        scales: {
          x: {
            display: true,
            grid: { display: false },
            ticks: {
              color: t.textColor,
              font: { size: 11 },
              maxRotation: 0,
              autoSkip: true,
              maxTicksLimit: 4,
              callback: (_: any, index: number) => data[index]?.date ? formatTick(data[index].date) : '',
            },
          },
          y: {
            display: true,
            grid: { color: t.gridColor },
            min: yMin,
            max: yMax,
            ticks: {
              color: t.textColor,
              font: { size: 11 },
              maxTicksLimit: yTicksLimit,
              callback: (v: any) => formatYTick(Number(v)),
            },
          },
        },
        elements: {
          point: { hitRadius: 10, hoverRadius: 8 },
          line: { borderCapStyle: 'round', borderJoinStyle: 'round' },
        },
      },
    };

    this.lineChart = new Chart(canvas, config);
  }

  // ── Acciones de compra / venta ──

  /**
   * Confirma la compra del activo a través del servicio de portfolio, notifica el
   * resultado al usuario y, si fue exitosa, recarga todos los datos del portfolio.
   * @param data Datos de la compra confirmados en el modal (símbolo y cantidad).
   */
  onBuyComplete(data: BuyData): void {
    this.portfolioService.buyAsset(this.activeId!, data.ticker, data.quantity).subscribe({
      next: (res) => {
        this.snackBarService.fromResponse(res.success, res.code, res.message);
        if (res.success) this.refreshPortfolio();
      },
      error: () => this.snackBarService.error(this.languageService.instant('PORTFOLIO.ERRORS.BUY_ERROR')),
    });
  }

  /**
   * Confirma la venta de la posición a través del servicio de portfolio, notifica el
   * resultado al usuario y, si fue exitosa, recarga todos los datos del portfolio.
   * @param data Datos de la venta confirmados en el modal (símbolo y cantidad).
   */
  sellPosition(data: SellData): void {
    this.portfolioService.sell(this.activeId!, data).subscribe({
      next: (res) => {
        this.snackBarService.fromResponse(res.success, res.code, res.message);
        if (res.success) this.refreshPortfolio();
      },
      error: (err) => this.snackBarService.fromResponse(false, err.error?.code, err.error?.message ?? this.languageService.instant('PORTFOLIO.ERRORS.SELL_ERROR')),
    });
  }

  // ── Reinicio de la simulación ──

  /**
   * Abre el diálogo de configuración del portfolio en modo `reset` (con el nombre
   * actual precargado) y, si el usuario confirma, ejecuta el reinicio.
   */
  async confirmResetSimulation(): Promise<void> {
    const dialogRef = this.dialog.open(PortfolioSetupDialog, {
      width: '420px',
      backdropClass: 'blur-backdrop',
      data: {
        mode: 'reset',
        currentName: this.portfolioSummary.portfolioName
      }
    });

    const result: SetupPortfolioRequest | undefined = await dialogRef.afterClosed().toPromise();

    if (!result) return;

    this.resetPortfolio(result);
  }

  /**
   * Reinicia el portfolio activo a través del servicio de portfolio (vuelve al saldo
   * inicial y borra tenencias, operaciones e historial), notifica el resultado al
   * usuario y, si fue exitoso, recarga todos los datos del portfolio.
   */
  resetPortfolio(dto: SetupPortfolioRequest): void {
    this.portfolioService.resetPortfolio(this.activeId!, dto).subscribe({
      next: response => {
        this.snackBarService.fromResponse(response.success, response.code, response.message);
        if (response.success) {
          this.activePortfolioService.refresh().subscribe();
          this.refreshPortfolio();
        }
      },
      error: error => {
        this.snackBarService.fromResponse(false, error.error?.code, error.error?.message ?? this.languageService.instant('PORTFOLIO.ERRORS.RESET_ERROR'));
      }
    });
  }

  // ── Gestión de portfolios (tabs) ──

  /**
   * Cambia el portfolio activo cuando el usuario selecciona otro tab.
   * @param index Índice del tab seleccionado dentro de `portfolios`.
   */
  onTabChange(index: number): void {
    const portfolio = this.portfolios[index];
    if (!portfolio || portfolio.id === this.activeId) return;
    this.activePortfolioService.setActive(portfolio.id);
  }

  /**
   * Abre el diálogo de configuración en modo `add-portfolio` y, si el usuario confirma,
   * crea el nuevo portfolio y lo selecciona como activo.
   */
  async openAddPortfolioDialog(): Promise<void> {
    if (!this.canCreateMore) return;

    const dialogRef = this.dialog.open(PortfolioSetupDialog, {
      width: '420px',
      backdropClass: 'blur-backdrop',
      data: { mode: 'add-portfolio' }
    });

    const result: SetupPortfolioRequest | undefined = await dialogRef.afterClosed().toPromise();

    if (!result) return;

    this.portfolioService.createPortfolio(result).subscribe({
      next: (res) => {
        this.snackBarService.fromResponse(res.success, res.code, res.message);
        if (res.success && res.data) {
          const newId = res.data.id;
          this.activePortfolioService.refresh().subscribe(() => {
            this.activePortfolioService.setActive(newId);
          });
        }
      },
      error: (err) => this.snackBarService.fromResponse(false, err.error?.code, err.error?.message ?? this.languageService.instant('PORTFOLIO.ERRORS.CREATE_ERROR')),
    });
  }

  /**
   * Abre un diálogo de confirmación y, si el usuario confirma, elimina el portfolio
   * activo (no disponible si es el único portfolio del usuario).
   */
  async confirmDeletePortfolio(): Promise<void> {
    if (this.isOnlyPortfolio || this.activeId === null) return;

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '420px',
      backdropClass: 'blur-backdrop',
      data: {
        title: this.languageService.instant('PORTFOLIO.DELETE_DIALOG.TITLE'),
        message: this.languageService.instant('PORTFOLIO.DELETE_DIALOG.MESSAGE', { name: this.portfolioSummary.portfolioName ?? '' }),
        confirmText: this.languageService.instant('PORTFOLIO.DELETE_DIALOG.CONFIRM_BUTTON'),
        cancelText: this.languageService.instant('SHARED.CONFIRM_DIALOG.CANCEL_BUTTON'),
        danger: true,
      }
    });

    const confirmed = await dialogRef.afterClosed().toPromise();

    if (!confirmed || this.activeId === null) return;

    this.portfolioService.deletePortfolio(this.activeId).subscribe({
      next: (res) => {
        this.snackBarService.fromResponse(res.success, res.code, res.message);
        if (res.success) this.activePortfolioService.refresh().subscribe();
      },
      error: (err) => this.snackBarService.fromResponse(false, err.error?.code, err.error?.message ?? this.languageService.instant('PORTFOLIO.ERRORS.DELETE_ERROR')),
    });
  }

  // ── Helpers ──

  /**
   * Color asignado a un activo dentro del ranking de "Top Activos", reciclando la
   * paleta `TOP_ASSET_COLORS` cuando hay más activos que colores disponibles.
   * @param index Posición del activo en el ranking (basada en cero).
   */
  getAssetColor(index: number): string {
    return this.TOP_ASSET_COLORS[index % this.TOP_ASSET_COLORS.length];
  }

  /** Renderiza (o vuelve a renderizar) los gráficos de torta y línea, una vez que la vista está lista. */
  private renderChartsWhenReady(): void {
    if (!this.viewInitialized) return;
    setTimeout(() => {
      this.createPieChart();
      this.createLineChart();
    }, 0);
  }

  /** Marca el inicio de una petición de gráficos, activando el indicador de carga correspondiente. */
  private beginChartLoading(): void {
    this.chartRequestsInProgress++;
    this.loadingCharts = true;
  }

  /** Marca el fin de una petición de gráficos, apagando el indicador de carga cuando no quedan pendientes. */
  private endChartLoading(): void {
    this.chartRequestsInProgress = Math.max(0, this.chartRequestsInProgress - 1);
    this.loadingCharts = this.chartRequestsInProgress > 0;
  }

  /** Recalcula las filas vacías a agregar en la tabla de tenencias para mantener una altura constante. */
  private updateEmptyHoldingRows(): void {
    const missing = Math.max(0, this.positionsPageSize - this.positions.length);
    this.emptyHoldingRows = Array(missing).fill(0);
  }

  /** Recalcula las filas vacías a agregar en la tabla de operaciones para mantener una altura constante. */
  private updateEmptyOperationRows(): void {
    const missing = Math.max(0, this.operationsPageSize - this.operations.length);
    this.emptyOperationRows = Array(missing).fill(0);
  }
}
