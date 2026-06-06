import { Component, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize, forkJoin, Subscription } from 'rxjs';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { PortfolioService } from '../../services/portfolio.service';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { MaterialModule } from '../../../../shared/material.module';
import { MatDialog } from '@angular/material/dialog';
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
  styleUrl: './portfolio.css'
})
export class Portfolio implements OnInit, OnDestroy, AfterViewInit {

  loadingSummary = false;
  loadingCharts = false;
  loadingPositions = false;
  loadingOperations = false;

  private chartRequestsInProgress = 0;
  private viewInitialized = false;

  portfolioSummary: PortfolioBalanceCards = {
    initialBalance: 0,
    currentBalance: 0,
    profitLoss: 0,
    profitLossPercent: 0,
    totalOperations: 0,
    maxOperations: 0,
    lastMarketCloseDate: ''
  };

  symbolFilter = '';
  positionStatusFilter = '';
  sortBy = 'profitLoss';
  sortDirection = 'desc';
  positionsPage = 1;
  positionsPageSize = 5;

  positions: any[] = [];
  totalPositions = 0;
  totalOperations = 0;
  emptyOperationRows: number[] = [];
  emptyHoldingRows: number[] = [];

  operationsPage = 1;
  operationsPageSize = 10;
  operationSymbolFilter = '';
  operationTypeFilter: number | null = null;
  operationDaysFilter: number | null = null;
  operationOrderBy = 'date';

  operations: PortfolioTransaction[] = [];

  topAssets: Array<{ ticker: string, percentage: number }> = [];

  pieChartData: PortfolioPieChartItem[] = [];
  lineChartData: PortfolioLineChartItem[] = [];
  pieChart: Chart | null = null;
  lineChart: Chart | null = null;

  selectedPeriod = '1m';

  timePeriods = [
    { value: '7d', label: '7 días' },
    { value: '1m', label: '1 mes' },
    { value: '3m', label: '3 meses' },
    { value: '6m', label: '6 meses' },
    { value: '1y', label: '1 año' }
  ];

  private subscription: Subscription | null = null;

  constructor(
    private snackBarService: SnackBarService,
    private portfolioService: PortfolioService,
    private dialog: MatDialog
  ) {
    Chart.register(...registerables);
  }

  ngOnInit(): void {
    this.refreshPortfolioData();
  }

  ngAfterViewInit(): void {
    this.viewInitialized = true;
    this.renderChartsWhenReady();
  }

  ngOnDestroy(): void {
    if (this.subscription) this.subscription.unsubscribe();
    if (this.pieChart) this.pieChart.destroy();
    if (this.lineChart) this.lineChart.destroy();
  }

  get totalPositionsPages(): number {
    return Math.max(1, Math.ceil(this.totalPositions / this.positionsPageSize));
  }

  get totalOperationsPages(): number {
    return Math.max(1, Math.ceil(this.totalOperations / this.operationsPageSize));
  }

  getPositionPageNumbers(): number[] {
    return this.buildPageNumbers(this.positionsPage, this.totalPositionsPages);
  }

  getOperationPageNumbers(): number[] {
    return this.buildPageNumbers(this.operationsPage, this.totalOperationsPages);
  }

  private buildPageNumbers(current: number, total: number): number[] {
    const maxVisible = 5;
    let start = Math.max(1, current - Math.floor(maxVisible / 2));
    let end = Math.min(total, start + maxVisible - 1);

    if (end - start + 1 < maxVisible) {
      start = Math.max(1, end - maxVisible + 1);
    }

    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  }

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

  loadBalanceCards(): void {
    this.loadingSummary = true;

    this.portfolioService.getBalanceCards()
      .pipe(finalize(() => this.loadingSummary = false))
      .subscribe({
        next: response => {
          if (!response.success || !response.data) {
            this.portfolioSummary = {
              initialBalance: 0,
              currentBalance: 0,
              profitLoss: 0,
              profitLossPercent: 0,
              totalOperations: 0,
              maxOperations: 0,
              lastMarketCloseDate: ''
            };

            return;
          }

          this.portfolioSummary = response.data;
        },
        error: error => {
          console.error('Error cargando resumen del portfolio', error);

          this.portfolioSummary = {
            initialBalance: 0,
            currentBalance: 0,
            profitLoss: 0,
            profitLossPercent: 0,
            totalOperations: 0,
            maxOperations: 0,
            lastMarketCloseDate: ''
          };
        }
      });
  }

  loadCharts(): void {
    this.beginChartLoading();

    forkJoin({
      pie: this.portfolioService.getPieChart(),
      line: this.portfolioService.getLineChart(this.selectedPeriod)
    })
      .pipe(finalize(() => this.endChartLoading()))
      .subscribe({
        next: response => {
          this.pieChartData = response.pie?.data ?? [];
          this.lineChartData = response.line?.data ?? [];

          this.topAssets = this.pieChartData.slice(0, 3).map(x => ({
            ticker: x.symbol,
            percentage: x.percentage
          }));

          this.renderChartsWhenReady();
        },
        error: error => {
          console.error('Error cargando gráficos del portfolio', error);

          this.pieChartData = [];
          this.lineChartData = [];
          this.topAssets = [];

          this.renderChartsWhenReady();
        }
      });
  }

  loadPieChart(): void {
    this.beginChartLoading();

    this.portfolioService.getPieChart()
      .pipe(finalize(() => this.endChartLoading()))
      .subscribe({
        next: response => {
          if (!response.success || !response.data) {
            this.pieChartData = [];
            this.topAssets = [];
            this.renderChartsWhenReady();
            return;
          }

          this.pieChartData = response.data;

          this.topAssets = this.pieChartData.slice(0, 3).map(x => ({
            ticker: x.symbol,
            percentage: x.percentage
          }));

          this.renderChartsWhenReady();
        },
        error: error => {
          console.error('Error cargando distribución del portfolio', error);

          this.pieChartData = [];
          this.topAssets = [];

          this.renderChartsWhenReady();
        }
      });
  }

  loadLineChart(): void {
    this.beginChartLoading();

    this.portfolioService.getLineChart(this.selectedPeriod)
      .pipe(finalize(() => this.endChartLoading()))
      .subscribe({
        next: response => {
          if (!response.success || !response.data) {
            this.lineChartData = [];
            this.renderChartsWhenReady();
            return;
          }

          this.lineChartData = response.data;
          this.renderChartsWhenReady();
        },
        error: error => {
          console.error('Error cargando evolución del portfolio', error);

          this.lineChartData = [];

          this.renderChartsWhenReady();
        }
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
      sortDirection: this.sortDirection
    };

    this.portfolioService.getOpenPositions(filter)
      .pipe(finalize(() => this.loadingPositions = false))
      .subscribe({
        next: response => {
          if (!response.success || !response.data) {
            this.positions = [];
            this.totalPositions = 0;
            this.updateEmptyHoldingRows();
            return;
          }

          this.positions = response.data.items ?? [];
          this.totalPositions = response.data.total ?? 0;
          this.updateEmptyHoldingRows();
        },
        error: error => {
          console.error('Error cargando tenencias', error);
          this.positions = [];
          this.totalPositions = 0;
          this.updateEmptyHoldingRows();
        }
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
      orderBy: this.operationOrderBy
    };

    this.portfolioService.getTransactionHistory(filter)
      .pipe(finalize(() => this.loadingOperations = false))
      .subscribe({
        next: response => {
          if (!response.success || !response.data) {
            this.operations = [];
            this.totalOperations = 0;
            this.updateEmptyOperationRows();
            return;
          }

          this.operations = response.data.data ?? [];
          this.totalOperations = response.data.total ?? 0;
          this.updateEmptyOperationRows();
        },
        error: error => {
          console.error('Error cargando operaciones', error);
          this.operations = [];
          this.totalOperations = 0;
          this.updateEmptyOperationRows();
        }
      });
  }

  onPeriodChange(event: Event): void {
    this.selectedPeriod = (event.target as HTMLSelectElement).value;
    this.loadLineChart();
  }

  onPositionFiltersChange(): void {
    this.positionsPage = 1;
    this.loadPositions();
  }

  onOperationFiltersChange(): void {
    this.operationsPage = 1;
    this.loadOperations();
  }

  openBuyModal(symbol?: string): void {
    const dialogRef = this.dialog.open(PortfolioModal, {
      width: '560px',
      maxWidth: '95vw',
      autoFocus: false,
      restoreFocus: false,
      backdropClass: 'blur-backdrop',
      panelClass: 'portfolio-dialog-panel',
      data: {
        mode: 'buy',
        symbol
      }
    });

    dialogRef.afterClosed().subscribe((result?: PortfolioModalResult) => {
      if (!result) return;
      if (result.mode === 'buy') this.onBuyComplete(result.data);
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
      data: {
        mode: 'sell',
        symbol
      }
    });

    dialogRef.afterClosed().subscribe((result?: PortfolioModalResult) => {
      if (!result) return;
      if (result.mode === 'sell') this.sellPosition(result.data);
    });
  }

  initializeCharts(): void {
    this.createPieChart();
    this.createLineChart();
  }

  createPieChart(): void {
    const canvas = document.getElementById('pieChart') as HTMLCanvasElement;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    if (this.pieChart) this.pieChart.destroy();

    this.pieChart = new Chart(ctx, {
      type: 'doughnut',
      data: {
        labels: this.pieChartData.map(x => x.symbol),
        datasets: [
          {
            data: this.pieChartData.map(x => x.currentValue),
            backgroundColor: [
              '#A9C455',
              '#4DA3F5',
              '#FF6E40',
              '#AB47BC',
              '#26A69A',
              '#FFA726',
              '#EC407A',
              '#5C6BC0'
            ]
          }
        ]
      },
      options: {
        cutout: '55%',
        responsive: true,
        plugins: {
          legend: { display: false },
          tooltip: {
            enabled: true,
            callbacks: {
              label: (context: any) => {
                const value = context.parsed as number;
                return `Valor: $${value.toLocaleString('es-ES')}`;
              }
            }
          }
        }
      }
    });
  }

  createLineChart(): void {
    const canvas = document.getElementById('lineChart') as HTMLCanvasElement;
    if (!canvas) return;

    if (this.lineChart) this.lineChart.destroy();

    const data = this.lineChartData;

    const config: ChartConfiguration = {
      type: 'line',
      data: {
        labels: data.map(d => new Date(d.date).toLocaleDateString('es-AR')),
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
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: {
          intersect: false,
          mode: 'index'
        },
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: 'rgba(0, 0, 0, 0.8)',
            titleColor: '#FFFFFF',
            bodyColor: '#FFFFFF',
            borderColor: '#4ECDC4',
            borderWidth: 2,
            padding: 16,
            displayColors: false,
            callbacks: {
              title: (context: any) => {
                const index = context[0].dataIndex;
                const date = new Date(data[index].date);

                if (this.selectedPeriod === '7d' || this.selectedPeriod === '1m' || this.selectedPeriod === '3m') {
                  return date.toLocaleDateString('es-ES', {
                    day: 'numeric',
                    month: 'long',
                    year: 'numeric'
                  });
                }

                return date.toLocaleDateString('es-ES', {
                  month: 'long',
                  year: 'numeric'
                });
              },
              label: (context: any) => {
                const value = context.raw ?? context.parsed?.y ?? 0;
                return `Valor: $${Number(value).toLocaleString('es-ES', {
                  minimumFractionDigits: 2,
                  maximumFractionDigits: 2
                })}`;
              }
            }
          }
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
              callback: (value: any, index: any) => {
                const date = new Date(data[index].date);

                if (this.selectedPeriod === '7d' || this.selectedPeriod === '1m' || this.selectedPeriod === '3m') {
                  return date.toLocaleDateString('es-ES', {
                    day: 'numeric',
                    month: 'short'
                  });
                }

                return date.toLocaleDateString('es-ES', {
                  month: 'short',
                  year: '2-digit'
                });
              }
            }
          },
          y: {
            display: true,
            grid: { color: 'rgba(255, 255, 255, 0.1)' },
            ticks: {
              color: '#FFFFFF',
              font: { size: 11 },
              callback: (value: any) => {
                return `$${(value as number / 1000).toFixed(1)}k`;
              }
            }
          }
        },
        elements: {
          point: { hitRadius: 10, hoverRadius: 8 },
          line: { borderCapStyle: 'round', borderJoinStyle: 'round' }
        }
      }
    };

    this.lineChart = new Chart(canvas, config);
  }

  getAssetColor(index: number): string {
    const colors = ['#FF6384', '#36A2EB', '#FFCE56'];
    return colors[index % colors.length];
  }

  onBuyComplete(data: BuyData): void {
    this.portfolioService.buyAsset(data.ticker, data.quantity).subscribe({
      next: response => {
        if (response.success) {
          this.snackBarService.success(response.message || 'Compra realizada correctamente');
          this.refreshPortfolioData();
        } else {
          this.snackBarService.info(response.message || 'No se pudo realizar la compra');
        }
      },
      error: () => {
        this.snackBarService.error('Error al realizar la compra');
      }
    });
  }

  sellPosition(data: SellData): void {
    this.portfolioService.sell(data).subscribe({
      next: response => {
        if (response.success) {
          this.snackBarService.success(response.message || 'Venta realizada correctamente');
          this.refreshPortfolioData();
        } else {
          this.snackBarService.info(response.message || 'No se pudo realizar la venta');
        }
      },
      error: error => {
        this.snackBarService.error(error.error?.message ?? 'Error al vender activo');
      }
    });
  }

  private refreshPortfolioData(): void {
    this.loadBalanceCards();
    this.loadCharts();
    this.loadPositions();
    this.loadOperations();
  }

  private beginChartLoading(): void {
    this.chartRequestsInProgress++;
    this.loadingCharts = true;
  }

  private endChartLoading(): void {
    this.chartRequestsInProgress = Math.max(0, this.chartRequestsInProgress - 1);
    this.loadingCharts = this.chartRequestsInProgress > 0;
  }

  private renderChartsWhenReady(): void {
    if (!this.viewInitialized) return;

    setTimeout(() => {
      this.createPieChart();
      this.createLineChart();
    }, 0);
  }

  private updateEmptyHoldingRows(): void {
    const missing = Math.max(0, this.positionsPageSize - this.positions.length);
    this.emptyHoldingRows = Array(missing).fill(0);
  }

  private updateEmptyOperationRows(): void {
    const missing = Math.max(0, this.operationsPageSize - this.operations.length);
    this.emptyOperationRows = Array(missing).fill(0);
  }
}