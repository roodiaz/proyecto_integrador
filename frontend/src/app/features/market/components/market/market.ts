import { Component, OnInit, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MaterialModule } from '../../../../shared/material.module';
import Chart from 'chart.js/auto';

@Component({
  selector: 'app-market',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MaterialModule
  ],
  templateUrl: './market-new.html',
  styleUrl: './market-new.css'
})
export class Market implements OnInit, AfterViewInit {
  selectedCategory: string = 'all';
  searchQuery: string = '';
  selectedTimeframe: string = '1d';
  selectedSort: string = 'volume';
  selectedMoverType: string = 'gainers';
  selectedMarketCap: string = 'all';
  selectedPerformance: string = 'all';
  marketData: any[] = [];
  trendingStocks: any[] = [];
  marketIndices: any[] = [];
  sectors: any[] = [];
  marketNews: any[] = [];
  private chart: Chart | null = null;

  constructor(private router: Router) {}

  ngOnInit() {
    this.loadMarketData();
    this.loadTrendingStocks();
    this.loadMarketIndices();
    this.loadSectors();
    this.loadMarketNews();
  }

  ngAfterViewInit() {
    this.setupMarketOverviewChart();
  }

  private loadMarketData() {
    // Datos de ejemplo para el mercado
    this.marketData = [
      { symbol: 'AAPL', name: 'Apple Inc.', price: 185.50, change: 2.35, changePercent: 1.28, volume: 52341234, marketCap: 2873000000000, sector: 'Technology' },
      { symbol: 'MSFT', name: 'Microsoft Corp.', price: 380.75, change: 3.12, changePercent: 0.83, volume: 23456789, marketCap: 2825000000000, sector: 'Technology' },
      { symbol: 'GOOGL', name: 'Alphabet Inc.', price: 142.30, change: -1.45, changePercent: -1.01, volume: 34567890, marketCap: 1780000000000, sector: 'Technology' },
      { symbol: 'AMZN', name: 'Amazon.com Inc.', price: 178.25, change: 4.67, changePercent: 2.69, volume: 45678901, marketCap: 1850000000000, sector: 'Consumer Discretionary' },
      { symbol: 'TSLA', name: 'Tesla Inc.', price: 245.80, change: -8.23, changePercent: -3.24, volume: 98765432, marketCap: 780000000000, sector: 'Consumer Discretionary' },
      { symbol: 'NVDA', name: 'NVIDIA Corp.', price: 875.30, change: 15.67, changePercent: 1.83, volume: 67890123, marketCap: 2160000000000, sector: 'Technology' },
      { symbol: 'META', name: 'Meta Platforms', price: 485.20, change: 12.34, changePercent: 2.61, volume: 34567890, marketCap: 1240000000000, sector: 'Technology' },
      { symbol: 'BRK.B', name: 'Berkshire Hathaway', price: 458.90, change: 1.23, changePercent: 0.27, volume: 2345678, marketCap: 730000000000, sector: 'Financial' },
      { symbol: 'JPM', name: 'JPMorgan Chase', price: 198.45, change: 2.89, changePercent: 1.48, volume: 12345678, marketCap: 560000000000, sector: 'Financial' },
      { symbol: 'V', name: 'Visa Inc.', price: 275.30, change: 1.87, changePercent: 0.68, volume: 8765432, marketCap: 530000000000, sector: 'Financial' },
      { symbol: 'JNJ', name: 'Johnson & Johnson', price: 165.20, change: -0.45, changePercent: -0.27, volume: 5678901, marketCap: 420000000000, sector: 'Healthcare' },
      { symbol: 'WMT', name: 'Walmart Inc.', price: 168.75, change: 0.89, changePercent: 0.53, volume: 9876543, marketCap: 530000000000, sector: 'Consumer Staples' }
    ];
  }

  private loadTrendingStocks() {
    this.trendingStocks = [
      { symbol: 'NVDA', name: 'NVIDIA Corp.', price: 875.30, change: 15.67, changePercent: 1.83, mentions: 45678 },
      { symbol: 'TSLA', name: 'Tesla Inc.', price: 245.80, change: -8.23, changePercent: -3.24, mentions: 34256 },
      { symbol: 'AAPL', name: 'Apple Inc.', price: 185.50, change: 2.35, changePercent: 1.28, mentions: 28934 },
      { symbol: 'AMZN', name: 'Amazon.com Inc.', price: 178.25, change: 4.67, changePercent: 2.69, mentions: 23456 },
      { symbol: 'META', name: 'Meta Platforms', price: 485.20, change: 12.34, changePercent: 2.61, mentions: 19876 }
    ];
  }

  private loadMarketIndices() {
    this.marketIndices = [
      { name: 'S&P 500', value: 5234.56, change: 23.45, changePercent: 0.45, trend: 'up' },
      { name: 'Dow Jones', value: 39876.23, change: 156.78, changePercent: 0.40, trend: 'up' },
      { name: 'NASDAQ', value: 16345.67, change: -45.23, changePercent: -0.28, trend: 'down' },
      { name: 'Russell 2000', value: 2234.56, change: 12.34, changePercent: 0.55, trend: 'up' }
    ];
  }

  private loadSectors() {
    this.sectors = [
      { name: 'Technology', change: 1.23, changePercent: 0.45, stocks: 156 },
      { name: 'Healthcare', change: -0.34, changePercent: -0.12, stocks: 89 },
      { name: 'Financial', change: 0.89, changePercent: 0.67, stocks: 67 },
      { name: 'Consumer Discretionary', change: 1.56, changePercent: 0.78, stocks: 78 },
      { name: 'Energy', change: -1.23, changePercent: -0.89, stocks: 45 },
      { name: 'Industrial', change: 0.45, changePercent: 0.34, stocks: 56 }
    ];
  }

  private loadMarketNews() {
    this.marketNews = [
      { title: 'Fed Signals Potential Rate Pause Amid Economic Uncertainty', source: 'Reuters', time: '2 hours ago' },
      { title: 'Tech Stocks Rally as AI Optimism Grows', source: 'Bloomberg', time: '3 hours ago' },
      { title: 'Oil Prices Surge on Supply Concerns', source: 'CNBC', time: '4 hours ago' },
      { title: 'Bitcoin Reaches New Monthly High', source: 'CoinDesk', time: '5 hours ago' }
    ];
  }

  private setupMarketOverviewChart() {
    const canvas = document.getElementById('marketOverviewChart') as HTMLCanvasElement;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Datos para el gráfico de overview del mercado
    const labels = ['9:30', '10:00', '10:30', '11:00', '11:30', '12:00', '12:30', '1:00', '1:30', '2:00', '2:30', '3:00', '3:30', '4:00'];
    const sp500Data = [5200, 5210, 5205, 5215, 5220, 5218, 5225, 5230, 5228, 5232, 5235, 5230, 5234, 5234.56];
    const nasdaqData = [16300, 16320, 16310, 16330, 16340, 16335, 16345, 16350, 16348, 16352, 16355, 16348, 16350, 16345.67];

    this.chart = new Chart(ctx, {
      type: 'line',
      data: {
        labels: labels,
        datasets: [
          {
            label: 'S&P 500',
            data: sp500Data,
            borderColor: '#4a90e2',
            backgroundColor: 'rgba(74, 144, 226, 0.1)',
            borderWidth: 2,
            tension: 0.4,
            fill: false
          },
          {
            label: 'NASDAQ',
            data: nasdaqData,
            borderColor: '#10b981',
            backgroundColor: 'rgba(16, 185, 129, 0.1)',
            borderWidth: 2,
            tension: 0.4,
            fill: false
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: true,
            position: 'top',
            labels: {
              color: '#ffffff',
              font: { size: 12 }
            }
          },
          tooltip: {
            mode: 'index',
            intersect: false,
            backgroundColor: 'rgba(0, 0, 0, 0.8)',
            titleColor: '#ffffff',
            bodyColor: '#ffffff',
            borderColor: '#4a90e2',
            borderWidth: 1
          }
        },
        scales: {
          x: {
            grid: { color: 'rgba(255, 255, 255, 0.1)' },
            ticks: { color: 'rgba(255, 255, 255, 0.7)' }
          },
          y: {
            grid: { color: 'rgba(255, 255, 255, 0.1)' },
            ticks: { color: 'rgba(255, 255, 255, 0.7)' }
          }
        }
      }
    });
  }

  get filteredMarketData() {
    let filtered = this.marketData;

    // Filtrar por categoría
    if (this.selectedCategory !== 'all') {
      filtered = filtered.filter(stock => stock.sector === this.selectedCategory);
    }

    // Filtrar por búsqueda
    if (this.searchQuery) {
      const query = this.searchQuery.toLowerCase();
      filtered = filtered.filter(stock => 
        stock.symbol.toLowerCase().includes(query) || 
        stock.name.toLowerCase().includes(query)
      );
    }

    // Ordenar
    switch (this.selectedSort) {
      case 'price':
        filtered.sort((a, b) => b.price - a.price);
        break;
      case 'change':
        filtered.sort((a, b) => b.change - a.change);
        break;
      case 'changePercent':
        filtered.sort((a, b) => b.changePercent - a.changePercent);
        break;
      case 'volume':
        filtered.sort((a, b) => b.volume - a.volume);
        break;
      case 'marketCap':
        filtered.sort((a, b) => b.marketCap - a.marketCap);
        break;
    }

    return filtered;
  }

  getTopMoversData() {
    const sorted = [...this.marketData].sort((a, b) => b.changePercent - a.changePercent);
    return this.selectedMoverType === 'gainers' ? sorted.slice(0, 5) : sorted.slice(-5).reverse();
  }

  goToStock(symbol: string) {
    this.router.navigate(['/market', symbol]);
  }

  addToWatchlist(symbol: string) {
    console.log('Adding to watchlist:', symbol);
  }

  createAlert(symbol: string) {
    console.log('Creating alert for:', symbol);
  }

  formatMarketCap(value: number): string {
    if (value >= 1000000000000) {
      return `$${(value / 1000000000000).toFixed(1)}T`;
    } else if (value >= 1000000000) {
      return `$${(value / 1000000000).toFixed(1)}B`;
    } else if (value >= 1000000) {
      return `$${(value / 1000000).toFixed(1)}M`;
    }
    return `$${value.toLocaleString()}`;
  }

  formatVolume(value: number): string {
    if (value >= 1000000) {
      return `${(value / 1000000).toFixed(1)}M`;
    } else if (value >= 1000) {
      return `${(value / 1000).toFixed(1)}K`;
    }
    return value.toString();
  }

  getSectorProgressWidth(changePercent: number): number {
    return Math.abs(changePercent * 10);
  }

  getCurrentTime(): string {
    return new Date().toLocaleTimeString('es-ES', { 
      hour: '2-digit', 
      minute: '2-digit',
      second: '2-digit'
    });
  }
}
