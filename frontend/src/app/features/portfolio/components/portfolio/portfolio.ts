import { Component, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize, forkJoin, Subscription } from 'rxjs';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { MatDialog } from '@angular/material/dialog';

import { PortfolioService } from '../../services/portfolio.service';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { MaterialModule } from '../../../../shared/material.module';
import { PortfolioModal } from '../portfolio-modal/portfolio-modal';
import { BuyData, SellData, PortfolioModalResult } from '../../models/portfolio.modal.model';
import {
  PortfolioBalanceCards,
  PortfolioPieChartItem,
  PortfolioLineChartItem,
  TransactionFilter,
  PortfolioTransaction,
} from '../../models/portfolio.model';

@Component({
  selector: 'app-portfolio',
  standalone: true,
  imports: [CommonModule, FormsModule, MaterialModule],
  templateUrl: './portfolio.html',
  styleUrl: './portfolio.css',
})
export class Portfolio implements OnInit, OnDestroy, AfterViewInit {

  // ─── Loading states ──────────────────────────────────────────────────────────
  loadingSummary = false;
  loadingCharts = false;
  loadingPositions = false;
  loadingOperations = false;

  // ─── Summary cards ──────────────────────────────────────────────────────────
  portfolioSummary: PortfolioBalanceCards = {
    initialBalance: 0,
    currentBalance: 0,
    profitLoss: 0,
    profitLossPercent: 0,
    totalOperations: 0,
    maxOperations: 0,
    lastMarketCloseDate: '',
  };

  // ─── Holdings ────────────────────────────────────────────────────────────────
  positions: any[] = [];
  totalPositions: number = 0;
  emptyHoldingRows: number[] = [];

  // Holdings filters & pagination
  symbolFilter: string = '';
  positionStatusFilter: string = '';
  sortBy: string = 'profitLoss';
  sortDirection: string = 'desc';
  positionsPage: number = 1;
  positionsPageSize: number = 5;

  // ─── Operations ──────────────────────────────────────────────────────────────
  operations: PortfolioTransaction[] = [];
  totalOperations: number = 0;
  emptyOperationRows: number[] = [];

  // Operations filters & pagination
  operationSymbolFilter: string = '';
  operationTypeFilter: number | null = null;
  operationDaysFilter: number | null = null;
  operationOrderBy: string = 'date';
  operationsPage: number = 1;
  operationsPageSize: number = 10;

  // ─── UI state ────────────────────────────────────────────────────────────────
  showHoldingsFilters = false;
  showOperationsFilters = false;

  // ─── Charts ──────────────────────────────────────────────────────────────────
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

  // ─── Internal ────────────────────────────────────────────────────────────────
  private chartRequestsInProgress = 0;
  private viewInitialized = false;
  private subscription: Subscription | null = null;

  private readonly PIE_COLORS = [
    '#A9C455', '#4DA3F5', '#FF6E40',
    '#AB47BC', '#26A69A', '#FFA726',
    '#EC407A', '#5C6BC0',
  ];
  private readonly TOP_ASSET_COLORS = ['#FF6384', '#36A2EB', '#FFCE56'];

  constructor(
    private snackBarService: SnackBarService,
    private portfolioService: PortfolioService,
    private dialog: MatDialog,
  ) {
    Chart.register(...registerables);
  }

  // ─── Lifecycle ───────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.refreshPortfolioData();
  }

  ngAfterViewInit(): void {
    this.viewInitialized = true;
    this.renderChartsWhenReady();
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
    this.pieChart?.destroy();
    this.lineChart?.destroy();
  }

  // ─── Computed: total pages ───────────────────────────────────────────────────

  get totalPositionsPages(): number {
    return Math.max(1, Math.ceil(this.totalPositions / this.positionsPageSize));
  }

  get totalOperationsPages(): number {
    return Math.max(1, Math.ceil(this.totalOperations / this.operationsPageSize));
  }

  // ─── Pagination: page number arrays ─────────────────────────────────────────

  getPositionPageNumbers(): number[] { return this.buildPageNumbers(this.positionsPage, this.totalPositionsPages); }
  getOperationPageNumbers(): number[] { return this.buildPageNumbers(this.operationsPage, this.totalOperationsPages); }

  private buildPageNumbers(current: number, total: number): number[] {
    const maxVisible = 5;
    let start = Math.max(1, current - Math.floor(maxVisible / 2));
    let end = Math.min(total, start + maxVisible - 1);
    if (end - start + 1 < maxVisible) start = Math.max(1, end - maxVisible + 1);
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  }

  // ─── Pagination: holdings navigation ────────────────────────────────────────

  previousPositionsPage(): void {
    if (this.positionsPage <= 1) return;
    this.positionsPage--;
    this.loadPositions();
  }

  nextPositionsPage(): void {
    if (this.positionsPage >= this.totalPositionsPages) return;
    this.positionsPage++;
    this.loadPositions();
  }

  goToPositionsPage(page: number): void {
    this.positionsPage = page;
    this.loadPositions();
  }

  onPositionsPageSizeChange(): void {
    this.positionsPage = 1;
    this.loadPositions();
  }

  // ─── Pagination: operations navigation ──────────────────────────────────────

  previousOperationsPage(): void {
    if (this.operationsPage <= 1) return;
    this.operationsPage--;
    this.loadOperations();
  }

  nextOperationsPage(): void {
    if (this.operationsPage >= this.totalOperationsPages) return;
    this.operationsPage++;
    this.loadOperations();
  }

  goToOperationsPage(page: number): void {
    this.operationsPage = page;
    this.loadOperations();
  }

  onOperationsPageSizeChange(): void {
    this.operationsPage = 1;
    this.loadOperations();
  }

  // ─── Filter handlers ─────────────────────────────────────────────────────────

  onPositionFiltersChange(): void {
    this.positionsPage = 1;
    this.loadPositions();
  }

  onOperationFiltersChange(): void {
    this.operationsPage = 1;
    this.loadOperations();
  }

  onPeriodChange(event: Event): void {
    this.selectedPeriod = (event.target as HTMLSelectElement).value;
    this.loadLineChart();
  }

  // ─── Modal handlers ──────────────────────────────────────────────────────────

  openBuyModal(symbol?: string): void {
    const dialogRef = this.dialog.open(PortfolioModal, {
      width: '560px',
      maxWidth: '95vw',
      autoFocus: false,
      restoreFocus: false,
      backdropClass: 'blur-backdrop',
      panelClass: 'portfolio-dialog-panel',
      data: { mode: 'buy', symbol },
    });

    dialogRef.afterClosed().subscribe((result?: PortfolioModalResult) => {
      if (result?.mode === 'buy') this.onBuyComplete(result.data);
    });
  }

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

  // ─── Data loading ────────────────────────────────────────────────────────────

  private refreshPortfolioData(): void {
    this.loadBalanceCards();
    this.loadCharts();
    this.loadPositions();
    this.loadOperations();
  }

  loadBalanceCards(): void {
    this.loadingSummary = true;
    this.portfolioService.getBalanceCards()
      .pipe(finalize(() => this.loadingSummary = false))
      .subscribe({
        next: (res) => { this.portfolioSummary = res.data!; },
        error: (err) => {
          console.error('Balance cards error', err);
        },
      });
  }

  loadCharts(): void {
    this.beginChartLoading();
    forkJoin({
      pie: this.portfolioService.getPieChart(),
      line: this.portfolioService.getLineChart(this.selectedPeriod),
    })
      .pipe(finalize(() => this.endChartLoading()))
      .subscribe({
        next: (res) => {
          this.pieChartData = res.pie.data ?? [];
          this.lineChartData = res.line.data ?? [];
          this.topAssets = this.pieChartData.slice(0, 3).map(x => ({
            ticker: x.symbol,
            percentage: x.percentage,
          }));
          this.renderChartsWhenReady();
        },
        error: (err) => {
          console.error('Charts error', err);
        },
      });
  }

  loadLineChart(): void {
    this.beginChartLoading();
    this.portfolioService.getLineChart(this.selectedPeriod)
      .pipe(finalize(() => this.endChartLoading()))
      .subscribe({
        next: (res) => {
          this.lineChartData = res.data ?? [];
          this.renderChartsWhenReady();
        },
        error: (err) => {
          console.error('Line chart error', err);
          this.snackBarService.error('No se pudo cargar la evolución del portfolio');
        },
      });
  }

  loadPositions(): void {
    this.loadingPositions = true;
    const filter = {
      page: this.positionsPage,
      pageSize: this.positionsPageSize,
      symbol: this.symbolFilter,
      status: this.positionStatusFilter,
      sortBy: this.sortBy,
      sortDirection: this.sortDirection,
    };

    this.portfolioService.getOpenPositions(filter)
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

  loadOperations(): void {
    this.loadingOperations = true;
    const filter: TransactionFilter = {
      page: this.operationsPage,
      pageSize: this.operationsPageSize,
      symbol: this.operationSymbolFilter,
      type: this.operationTypeFilter,
      days: this.operationDaysFilter,
      orderBy: this.operationOrderBy,
    };

    this.portfolioService.getTransactionHistory(filter)
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

  // ─── Chart creation ──────────────────────────────────────────────────────────

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
              label: (ctx: any) => `Valor: $${(ctx.parsed as number).toLocaleString('es-ES')}`,
            },
          },
        },
      },
    });
  }

  createLineChart(): void {
    const canvas = document.getElementById('lineChart') as HTMLCanvasElement;
    if (!canvas) return;

    this.lineChart?.destroy();

    const data = this.lineChartData;
    const isShortPeriod = ['7d', '1m', '3m'].includes(this.selectedPeriod);

    const formatTick = (value: string | Date) => {
      const d = value instanceof Date ? value : new Date(value);
      return isShortPeriod
        ? d.toLocaleDateString('es-ES', { day: 'numeric', month: 'short' })
        : d.toLocaleDateString('es-ES', { month: 'short', year: '2-digit' });
    };

    const formatTooltipTitle = (value: string | Date) => {
      const d = value instanceof Date ? value : new Date(value);
      return isShortPeriod
        ? d.toLocaleDateString('es-ES', { day: 'numeric', month: 'long', year: 'numeric' })
        : d.toLocaleDateString('es-ES', { month: 'long', year: 'numeric' });
    };

    const config: ChartConfiguration = {
      type: 'line',
      data: {
        labels: data.map(d => formatTick(d.date)),
        datasets: [{
          label: 'Portfolio Value',
          data: data.map(d => d.totalValue),
          borderColor: '#4ECDC4',
          backgroundColor: 'rgba(78, 205, 196, 0.1)',
          borderWidth: 0,
          fill: true,
          tension: 0.3,
          pointBackgroundColor: '#4ECDC4',
          pointBorderColor: '#FFFFFF',
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
            backgroundColor: 'rgba(0,0,0,0.8)',
            titleColor: '#FFFFFF',
            bodyColor: '#FFFFFF',
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
              color: '#FFFFFF',
              font: { size: 11 },
              maxRotation: 0,
              autoSkip: true,
              maxTicksLimit: 4,
              callback: (_: any, index: number) => data[index]?.date ? formatTick(data[index].date) : '',
            },
          },
          y: {
            display: true,
            grid: { color: 'rgba(255,255,255,0.1)' },
            ticks: {
              color: '#FFFFFF',
              font: { size: 11 },
              callback: (v: any) => `$${(Number(v) / 1000).toFixed(1)}k`,
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

  // ─── Buy / Sell actions ──────────────────────────────────────────────────────

  onBuyComplete(data: BuyData): void {
    this.portfolioService.buyAsset(data.ticker, data.quantity).subscribe({
      next: (res) => {
        if (res.success) {
          this.snackBarService.success(res.message || 'Compra realizada correctamente');
          this.refreshPortfolioData();
        } else {
          this.snackBarService.info(res.message || 'No se pudo realizar la compra');
        }
      },
      error: () => this.snackBarService.error('Error al realizar la compra'),
    });
  }

  sellPosition(data: SellData): void {
    this.portfolioService.sell(data).subscribe({
      next: (res) => {
        if (res.success) {
          this.snackBarService.success(res.message || 'Venta realizada correctamente');
          this.refreshPortfolioData();
        } else {
          this.snackBarService.info(res.message || 'No se pudo realizar la venta');
        }
      },
      error: (err) => this.snackBarService.error(err.error?.message ?? 'Error al vender activo'),
    });
  }

  // ─── Helpers ─────────────────────────────────────────────────────────────────

  getAssetColor(index: number): string {
    return this.TOP_ASSET_COLORS[index % this.TOP_ASSET_COLORS.length];
  }

  private renderChartsWhenReady(): void {
    if (!this.viewInitialized) return;
    setTimeout(() => {
      this.createPieChart();
      this.createLineChart();
    }, 0);
  }

  private beginChartLoading(): void {
    this.chartRequestsInProgress++;
    this.loadingCharts = true;
  }

  private endChartLoading(): void {
    this.chartRequestsInProgress = Math.max(0, this.chartRequestsInProgress - 1);
    this.loadingCharts = this.chartRequestsInProgress > 0;
  }

  private updateEmptyHoldingRows(): void {
    const missing = Math.max(0, this.positionsPageSize - this.positions.length);
    this.emptyHoldingRows = Array(missing).fill(0);
  }

  private updateEmptyOperationRows(): void {
    const missing = Math.max(0, this.operationsPageSize - this.operations.length);
    this.emptyOperationRows = Array(missing).fill(0);
  }

  async confirmResetSimulation(): Promise<void> {
    const ConfirmDialog = await import('../../../../shared/confirm-dialog/confirm-dialog.component');

    const dialogRef = this.dialog.open(ConfirmDialog.ConfirmDialogComponent, {
      width: '420px',
      backdropClass: 'blur-backdrop',
      data: {
        title: 'Reiniciar portfolio',
        message: '¿Estás segura de que querés reiniciar tu portfolio? Se eliminarán tus tenencias, operaciones e historial de evolución. Tu saldo volverá al monto inicial.'
      }
    });

    const result = await dialogRef.afterClosed().toPromise();

    if (!result) return;

    this.resetSimulation();
  }

  resetSimulation(): void {
    this.portfolioService.resetSimulation().subscribe({
      next: response => {
        if (response.success) {
          this.snackBarService.success(response.message || 'Portfolio reiniciado correctamente');
          this.refreshPortfolioData();
          return;
        }

        this.snackBarService.info(response.message || 'No se pudo reiniciar el portfolio');
      },
      error: error => {
        this.snackBarService.error(error.error?.message ?? 'Error al reiniciar el portfolio');
      }
    });
  }
}