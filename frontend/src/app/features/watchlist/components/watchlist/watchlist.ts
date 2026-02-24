import { Component, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { WatchlistService } from '../../services/watchlist.service';
import { WatchlistItem } from '../../models/watchlist-item';

@Component({
  selector: 'app-watchlist',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './watchlist.html',
  styleUrl: './watchlist.css'
})
export class Watchlist implements OnInit, OnDestroy, AfterViewInit {
  watchlistItems: WatchlistItem[] = [];
  newTicker: string = '';
  searchQuery: string = '';
  maxFavorites: number = 3;
  private subscription: Subscription | null = null;

  constructor(private router: Router) {}

  ngOnInit(): void {
    this.loadWatchlist();
    this.startLiveUpdates();
  }

  ngOnDestroy(): void {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }

  ngAfterViewInit(): void {
    // Dibujar sparklines después de que la vista se renderice
    setTimeout(() => {
      this.drawSparklines();
    }, 100);
  }

  loadWatchlist(): void {
    // Datos de ejemplo para mostrar mientras el backend no está disponible
    this.watchlistItems = [
      { ticker: 'AAPL', currentPrice: 185.50, variationPercent: 2.5, isPositive: true },
      { ticker: 'GOOGL', currentPrice: 142.30, variationPercent: -1.2, isPositive: false },
      { ticker: 'MSFT', currentPrice: 380.75, variationPercent: 0.8, isPositive: true }
    ];
  }

  startLiveUpdates(): void {
    // Simulación de actualización en vivo
    this.subscription = new Subscription();
  }

  isLimitReached(): boolean {
    return this.watchlistItems.length >= this.maxFavorites;
  }

  addToWatchlist(): void {
    if (this.newTicker.trim() && !this.isLimitReached()) {
      // Simulación de agregar a la lista
      const newItem: WatchlistItem = {
        ticker: this.newTicker.trim().toUpperCase(),
        currentPrice: Math.random() * 200 + 50,
        variationPercent: (Math.random() - 0.5) * 10,
        isPositive: Math.random() > 0.5
      };
      this.watchlistItems.push(newItem);
      this.newTicker = '';
      
      // Redibujar sparklines después de agregar
      setTimeout(() => {
        this.drawSparklines();
      }, 100);
    }
  }

  removeFromWatchlist(ticker: string): void {
    this.watchlistItems = this.watchlistItems.filter(item => item.ticker !== ticker);
  }

  goToMarket(ticker: string): void {
    // Navegar a la página de mercado con el ticker pre-cargado
    this.router.navigate(['/market'], { queryParams: { ticker: ticker } });
  }

  searchTicker(): void {
    if (this.searchQuery.trim()) {
      // Navegar a la página de mercado con el ticker de búsqueda
      this.router.navigate(['/market'], { queryParams: { ticker: this.searchQuery.trim().toUpperCase() } });
    }
  }

  drawSparklines(): void {
    this.watchlistItems.forEach(item => {
      const canvas = document.getElementById(`sparkline-${item.ticker}`) as HTMLCanvasElement;
      if (canvas) {
        this.drawSparkline(canvas, item.isPositive);
      }
    });
  }

  drawSparkline(canvas: HTMLCanvasElement, isPositive: boolean): void {
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const width = canvas.width;
    const height = canvas.height;
    
    // Generar datos de ejemplo para el sparkline
    const points = 20;
    const data: number[] = [];
    let currentValue = 50;
    
    for (let i = 0; i < points; i++) {
      currentValue += (Math.random() - 0.5) * 10;
      currentValue = Math.max(10, Math.min(90, currentValue));
      data.push(currentValue);
    }

    // Limpiar canvas
    ctx.clearRect(0, 0, width, height);

    // Encontrar min y max para escalar
    const minValue = Math.min(...data);
    const maxValue = Math.max(...data);
    const range = maxValue - minValue || 1;

    // Dibujar la línea
    ctx.strokeStyle = isPositive ? '#10b981' : '#ef4444';
    ctx.lineWidth = 2;
    ctx.beginPath();

    data.forEach((value, index) => {
      const x = (index / (data.length - 1)) * width;
      const y = height - ((value - minValue) / range) * height;
      
      if (index === 0) {
        ctx.moveTo(x, y);
      } else {
        ctx.lineTo(x, y);
      }
    });

    ctx.stroke();

    // Agregar gradiente sutil
    const gradient = ctx.createLinearGradient(0, 0, 0, height);
    if (isPositive) {
      gradient.addColorStop(0, 'rgba(16, 185, 129, 0.2)');
      gradient.addColorStop(1, 'rgba(16, 185, 129, 0)');
    } else {
      gradient.addColorStop(0, 'rgba(239, 68, 68, 0.2)');
      gradient.addColorStop(1, 'rgba(239, 68, 68, 0)');
    }

    ctx.fillStyle = gradient;
    ctx.lineTo(width, height);
    ctx.lineTo(0, height);
    ctx.closePath();
    ctx.fill();
  }
}
