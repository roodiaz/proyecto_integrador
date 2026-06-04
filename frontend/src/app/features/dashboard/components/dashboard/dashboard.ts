import { Component, OnInit, ViewChild, HostListener, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterOutlet } from '@angular/router';
import { MaterialModule } from '../../../../shared/material.module';
import { DashboardService } from '../../services/dashboard.service';
import { DashboardTopCards, DashboardPerformanceChart } from '../../models/dashboard.models';

// Importación de Chart.js con fallback
import Chart from 'chart.js/auto';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MaterialModule
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard implements OnInit, AfterViewInit {
  selectedChartType: 'line' | 'bar' = 'line';
  selectedPeriod: '1W' | '1M' | '3M' | '1Y' = '1M';
  topCards: DashboardTopCards | null = null;
  performanceChart: DashboardPerformanceChart | null = null;
  loadingTopCards = false;
  loadingPerformanceChart = false;

  private chart: Chart | null = null;
  private resizeObserver: any;

  constructor(
    private dashboardService: DashboardService
  ) { }

  ngOnInit() {
    this.loadTopCards();
    this.loadPerformanceChart();
  }

  ngAfterViewInit() {
    this.setupChart();
    this.setupResizeObserver();
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

    console.log('Canvas size:', canvas.width, 'x', canvas.height);

    // Generar datos según el período seleccionado
    const data = this.generateChartData();

    console.log('Generated data:', data);

    const config = this.getChartConfiguration(data);

    console.log('Chart config:', config);

    this.chart = new Chart(ctx, config);

    console.log('Chart created successfully');
  }

  loadTopCards() {
    this.loadingTopCards = true;

    this.dashboardService.getTopCards().subscribe({
      next: (response) => {
        this.loadingTopCards = false;

        if (!response.success) return;

        this.topCards = response.data;
        this.setupChart();
      },
      error: () => {
        this.loadingTopCards = false;
      }
    });
  }

  loadPerformanceChart() {
    this.loadingPerformanceChart = true;

    this.dashboardService.getPerformanceChart(this.selectedPeriod).subscribe({
      next: (response) => {
        this.loadingPerformanceChart = false;

        if (!response.success) return;

        this.performanceChart = response.data;

        if (!this.hasEnoughChartData) {
          if (this.chart) {
            this.chart.destroy();
            this.chart = null;
          }

          return;
        }

        setTimeout(() => this.setupChart());
      },
      error: () => {
        this.loadingPerformanceChart = false;
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

  onChartTypeChange() {
    if (!this.hasEnoughChartData) return;
    this.setupChart();
  }

  onPeriodChange() {
    this.loadPerformanceChart();
  }

  get hasEnoughChartData(): boolean {
    return (this.performanceChart?.data?.length ?? 0) >= 2;
  }
}


