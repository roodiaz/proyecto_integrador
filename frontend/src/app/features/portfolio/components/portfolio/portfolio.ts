import { Component, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { Chart, ChartConfiguration, ChartType, registerables, TooltipItem } from 'chart.js';
import { PortfolioService } from '../../services/portfolio.service';
import { PortfolioOperation } from '../../models/portfolio-operation';
import { PortfolioSummary } from '../../models/portfolio-summary';
import { PortfolioModal, BuyData } from '../portfolio-modal/portfolio-modal';

@Component({
  selector: 'app-portfolio',
  standalone: true,
  imports: [CommonModule, FormsModule, PortfolioModal],
  templateUrl: './portfolio.html',
  styleUrl: './portfolio.css'
})
export class Portfolio implements OnInit, OnDestroy, AfterViewInit {
  operations: PortfolioOperation[] = [];
  portfolioSummary: PortfolioSummary = {
    initialBalance: 10000,
    currentBalance: 10000,
    totalProfitLoss: 0,
    totalProfitLossPercent: 0,
    totalInvested: 0
  };
  
  // Modal controls
  showBuyModal: boolean = false;
  showSellModal: boolean = false;
  selectedOperation: PortfolioOperation | null = null;

  // Filter controls
  filterText: string = '';
  sortBy: string = '';

  // Top assets data
  topAssets: Array<{ticker: string, quantity: number, percentage: number}> = [];

  // Chart.js instances
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

  constructor() {
    // Register Chart.js components
    Chart.register(...registerables);
  }

  ngOnInit(): void {
    this.loadPortfolioData();
    this.startLiveUpdates();
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

  loadPortfolioData(): void {
    // Datos de ejemplo para mostrar mientras el backend no está disponible
    this.operations = [
      {
        id: '1',
        ticker: 'AAPL',
        quantity: 10,
        buyPrice: 175.50,
        currentPrice: 185.50,
        variationPercent: 5.7,
        profitLoss: 100.00,
        profitLossPercent: 5.7,
        operationDate: new Date('2024-01-15'),
        isOpen: true
      },
      {
        id: '2',
        ticker: 'GOOGL',
        quantity: 5,
        buyPrice: 140.00,
        currentPrice: 142.30,
        variationPercent: 1.6,
        profitLoss: 11.50,
        profitLossPercent: 1.6,
        operationDate: new Date('2024-01-20'),
        isOpen: true
      },
      {
        id: '3',
        ticker: 'MSFT',
        quantity: 8,
        buyPrice: 370.00,
        currentPrice: 380.75,
        variationPercent: 2.9,
        profitLoss: 86.00,
        profitLossPercent: 2.9,
        operationDate: new Date('2024-02-01'),
        isOpen: true
      }
    ];
    
    this.updatePortfolioSummary();
    this.updateTopAssets();
  }

  startLiveUpdates(): void {
    // Simulación de actualización en vivo
    this.subscription = new Subscription();
  }

  updatePortfolioSummary(): void {
    const totalInvested = this.operations
      .filter(op => op.isOpen)
      .reduce((sum, op) => sum + (op.quantity * op.buyPrice), 0);
    
    const currentValue = this.operations
      .filter(op => op.isOpen)
      .reduce((sum, op) => sum + (op.quantity * op.currentPrice), 0);
    
    const currentBalance = this.portfolioSummary.initialBalance - totalInvested + currentValue;
    const totalProfitLoss = currentBalance - this.portfolioSummary.initialBalance;
    const totalProfitLossPercent = (totalProfitLoss / this.portfolioSummary.initialBalance) * 100;

    this.portfolioSummary = {
      ...this.portfolioSummary,
      currentBalance,
      totalProfitLoss,
      totalProfitLossPercent,
      totalInvested
    };
  }

  updateTopAssets(): void {
    this.topAssets = this.getTopAssets();
  }

  // Filter methods
  get filteredOperations(): PortfolioOperation[] {
    let filtered = this.operations.filter(op => op.isOpen);
    
    // Apply text filter
    if (this.filterText) {
      filtered = filtered.filter(op => 
        op.ticker.toLowerCase().includes(this.filterText.toLowerCase())
      );
    }
    
    // Apply sorting
    switch (this.sortBy) {
      case 'best':
        filtered.sort((a, b) => b.profitLoss - a.profitLoss);
        break;
      case 'worst':
        filtered.sort((a, b) => a.profitLoss - b.profitLoss);
        break;
      default:
        filtered.sort((a, b) => new Date(b.operationDate).getTime() - new Date(a.operationDate).getTime());
        break;
    }
    
    return filtered;
  }

  onFilterChange(event: Event): void {
    this.filterText = (event.target as HTMLInputElement).value;
  }

  onSortChange(event: Event): void {
    this.sortBy = (event.target as HTMLSelectElement).value;
  }

  onPeriodChange(event: Event): void {
    this.selectedPeriod = (event.target as HTMLSelectElement).value;
    this.createLineChart(); // Actualizar gráfico con nuevo período
  }

  getTopAssets(): Array<{ticker: string, quantity: number, percentage: number}> {
    const openOperations = this.operations.filter(op => op.isOpen);
    const totalQuantity = openOperations.reduce((sum, op) => sum + op.quantity, 0);
    
    return openOperations
      .sort((a, b) => b.quantity - a.quantity)
      .slice(0, 3)
      .map(op => ({
        ticker: op.ticker,
        quantity: op.quantity,
        percentage: (op.quantity / totalQuantity) * 100
      }));
  }

  // Modal methods
  openBuyModal(): void {
    this.showBuyModal = true;
  }

  openSellModal(operation: PortfolioOperation): void {
    this.selectedOperation = operation;
    this.showSellModal = true;
  }

  closeBuyModal(): void {
    this.showBuyModal = false;
  }

  closeSellModal(): void {
    this.showSellModal = false;
    this.selectedOperation = null;
  }

  onBuyComplete(buyData: BuyData): void {
    const newOperation: PortfolioOperation = {
      id: Date.now().toString(),
      ticker: buyData.ticker,
      quantity: buyData.quantity,
      buyPrice: buyData.price,
      currentPrice: buyData.price,
      variationPercent: 0,
      profitLoss: 0,
      profitLossPercent: 0,
      operationDate: new Date(),
      isOpen: true
    };

    this.operations.push(newOperation);
    this.updatePortfolioSummary();
    this.closeBuyModal();
    
    // Redibujar gráficos
    setTimeout(() => {
      this.initializeCharts();
    }, 100);
  }

  onSellComplete(operationId: string): void {
    const operation = this.operations.find(op => op.id === operationId);
    if (operation) {
      operation.isOpen = false;
      operation.profitLoss = (operation.currentPrice - operation.buyPrice) * operation.quantity;
      operation.profitLossPercent = ((operation.currentPrice - operation.buyPrice) / operation.buyPrice) * 100;
      this.updatePortfolioSummary();
    }
    this.closeSellModal();
    
    // Redibujar gráficos
    setTimeout(() => {
      this.initializeCharts();
    }, 100);
  }

  initializeCharts(): void {
    this.createPieChart();
    this.createLineChart();
  }

  createPieChart(): void {
    const canvas = document.getElementById('pieChart') as HTMLCanvasElement;
    if (!canvas) return;

    // Destroy existing chart if it exists
    if (this.pieChart) {
      this.pieChart.destroy();
    }

    const openOperations = this.operations.filter(op => op.isOpen);
    const colors = ['#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF', '#FF9F40'];
    
    const data = {
      labels: openOperations.map(op => op.ticker),
      datasets: [{
        data: openOperations.map(op => op.quantity * op.currentPrice),
        backgroundColor: colors.slice(0, openOperations.length),
        borderColor: '#1E1E1E',
        borderWidth: 2,
        hoverOffset: 4
      }]
    };

    const config: ChartConfiguration = {
      type: 'doughnut' as ChartType,
      data: data,
      options: {
        responsive: true,
        maintainAspectRatio: false,
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
            displayColors: true,
            callbacks: {
              label: (context: any) => {
                const label = context.label || '';
                const value = context.parsed || 0;
                const total = context.dataset.data.reduce((a: number, b: number) => a + b, 0);
                const percentage = ((value / total) * 100).toFixed(1);
                return `${label}: $${value.toFixed(2)} (${percentage}%)`;
              }
            }
          }
        }
      }
    };

    this.pieChart = new Chart(canvas, config);
  }

  createLineChart(): void {
    const canvas = document.getElementById('lineChart') as HTMLCanvasElement;
    if (!canvas) return;

    // Destroy existing chart if it exists
    if (this.lineChart) {
      this.lineChart.destroy();
    }

    // Generar datos según el período seleccionado
    const data = this.generateDataForPeriod(this.selectedPeriod);

    const config: ChartConfiguration = {
      type: 'line' as ChartType,
      data: {
        labels: data.map(d => d.date),
        datasets: [{
          label: 'Portfolio Value',
          data: data.map(d => d.value),
          borderColor: '#4ECDC4',
          backgroundColor: 'rgba(78, 205, 196, 0.1)',
          borderWidth: 3,
          fill: true,
          tension: 0.3,
          pointBackgroundColor: '#4ECDC4',
          pointBorderColor: '#FFFFFF',
          pointBorderWidth: 3,
          pointRadius: 6,
          pointHoverRadius: 8
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
                const date = new Date(context[0].label);
                return date.toLocaleDateString('es-ES', { month: 'short', day: 'numeric', year: 'numeric' });
              },
              label: (context: any) => {
                const value = context.parsed as number;
                return `Valor: $${value.toLocaleString('es-ES')}`;
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
                const date = new Date(data[index].date);
                return date.toLocaleDateString('es-ES', { month: 'short', day: 'numeric' });
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

  generateDataForPeriod(period: string): Array<{date: string, value: number}> {
    const baseValue = 10000;
    const today = new Date();
    let startDate = new Date();
    let dataPoints = 0;

    switch (period) {
      case '7d':
        startDate.setDate(today.getDate() - 7);
        dataPoints = 7;
        break;
      case '1m':
        startDate.setDate(today.getDate() - 30);
        dataPoints = 6;
        break;
      case '3m':
        startDate.setMonth(today.getMonth() - 3);
        dataPoints = 12;
        break;
      case '6m':
        startDate.setMonth(today.getMonth() - 6);
        dataPoints = 12;
        break;
      case '1y':
        startDate.setFullYear(today.getFullYear() - 1);
        dataPoints = 12;
        break;
      default:
        startDate.setDate(today.getDate() - 30);
        dataPoints = 6;
    }

    const data: Array<{date: string, value: number}> = [];
    const timeDiff = today.getTime() - startDate.getTime();
    
    for (let i = 0; i < dataPoints; i++) {
      const date = new Date(startDate.getTime() + (timeDiff / dataPoints) * i);
      const randomVariation = (Math.random() - 0.3) * 500; // Tendencia a subir
      const trend = i * 30; // Tendencia de crecimiento
      const value = baseValue + trend + randomVariation + (Math.sin(i / 2) * 100);
      
      data.push({
        date: date.toISOString().split('T')[0],
        value: Math.round(value)
      });
    }

    return data;
  }

  private getRandomColor(): string {
    const colors = ['#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF', '#FF9F40'];
    return colors[Math.floor(Math.random() * colors.length)];
  }

  getAssetColor(index: number): string {
    const colors = ['#FF6384', '#36A2EB', '#FFCE56'];
    return colors[index % colors.length];
  }
}
