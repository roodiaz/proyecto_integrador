import { Component, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize, Subscription } from 'rxjs';
import { MatDialog } from '@angular/material/dialog';

import { MaterialModule } from '../../../../shared/material.module';
import { WatchlistService } from '../../services/watchlist.service';
import { FavoriteItem } from '../../models/watchlist-item';
import { AddFavoriteDialog } from '../add-favorite-dialog/add-favorite-dialog';
import { SnackBarService } from '../../../../core/services/snackbar.service';

@Component({
  selector: 'app-watchlist',
  standalone: true,
  imports: [CommonModule, FormsModule, MaterialModule],
  templateUrl: './watchlist.html',
  styleUrl: './watchlist.css'
})
export class Watchlist implements OnInit, OnDestroy, AfterViewInit {

  watchlistItems: FavoriteItem[] = [];
  filteredItems: FavoriteItem[] = [];
  ghostRows: null[] = [];

  searchTerm = '';

  readonly pageSize = 7;
  currentPage = 1;
  totalRecords = 0;

  currentFavorites = 0;
  maxFavorites = 0;

  loadingWatchlist = false;

  private liveSubscription: Subscription | null = null;

  constructor(
    private router: Router,
    private watchlistService: WatchlistService,
    private dialog: MatDialog,
    private notificationService: SnackBarService
  ) { }

  ngOnInit(): void {
    this.loadWatchlist();
  }

  ngAfterViewInit(): void {
    setTimeout(() => this.drawSparklines(), 100);
  }

  ngOnDestroy(): void {
    this.liveSubscription?.unsubscribe();
  }

  loadWatchlist(): void {
    this.loadingWatchlist = true;

    const filter = {
      page: this.currentPage,
      pageSize: this.pageSize,
      search: ''
    };

    this.watchlistService.getFavorites(filter)
      .pipe(finalize(() => this.loadingWatchlist = false))
      .subscribe({
        next: response => {
          if (!response.success || !response.data) return;

          this.watchlistItems = response.data.items;
          this.totalRecords = response.data.total;
          this.currentFavorites = response.data.currentFavorites;
          this.maxFavorites = response.data.maxFavorites;

          this.applySearch();
          setTimeout(() => this.drawSparklines());
        },
        error: err => {
          console.error(err);
          this.notificationService.error('No se pudieron cargar los favoritos');
        }
      });
  }

  onSearch(): void {
    this.applySearch();
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.applySearch();
  }

  private applySearch(): void {
    const term = this.searchTerm.trim().toLowerCase();

    this.filteredItems = term
      ? this.watchlistItems.filter(item => item.symbol.toLowerCase().includes(term) || item.name.toLowerCase().includes(term))
      : [...this.watchlistItems];

    this.updateGhostRows();
    setTimeout(() => this.drawSparklines());
  }

  private updateGhostRows(): void {
    const missing = this.pageSize - this.filteredItems.length;
    this.ghostRows = missing > 0 ? Array(missing).fill(null) : [];
  }

  getStartRecord(): number {
    return this.totalRecords === 0 ? 0 : (this.currentPage - 1) * this.pageSize + 1;
  }

  getEndRecord(): number {
    return Math.min(this.currentPage * this.pageSize, this.totalRecords);
  }

  getPageNumbers(): number[] {
    const total = Math.ceil(this.totalRecords / this.pageSize);
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  goToPage(page: number): void {
    if (page < 1 || page > Math.ceil(this.totalRecords / this.pageSize)) return;

    this.currentPage = page;
    this.loadWatchlist();
  }

  previousPage(): void {
    if (this.currentPage <= 1) return;

    this.currentPage--;
    this.loadWatchlist();
  }

  nextPage(): void {
    if (this.getEndRecord() >= this.totalRecords) return;

    this.currentPage++;
    this.loadWatchlist();
  }

  sortBySymbol(): void {
    this.watchlistItems.sort((a, b) => a.symbol.localeCompare(b.symbol));
    this.applySearch();
  }

  sortByPriceDesc(): void {
    this.watchlistItems.sort((a, b) => b.price - a.price);
    this.applySearch();
  }

  sortByPriceAsc(): void {
    this.watchlistItems.sort((a, b) => a.price - b.price);
    this.applySearch();
  }

  sortByVariationDesc(): void {
    this.watchlistItems.sort((a, b) => b.variationPercent - a.variationPercent);
    this.applySearch();
  }

  sortByVariationAsc(): void {
    this.watchlistItems.sort((a, b) => a.variationPercent - b.variationPercent);
    this.applySearch();
  }

  goToMarket(symbol: string): void {
    this.router.navigate(['/market'], { queryParams: { ticker: symbol } });
  }

  goToAlerts(symbol: string): void {
    this.router.navigate(['/alerts'], { queryParams: { ticker: symbol } });
  }

  removeFromWatchlist(symbol: string): void {
    this.watchlistService.removeFavorite(symbol).subscribe({
      next: () => {
        this.notificationService.success('Favorito eliminado correctamente');
        this.loadWatchlist();
      },
      error: err => {
        console.error(err);
        this.notificationService.error(err.error?.message ?? 'Error al eliminar favorito');
      }
    });
  }

  openAddFavoriteDialog(): void {
    const dialogRef = this.dialog.open(AddFavoriteDialog, {
      width: '420px',
      maxWidth: '95vw',
      panelClass: 'watchlist-dialog',
      backdropClass: 'blur-backdrop',
    });

    dialogRef.afterClosed().subscribe(symbol => {
      if (!symbol) return;

      this.watchlistService.addFavorite(symbol).subscribe({
        next: res => {
          this.notificationService.success(res.message || 'Favorito agregado correctamente');
          this.loadWatchlist();
        },
        error: err => {
          this.notificationService.error(err.error?.message ?? 'Error al agregar favorito');
        }
      });
    });
  }

  drawSparklines(): void {
    if (this.loadingWatchlist) return;

    this.filteredItems.forEach(item => {
      const canvas = document.getElementById(`sparkline-${item.symbol}`) as HTMLCanvasElement;
      if (!canvas) return;
    });
  }
}