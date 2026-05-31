import { Component, OnInit, ViewChild, HostListener, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterOutlet } from '@angular/router';
import { MaterialModule } from '../../../../shared/material.module';

// Importación de Chart.js con fallback
import Chart from 'chart.js/auto';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    // RouterOutlet,
    MaterialModule
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard implements OnInit, AfterViewInit {
  selectedChartType: 'line' | 'bar' = 'line';
  selectedPeriod: string = '1m'; private chart: Chart | null = null;
  private resizeObserver: any;
  constructor() {
    // Chart.js auto-registra todos los componentes con la importación 'chart.js/auto'
    console.log('Chart.js loaded:', typeof Chart !== 'undefined');
  }

  ngOnInit() {
    this.initializeChart();
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

  private initializeChart() {
    console.log('Inicializando gráfico con Chart.js...');
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

  private generateChartData() {
    let points = 30;
    let baseValue = 100000;

    // Ajustar cantidad de puntos según el período
    switch (this.selectedPeriod) {
      case '1w':
        points = 7; // 7 días
        break;
      case '1m':
        points = 30; // 30 días
        break;
      case '3m':
        points = 90; // 90 días
        break;
      case '1y':
        points = 365; // 365 días
        break;
    }

    const labels = this.generateLabels(points);
    const datasets = this.generateDatasets(points, baseValue);

    return { labels, datasets };
  }

  private generateLabels(points: number): string[] {
    const labels = [];
    const now = new Date();

    for (let i = 0; i < points; i++) {
      const date = new Date(now);

      switch (this.selectedPeriod) {
        case '1w':
          date.setDate(date.getDate() - (points - i));
          labels.push(date.toLocaleDateString('es-ES', { weekday: 'short' }));
          break;
        case '1m':
          date.setDate(date.getDate() - (points - i));
          labels.push(date.toLocaleDateString('es-ES', { day: '2-digit', month: 'short' }));
          break;
        case '3m':
          date.setDate(date.getDate() - (points - i));
          labels.push(date.toLocaleDateString('es-ES', { month: 'short' }));
          break;
        case '1y':
          date.setMonth(date.getMonth() - (points - i));
          labels.push(date.toLocaleDateString('es-ES', { month: 'short' }));
          break;
      }
    }

    return labels;
  }

  private generateDatasets(points: number, baseValue: number): any[] {
    const datasets = [];

    // Datos para gráfico de líneas o barras
    const portfolioData = [];
    const comparisonData = [];
    let portfolioValue = baseValue;
    let comparisonValue = baseValue * 0.8;

    for (let i = 0; i < points; i++) {
      portfolioValue += (Math.random() - 0.45) * 3000;
      comparisonValue += (Math.random() - 0.5) * 2000;

      portfolioData.push(portfolioValue);
      comparisonData.push(comparisonValue);
    }

    datasets.push({
      label: 'Portfolio',
      data: portfolioData,
      borderColor: '#4a90e2',
      backgroundColor: this.selectedChartType === 'line' ? 'rgba(74, 144, 226, 0.1)' : '#4a90e2',
      borderWidth: 2,
      tension: 0.4,
      fill: this.selectedChartType === 'line'
    });

    datasets.push({
      label: 'S&P 500',
      data: comparisonData,
      borderColor: '#10b981',
      backgroundColor: this.selectedChartType === 'line' ? 'rgba(16, 185, 129, 0.1)' : '#10b981',
      borderWidth: 2,
      tension: 0.4,
      fill: this.selectedChartType === 'line'
    });

    return datasets;
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
                return new Intl.NumberFormat('es-ES', {
                  style: 'currency',
                  currency: 'USD',
                  notation: 'compact',
                  minimumFractionDigits: 0,
                  maximumFractionDigits: 0
                }).format(value);
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
            label += new Intl.NumberFormat('es-ES', {
              style: 'currency',
              currency: 'USD',
              minimumFractionDigits: 0,
              maximumFractionDigits: 0
            }).format(context.parsed.y);
          }
          return label;
        }
      };
    }

    return baseConfig;
  }

  onChartTypeChange() {
    this.setupChart();
  }

  onPeriodChange() {
    this.setupChart();
  }
}


