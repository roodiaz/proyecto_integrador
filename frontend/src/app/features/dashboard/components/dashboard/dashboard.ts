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
  selectedChartType: string = 'line';
  selectedPeriod: string = '1m';
  selectedDatasets: string[] = ['portfolio', 'sp500']; // Para candlestick
  private chart: Chart | null = null;
  private resizeObserver: any;
  private candlestickCanvas: HTMLCanvasElement | null = null;
  private candlestickData: any[] = [];

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

    // Si es candlestick, usar implementación personalizada
    if (this.selectedChartType === 'candlestick') {
      this.candlestickCanvas = canvas;
      this.candlestickData = this.generateCandlestickData();
      this.drawCandlestickChart(canvas, ctx);
      
      // Agregar evento de clic para selección de datasets
      canvas.addEventListener('click', this.handleCandlestickClick.bind(this));
      canvas.addEventListener('mousemove', this.handleCandlestickHover.bind(this));
    } else {
      // Remover eventos si no es candlestick
      if (this.candlestickCanvas) {
        this.candlestickCanvas.removeEventListener('click', this.handleCandlestickClick.bind(this));
        this.candlestickCanvas.removeEventListener('mousemove', this.handleCandlestickHover.bind(this));
        this.candlestickCanvas = null;
      }
      
      // Generar datos según el período seleccionado
      const data = this.generateChartData();
      
      console.log('Generated data:', data);
      
      // Configuración según el tipo de gráfico
      const config = this.getChartConfiguration(data);
      
      console.log('Chart config:', config);
      
      // Crear el gráfico
      this.chart = new Chart(ctx, config);
      
      console.log('Chart created successfully');
    }
  }

  private handleCandlestickClick(event: MouseEvent) {
    const canvas = this.candlestickCanvas;
    if (!canvas) return;
    
    const rect = canvas.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;
    
    const padding = 40;
    const chartWidth = canvas.width - 2 * padding;
    const candleSpacing = chartWidth / this.candlestickData.length;
    
    // Determinar qué dataset se hizo clic
    const clickedIndex = Math.floor((x - padding) / candleSpacing);
    
    if (clickedIndex >= 0 && clickedIndex < this.candlestickData.length) {
      const candleX = padding + clickedIndex * candleSpacing + candleSpacing / 2;
      const candleWidth = Math.max(2, (chartWidth / this.candlestickData.length * 0.6) / 2);
      
      // Determinar si se hizo clic en Portfolio o S&P 500
      if (x >= candleX - candleWidth && x <= candleX) {
        // Portfolio
        this.toggleDataset('portfolio');
      } else if (x >= candleX + 1 && x <= candleX + candleWidth + 2) {
        // S&P 500
        this.toggleDataset('sp500');
      }
      
      // Redibujar gráfico
      this.drawCandlestickChart(canvas, canvas.getContext('2d')!);
    }
  }

  private handleCandlestickHover(event: MouseEvent) {
    const canvas = this.candlestickCanvas;
    if (!canvas) return;
    
    const rect = canvas.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;
    
    const padding = 40;
    const chartWidth = canvas.width - 2 * padding;
    const candleSpacing = chartWidth / this.candlestickData.length;
    
    // Cambiar cursor para indicar áreas clickeables
    const clickedIndex = Math.floor((x - padding) / candleSpacing);
    
    if (clickedIndex >= 0 && clickedIndex < this.candlestickData.length) {
      const candleX = padding + clickedIndex * candleSpacing + candleSpacing / 2;
      const candleWidth = Math.max(2, (chartWidth / this.candlestickData.length * 0.6) / 2);
      
      if ((x >= candleX - candleWidth && x <= candleX) || (x >= candleX + 1 && x <= candleX + candleWidth + 2)) {
        canvas.style.cursor = 'pointer';
      } else {
        canvas.style.cursor = 'default';
      }
    } else {
      canvas.style.cursor = 'default';
    }
  }

  private toggleDataset(dataset: string) {
    const index = this.selectedDatasets.indexOf(dataset);
    if (index > -1) {
      this.selectedDatasets.splice(index, 1);
    } else {
      this.selectedDatasets.push(dataset);
    }
    
    // Si no hay datasets seleccionados, mantener ambos activos
    if (this.selectedDatasets.length === 0) {
      this.selectedDatasets = ['portfolio', 'sp500'];
    }
    
    console.log('Selected datasets:', this.selectedDatasets);
  }

  private drawCandlestickChart(canvas: HTMLCanvasElement, ctx: CanvasRenderingContext2D) {
    const data = this.generateCandlestickData();
    const width = canvas.width;
    const height = canvas.height;
    const padding = 40;
    
    // Limpiar canvas
    ctx.clearRect(0, 0, width, height);
    
    // Calcular dimensiones
    const chartWidth = width - 2 * padding;
    const chartHeight = height - 2 * padding;
    const candleWidth = Math.max(2, (chartWidth / data.length * 0.6) / 2);
    const candleSpacing = chartWidth / data.length;
    
    // Encontrar min y max para escalar (solo datasets seleccionados)
    let allValues = [];
    if (this.selectedDatasets.includes('portfolio')) {
      allValues.push(...data.flatMap(d => [d.low, d.high]));
    }
    if (this.selectedDatasets.includes('sp500')) {
      allValues.push(...data.flatMap(d => [d.sp500Low, d.sp500High]));
    }
    
    const minValue = Math.min(...allValues);
    const maxValue = Math.max(...allValues);
    const range = maxValue - minValue || 1;
    
    // Dibujar grid
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.1)';
    ctx.lineWidth = 1;
    
    // Líneas horizontales
    for (let i = 0; i <= 5; i++) {
      const y = padding + (chartHeight / 5) * i;
      ctx.beginPath();
      ctx.moveTo(padding, y);
      ctx.lineTo(width - padding, y);
      ctx.stroke();
    }
    
    // Líneas verticales
    for (let i = 0; i <= data.length; i += Math.ceil(data.length / 10)) {
      const x = padding + (chartWidth / data.length) * i;
      ctx.beginPath();
      ctx.moveTo(x, padding);
      ctx.lineTo(x, height - padding);
      ctx.stroke();
    }
    
    // Dibujar velas según datasets seleccionados
    data.forEach((candle, index) => {
      const x = padding + index * candleSpacing + candleSpacing / 2;
      
      // Portfolio candles (izquierda)
      if (this.selectedDatasets.includes('portfolio')) {
        const portfolioHighY = padding + ((maxValue - candle.high) / range) * chartHeight;
        const portfolioLowY = padding + ((maxValue - candle.low) / range) * chartHeight;
        const portfolioOpenY = padding + ((maxValue - candle.open) / range) * chartHeight;
        const portfolioCloseY = padding + ((maxValue - candle.close) / range) * chartHeight;
        
        const portfolioIsGreen = candle.close >= candle.open;
        const portfolioColor = portfolioIsGreen ? '#4a90e2' : '#357ae8';
        const portfolioOpacity = this.selectedDatasets.includes('sp500') ? 0.8 : 1.0;
        
        // Dibujar wick de portfolio
        ctx.strokeStyle = portfolioColor;
        ctx.globalAlpha = portfolioOpacity;
        ctx.lineWidth = 1;
        ctx.beginPath();
        ctx.moveTo(x - candleWidth / 2, portfolioHighY);
        ctx.lineTo(x - candleWidth / 2, portfolioLowY);
        ctx.stroke();
        
        // Dibujar cuerpo de portfolio
        const portfolioBodyTop = Math.min(portfolioOpenY, portfolioCloseY);
        const portfolioBodyHeight = Math.abs(portfolioCloseY - portfolioOpenY);
        
        if (portfolioBodyHeight > 1) {
          ctx.fillStyle = portfolioColor;
          ctx.globalAlpha = portfolioOpacity * 0.8;
          ctx.fillRect(x - candleWidth - 1, portfolioBodyTop, candleWidth, portfolioBodyHeight);
          ctx.globalAlpha = 1.0;
        } else {
          // Doji
          ctx.strokeStyle = portfolioColor;
          ctx.lineWidth = 2;
          ctx.beginPath();
          ctx.moveTo(x - candleWidth - 1, portfolioBodyTop);
          ctx.lineTo(x, portfolioBodyTop);
          ctx.stroke();
        }
      }
      
      // S&P 500 candles (derecha)
      if (this.selectedDatasets.includes('sp500')) {
        const sp500HighY = padding + ((maxValue - candle.sp500High) / range) * chartHeight;
        const sp500LowY = padding + ((maxValue - candle.sp500Low) / range) * chartHeight;
        const sp500OpenY = padding + ((maxValue - candle.sp500Open) / range) * chartHeight;
        const sp500CloseY = padding + ((maxValue - candle.sp500Close) / range) * chartHeight;
        
        const sp500IsGreen = candle.sp500Close >= candle.sp500Open;
        const sp500Color = sp500IsGreen ? '#10b981' : '#ef4444';
        const sp500Opacity = this.selectedDatasets.includes('portfolio') ? 0.8 : 1.0;
        
        // Dibujar wick de S&P 500
        ctx.strokeStyle = sp500Color;
        ctx.globalAlpha = sp500Opacity;
        ctx.lineWidth = 1;
        ctx.beginPath();
        ctx.moveTo(x + 1, sp500HighY);
        ctx.lineTo(x + 1, sp500LowY);
        ctx.stroke();
        
        // Dibujar cuerpo de S&P 500
        const sp500BodyTop = Math.min(sp500OpenY, sp500CloseY);
        const sp500BodyHeight = Math.abs(sp500CloseY - sp500OpenY);
        
        if (sp500BodyHeight > 1) {
          ctx.fillStyle = sp500Color;
          ctx.globalAlpha = sp500Opacity * 0.8;
          ctx.fillRect(x + 1, sp500BodyTop, candleWidth, sp500BodyHeight);
          ctx.globalAlpha = 1.0;
        } else {
          // Doji
          ctx.strokeStyle = sp500Color;
          ctx.lineWidth = 2;
          ctx.beginPath();
          ctx.moveTo(x + 1, sp500BodyTop);
          ctx.lineTo(x + candleWidth + 1, sp500BodyTop);
          ctx.stroke();
        }
      }
    });
    
    ctx.globalAlpha = 1.0;
    
    // Dibujar leyenda
    ctx.fillStyle = 'rgba(255, 255, 255, 0.7)';
    ctx.font = '12px sans-serif';
    
    let legendY = 15;
    
    // Portfolio legend
    if (this.selectedDatasets.includes('portfolio')) {
      const portfolioOpacity = this.selectedDatasets.includes('sp500') ? 0.8 : 1.0;
      ctx.globalAlpha = portfolioOpacity;
      ctx.fillStyle = '#4a90e2';
      ctx.fillRect(width - 150, legendY, 12, 12);
      ctx.globalAlpha = 1.0;
      ctx.fillStyle = 'rgba(255, 255, 255, 0.9)';
      ctx.fillText('Portfolio', width - 130, legendY + 4);
      legendY += 20;
    }
    
    // S&P 500 legend
    if (this.selectedDatasets.includes('sp500')) {
      const sp500Opacity = this.selectedDatasets.includes('portfolio') ? 0.8 : 1.0;
      ctx.globalAlpha = sp500Opacity;
      ctx.fillStyle = '#10b981';
      ctx.fillRect(width - 150, legendY, 12, 12);
      ctx.globalAlpha = 1.0;
      ctx.fillStyle = 'rgba(255, 255, 255, 0.9)';
      ctx.fillText('S&P 500', width - 130, legendY + 4);
      legendY += 20;
    }
    
    // Dibujar etiquetas de precios
    ctx.fillStyle = 'rgba(255, 255, 255, 0.7)';
    ctx.font = '11px sans-serif';
    
    // Etiquetas Y
    for (let i = 0; i <= 5; i++) {
      const value = minValue + (range / 5) * (5 - i);
      const y = padding + (chartHeight / 5) * i;
      const label = `$${(value / 1000).toFixed(0)}K`;
      ctx.fillText(label, 5, y + 4);
    }
    
    console.log('Candlestick chart with Portfolio vs S&P 500 drawn successfully');
  }

  private generateCandlestickData() {
    let points = 30;
    let baseValue = 100000;
    
    // Ajustar cantidad de puntos según el período
    switch (this.selectedPeriod) {
      case '1d':
        points = 24;
        break;
      case '1w':
        points = 7;
        break;
      case '1m':
        points = 30;
        break;
      case '3m':
        points = 90;
        break;
      case '1y':
        points = 365;
        break;
    }
    
    const data = [];
    let portfolioValue = baseValue;
    let sp500Value = baseValue * 0.8;
    
    for (let i = 0; i < points; i++) {
      // Portfolio data
      const portfolioVariation = (Math.random() - 0.5) * 2000;
      portfolioValue += portfolioVariation;
      
      const portfolioOpen = portfolioValue - Math.random() * 500;
      const portfolioClose = portfolioValue + Math.random() * 500;
      const portfolioHigh = Math.max(portfolioOpen, portfolioClose) + Math.random() * 1000;
      const portfolioLow = Math.min(portfolioOpen, portfolioClose) - Math.random() * 1000;
      
      // S&P 500 data
      const sp500Variation = (Math.random() - 0.5) * 1500;
      sp500Value += sp500Variation;
      
      const sp500Open = sp500Value - Math.random() * 400;
      const sp500Close = sp500Value + Math.random() * 400;
      const sp500High = Math.max(sp500Open, sp500Close) + Math.random() * 800;
      const sp500Low = Math.min(sp500Open, sp500Close) - Math.random() * 800;
      
      data.push({
        x: i,
        open: Math.max(portfolioLow, portfolioOpen),
        high: portfolioHigh,
        low: Math.min(portfolioLow, portfolioClose),
        close: portfolioClose,
        sp500Open: Math.max(sp500Low, sp500Open),
        sp500High: sp500High,
        sp500Low: Math.min(sp500Low, sp500Close),
        sp500Close: sp500Close
      });
    }
    
    return data;
  }

  private generateChartData() {
    let points = 30;
    let baseValue = 100000;
    
    // Ajustar cantidad de puntos según el período
    switch (this.selectedPeriod) {
      case '1d':
        points = 24; // 24 horas
        break;
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
        case '1d':
          date.setHours(date.getHours() - (points - i));
          labels.push(date.toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' }));
          break;
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
    
    if (this.selectedChartType === 'candlestick') {
      // Datos para gráfico de velas - usar un enfoque diferente
      const ohlcData = [];
      let currentValue = baseValue;
      
      for (let i = 0; i < points; i++) {
        const variation = (Math.random() - 0.5) * 2000;
        currentValue += variation;
        
        const open = currentValue - Math.random() * 500;
        const close = currentValue + Math.random() * 500;
        const high = Math.max(open, close) + Math.random() * 1000;
        const low = Math.min(open, close) - Math.random() * 1000;
        
        // Formato para Chart.js candlestick plugin o gráfico de barras personalizado
        ohlcData.push({
          x: i,
          y: [low, open, close, high] // [low, open, close, high] para financial charts
        });
      }
      
      datasets.push({
        label: 'Portfolio Value',
        data: ohlcData,
        borderColor: '#4a90e2',
        backgroundColor: 'rgba(74, 144, 226, 0.1)',
        borderWidth: 2,
        // Para gráfico de barras que simule velas
        barPercentage: 0.8,
        categoryPercentage: 0.9
      });
    } else {
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
    }
    
    return datasets;
  }

  private getChartConfiguration(data: any): any {
    const baseConfig = {
      type: this.selectedChartType === 'candlestick' ? 'bar' : this.selectedChartType,
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
                if (this.selectedChartType === 'candlestick') {
                  const dataPoint = context.raw;
                  return [
                    `Open: $${dataPoint.y[1]?.toFixed(2) || '0.00'}`,
                    `High: $${dataPoint.y[3]?.toFixed(2) || '0.00'}`,
                    `Low: $${dataPoint.y[0]?.toFixed(2) || '0.00'}`,
                    `Close: $${dataPoint.y[2]?.toFixed(2) || '0.00'}`
                  ];
                } else {
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

    // Configuración específica para gráfico de velas
    if (this.selectedChartType === 'candlestick') {
      baseConfig.options.plugins.tooltip.callbacks = {
        label: (context: any) => {
          const dataPoint = context.raw;
          return [
            `Open: $${dataPoint.y[1]?.toFixed(2) || '0.00'}`,
            `High: $${dataPoint.y[3]?.toFixed(2) || '0.00'}`,
            `Low: $${dataPoint.y[0]?.toFixed(2) || '0.00'}`,
            `Close: $${dataPoint.y[2]?.toFixed(2) || '0.00'}`
          ];
        }
      };
      
      // Configuración para simular velas con barras
      baseConfig.data.datasets[0].backgroundColor = (context: any) => {
        const dataPoint = context.raw;
        const isGreen = dataPoint.y[2] >= dataPoint.y[1]; // close >= open
        return isGreen ? 'rgba(16, 185, 129, 0.8)' : 'rgba(239, 68, 68, 0.8)';
      };
      
      baseConfig.data.datasets[0].borderColor = (context: any) => {
        const dataPoint = context.raw;
        const isGreen = dataPoint.y[2] >= dataPoint.y[1]; // close >= open
        return isGreen ? '#10b981' : '#ef4444';
      };
    }

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
