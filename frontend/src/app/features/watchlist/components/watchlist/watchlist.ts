import { Component, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { MaterialModule } from '../../../../shared/material.module';
import { WatchlistService } from '../../services/watchlist.service';
import { FavoriteItem } from '../../models/watchlist-item';
import { MatDialog } from '@angular/material/dialog';
import { AddFavoriteDialog } from '../add-favorite-dialog/add-favorite-dialog';
import { SnackBarService } from '../../../../core/services/snackbar.service';

@Component({
  selector: 'app-watchlist',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MaterialModule
  ],
  templateUrl: './watchlist.html',
  styleUrl: './watchlist.css'
})
export class Watchlist implements OnInit, OnDestroy, AfterViewInit {

  watchlistItems: FavoriteItem[] = [];
  searchTerm = '';
  pageSize = 10;
  currentPage = 1;
  totalRecords = 0;
  currentFavorites: number = 0;
  maxFavorites: number = 0;
  private subscription: Subscription | null = null;

  constructor(
    private router: Router,
    private watchlistService: WatchlistService,
    private dialog: MatDialog,
    private notificationService: SnackBarService
  ) { }

  ngOnInit(): void {
    this.loadWatchlist();
    this.startLiveUpdates();
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  ngAfterViewInit(): void {

    setTimeout(() => {
      this.drawSparklines();
    }, 100);

  }

  loadFavoritesCount(): void {

    this.currentFavorites =
      this.watchlistItems.length;

  }

  loadWatchlist(): void {

    const filter = {
      page: this.currentPage,
      pageSize: this.pageSize,
      search: this.searchTerm
    };

    this.watchlistService
      .getFavorites(filter)
      .subscribe({

        next: (response) => {

          if (!response.success || !response.data)
            return;

          this.watchlistItems = response.data.items;
          this.totalRecords = response.data.total;
          this.currentFavorites = response.data.currentFavorites;
          this.maxFavorites = response.data.maxFavorites;

          setTimeout(() => {
            this.drawSparklines();
          });
        },

        error: (error) => {
          console.error(error);
        }

      });

  }

  startLiveUpdates(): void {
    this.subscription = new Subscription();
  }

  onSearch(): void {
    this.currentPage = 1;
    this.loadWatchlist();
  }

  removeFromWatchlist(symbol: string): void {

    this.watchlistService
      .removeFavorite(symbol)
      .subscribe({
        next: () => this.loadWatchlist(),
        error: err => console.error(err)
      });

  }

  goToMarket(symbol: string): void {

    this.router.navigate(
      ['/market'],
      {
        queryParams: { ticker: symbol }
      }
    );

  }

  goToAlerts(symbol: string): void {

    this.router.navigate(
      ['/alerts'],
      {
        queryParams: { ticker: symbol }
      }
    );

  }

  drawSparklines(): void {

    this.watchlistItems.forEach(item => {

      const canvas =
        document.getElementById(
          `sparkline-${item.symbol}`
        ) as HTMLCanvasElement;

      if (!canvas)
        return;

    });

  }

  openAddFavoriteDialog(): void {

    const dialogRef = this.dialog.open(
      AddFavoriteDialog,
      {
        width: '420px',
        maxWidth: '95vw',
        panelClass: 'watchlist-dialog'
      }
    );

    dialogRef.afterClosed()
      .subscribe(symbol => {

        if (!symbol)
          return;

        this.watchlistService
          .addFavorite(symbol)
          .subscribe({

            next: (response) => {
              this.notificationService.success(response.message || 'Favorito agregado correctamente');
              this.loadWatchlist();
            },

            error: (error) => {
              this.notificationService.error(error.error?.message ?? 'Error al agregar favorito');
            }
          });
      });
  }

  sortBySymbol(): void {

    this.watchlistItems.sort(
      (a, b) =>
        a.symbol.localeCompare(b.symbol)
    );

  }

  sortByPriceDesc(): void {
    this.watchlistItems.sort(
      (a, b) =>
        b.price - a.price
    );
  }

  sortByPriceAsc(): void {
    this.watchlistItems.sort(
      (a, b) =>
        a.price - b.price
    );
  }

  sortByVariationDesc(): void {
    this.watchlistItems.sort(
      (a, b) =>
        b.variationPercent -
        a.variationPercent
    );
  }

  sortByVariationAsc(): void {
    this.watchlistItems.sort(
      (a, b) =>
        a.variationPercent -
        b.variationPercent
    );
  }

  getStartRecord(): number {

    if (this.totalRecords === 0)
      return 0;

    return (
      (this.currentPage - 1) *
      this.pageSize
    ) + 1;

  }

  getEndRecord(): number {

    return Math.min(
      this.currentPage *
      this.pageSize,
      this.totalRecords
    );

  }

  previousPage(): void {

    if (this.currentPage <= 1)
      return;

    this.currentPage--;
    this.loadWatchlist();

  }

  nextPage(): void {

    const totalPages =
      Math.ceil(
        this.totalRecords /
        this.pageSize
      );

    if (this.currentPage >= totalPages)
      return;

    this.currentPage++;
    this.loadWatchlist();
  }
}