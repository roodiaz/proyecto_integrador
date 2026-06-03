import { Component, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PageEvent } from '@angular/material/paginator';
import { Subscription } from 'rxjs';
import { Chart, ChartConfiguration, ChartType, registerables } from 'chart.js';
import { PortfolioService } from '../../services/portfolio.service';
import { PortfolioModal } from '../portfolio-modal/portfolio-modal';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { MaterialModule } from '../../../../shared/material.module';
import { BuyData, SellData } from '../../models/portfolio.modal.model';

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
  imports: [CommonModule, FormsModule, MaterialModule, PortfolioModal],
  templateUrl: './portfolio.html',
  styleUrl: './portfolio.css'
})
export class Portfolio implements OnInit, OnDestroy, AfterViewInit {

  // Top Cards
  portfolioSummary: PortfolioBalanceCards = {
    initialBalance: 0,
    currentBalance: 0,
    profitLoss: 0,
    profitLossPercent: 0,
    totalOperations: 0,
    maxOperations: 0,
    lastMarketCloseDate: ''
  };

  // Filters Holdings
  symbolFilter = '';
  positionStatusFilter = '';
  sortBy = 'profitLoss';
  sortDirection = 'desc';
  positionsPage = 1;
  positionsPageSize = 5;

  // Holdings
  positions: any[] = [];
  totalPositions = 0;
  totalOperations = 0;
  emptyOperationRows: number[] = [];
  emptyHoldingRows: number[] = [];

  // Filter Operations
  operationsPage = 1;
  operationsPageSize = 10;
  operationSymbolFilter = '';
  operationTypeFilter: number | null = null;
  operationDaysFilter: number | null = null;
  operationOrderBy = 'date';

  // Operations
  operations: PortfolioTransaction[] = [];

  // Modal controls
  showBuyModal: boolean = false;
  showSellModal: boolean = false;
  selectedSymbol = '';

  // Top assets data
  topAssets: Array<{ ticker: string, percentage: number }> = [];

  // Chart.js instances
  pieChartData: PortfolioPieChartItem[] = [];
  lineChartData: PortfolioLineChartItem[] = [];
  pieChart: Chart | null = null;
  lineChart: Chart | null = null;

  // Time period selector
  selectedPeriod: string = '1m';
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
    private portfolioService: PortfolioService
  ) {
    // Register Chart.js components
    Chart.register(...registerables);
  }

  ngOnInit(): void {
    this.refreshPortfolioData();
  }

  ngOnDestroy(): void {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }

  ngAfterViewInit(): void {
    // Inicializar gráficos después de que la vista se renderice
    setTimeout(() => {
      this.initializeCharts();
    }, 100);
  }

  loadOperations(): void {

    const filter: TransactionFilter = {
      page: this.operationsPage,
      pageSize: this.operationsPageSize,
      symbol: this.operationSymbolFilter,
      type: this.operationTypeFilter,
      days: this.operationDaysFilter,
      orderBy: this.operationOrderBy
    };

    this.portfolioService
      .getTransactionHistory(filter)
      .subscribe({

        next: (response) => {

          this.operations = response.data?.data ?? [];
          this.totalOperations = response.data?.total ?? 0;
          this.updateEmptyOperationRows();

        },

        error: (error) => { console.error(error); }

      });

  }

  loadBalanceCards(): void {

    this.portfolioService
      .getBalanceCards()
      .subscribe({

        next: (response) => {
          this.portfolioSummary = response.data!;
        },

        error: (error) => {
          console.error('Error loading balance cards', error);
        }
      });

  }

  onPeriodChange(event: Event): void {
    this.selectedPeriod =
      (event.target as HTMLSelectElement).value;

    this.loadLineChart();
  }

  // Modal methods
  openBuyModal(): void {
    this.showBuyModal = true;
  }

  openSellModal(symbol: string): void {
    this.selectedSymbol = symbol;
    this.showSellModal = true;
  }

  closeBuyModal(): void {
    this.showBuyModal = false;
  }

  closeSellModal(): void {
    this.showSellModal = false;
    this.selectedSymbol = '';
  }

  initializeCharts(): void {
    this.createPieChart();
    this.createLineChart();
  }

  loadPieChart(): void {
    this.portfolioService
      .getPieChart()
      .subscribe({

        next: (response) => {

          this.pieChartData = response.data!;
          this.topAssets = this.pieChartData
            .slice(0, 3)
            .map(x => ({
              ticker: x.symbol,
              percentage: x.percentage
            }));

          this.createPieChart();

        },

        error: (error) => {
          console.error(error);
        }

      });

  }

  loadLineChart(): void {

    this.portfolioService
      .getLineChart(this.selectedPeriod)
      .subscribe({

        next: (response) => {

          this.lineChartData = response.data!;

          this.createLineChart();

        },

        error: (error) => {
          console.error(error);
        }

      });

  }

  createPieChart(): void {

    const canvas = document.getElementById('pieChart') as HTMLCanvasElement;

    if (!canvas)
      return;

    const ctx = canvas.getContext('2d');

    if (!ctx)
      return;

    if (this.pieChart)
      this.pieChart.destroy();

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

          legend: {
            display: false
          },

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

    // Destroy existing chart if it exists
    if (this.lineChart) {
      this.lineChart.destroy();
    }

    // Generar datos según el período seleccionado
    const data = this.lineChartData;

    const config: ChartConfiguration = {
      type: 'line',
      data: {
        labels: data.map(d =>
          new Date(d.date).toLocaleDateString('es-AR')
        ),
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
          legend: {
            display: false
          },
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

                const date =
                  new Date(data[index].date);

                if (
                  this.selectedPeriod === '7d' ||
                  this.selectedPeriod === '1m' ||
                  this.selectedPeriod === '3m'
                ) {
                  return date.toLocaleDateString(
                    'es-ES',
                    {
                      day: 'numeric',
                      month: 'long',
                      year: 'numeric'
                    });
                }

                return date.toLocaleDateString(
                  'es-ES',
                  {
                    month: 'long',
                    year: 'numeric'
                  });
              },
              label: (context: any) => {

                const value =
                  context.raw ??
                  context.parsed?.y ??
                  0;

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
            grid: {
              display: false
            },
            ticks: {
              color: '#FFFFFF',
              font: {
                size: 11
              },
              maxRotation: 0,
              autoSkip: true,
              maxTicksLimit: 4,
              callback: (value: any, index: any) => {

                const date =
                  new Date(data[index].date);

                if (
                  this.selectedPeriod === '7d' ||
                  this.selectedPeriod === '1m' ||
                  this.selectedPeriod === '3m'
                ) {

                  return date.toLocaleDateString(
                    'es-ES',
                    {
                      day: 'numeric',
                      month: 'short'
                    });

                }

                return date.toLocaleDateString(
                  'es-ES',
                  {
                    month: 'short',
                    year: '2-digit'
                  });
              }
            }
          },
          y: {
            display: true,
            grid: {
              color: 'rgba(255, 255, 255, 0.1)'
            },
            ticks: {
              color: '#FFFFFF',
              font: {
                size: 11
              },
              callback: (value: any) => {
                return `$${(value as number / 1000).toFixed(1)}k`;
              }
            }
          }
        },
        elements: {
          point: {
            hitRadius: 10,
            hoverRadius: 8
          },
          line: {
            borderCapStyle: 'round',
            borderJoinStyle: 'round'
          }
        }
      }
    };

    this.lineChart = new Chart(canvas, config);
  }

  getAssetColor(index: number): string {
    const colors = ['#FF6384', '#36A2EB', '#FFCE56'];
    return colors[index % colors.length];
  }

  onPositionsPageChange(event: PageEvent): void {

    this.positionsPage = event.pageIndex + 1;
    this.positionsPageSize = event.pageSize;

    this.loadPositions();

  }

  onOperationsPageChange(event: PageEvent): void {

    this.operationsPage = event.pageIndex + 1;
    this.operationsPageSize = event.pageSize;
    this.loadOperations();

  }

  onOperationFiltersChange(): void {

    this.operationsPage = 1;
    this.loadOperations();

  }

  loadPositions(): void {

    const filter = {

      page: this.positionsPage,
      pageSize: this.positionsPageSize,

      symbol: this.symbolFilter,

      status: this.positionStatusFilter,

      sortBy: this.sortBy,

      sortDirection: this.sortDirection

    };

    this.portfolioService
      .getOpenPositions(filter)
      .subscribe({

        next: (response) => {

          this.positions = response.data?.items ?? [];
          this.totalPositions = response.data?.total ?? 0;
          this.updateEmptyHoldingRows();
        },

        error: (error) => {
          console.error(error);
        }

      });

  }

  onPositionFiltersChange(): void {

    this.positionsPage = 1;

    this.loadPositions();

  }

  private updateEmptyHoldingRows(): void {

    const missing =
      Math.max(
        0,
        this.positionsPageSize - this.positions.length
      );

    this.emptyHoldingRows = Array(missing).fill(0);
  }

  private updateEmptyOperationRows(): void {

    const missing =
      Math.max(
        0,
        this.operationsPageSize - this.operations.length
      );

    this.emptyOperationRows = Array(missing).fill(0);
  }

  onBuyComplete(data: BuyData): void {
    this.portfolioService.buyAsset(data.ticker, data.quantity).subscribe({
      next: (response) => {
        if (response.success) {
          this.snackBarService.success(response.message || 'Compra realizada correctamente');

          this.closeBuyModal();

          this.loadBalanceCards();
          this.loadPieChart();
          this.loadPositions();
          this.loadOperations();
        }
        else {
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
      next: (response) => {
        if (response.success) {
          this.snackBarService.success('Venta realizada correctamente');
          this.closeSellModal();
          this.refreshPortfolioData();
        }
      },
      error: (error) => {
        this.snackBarService.error(error.error?.message ?? 'Error al vender activo');
      }
    });
  }

  private refreshPortfolioData(): void {
    this.loadBalanceCards();
    this.loadPieChart();
    this.loadLineChart();
    this.loadPositions();
    this.loadOperations();
  }
}
