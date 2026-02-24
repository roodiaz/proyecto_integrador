import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
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
export class Watchlist implements OnInit, OnDestroy {
  watchlistItems: WatchlistItem[] = [];
  newTicker: string = '';
  private subscription: Subscription | null = null;

  constructor() {}

  ngOnInit(): void {
    this.loadWatchlist();
    this.startLiveUpdates();
  }

  ngOnDestroy(): void {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }

  loadWatchlist(): void {
    // Datos de ejemplo para mostrar mientras el backend no está disponible
    this.watchlistItems = [
      { ticker: 'AAPL', currentPrice: 185.50, variationPercent: 2.5, isPositive: true },
      { ticker: 'GOOGL', currentPrice: 142.30, variationPercent: -1.2, isPositive: false },
      { ticker: 'MSFT', currentPrice: 380.75, variationPercent: 0.8, isPositive: true },
      { ticker: 'TSLA', currentPrice: 245.80, variationPercent: 3.2, isPositive: true }
    ];
  }

  startLiveUpdates(): void {
    // Simulación de actualización en vivo
    this.subscription = new Subscription();
  }

  addToWatchlist(): void {
    if (this.newTicker.trim()) {
      // Simulación de agregar a la lista
      const newItem: WatchlistItem = {
        ticker: this.newTicker.trim().toUpperCase(),
        currentPrice: Math.random() * 200 + 50,
        variationPercent: (Math.random() - 0.5) * 10,
        isPositive: Math.random() > 0.5
      };
      this.watchlistItems.push(newItem);
      this.newTicker = '';
    }
  }

  removeFromWatchlist(ticker: string): void {
    this.watchlistItems = this.watchlistItems.filter(item => item.ticker !== ticker);
  }
}
