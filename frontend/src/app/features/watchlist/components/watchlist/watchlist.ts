import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { MatDialog } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';

import { MaterialModule } from '../../../../shared/material.module';
import { WatchlistService } from '../../services/watchlist.service';
import { FavoriteItem } from '../../models/watchlist-item';
import { AddFavoriteDialog } from '../add-favorite-dialog/add-favorite-dialog';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { LanguageService } from '../../../../core/services/language.service';
import { ViewportService } from '../../../../core/services/viewport.service';
import { InfoTooltipComponent } from '../../../../shared/components/info-tooltip/info-tooltip.component';

@Component({
  selector: 'app-watchlist',
  standalone: true,
  imports: [CommonModule, FormsModule, MaterialModule, InfoTooltipComponent, TranslateModule],
  templateUrl: './watchlist.html',
  styleUrl: './watchlist.css'
})
export class Watchlist implements OnInit {

  // ── Datos de la lista ──
  watchlistItems: FavoriteItem[] = [];
  filteredItems: FavoriteItem[] = [];
  ghostRows: null[] = [];

  // ── Búsqueda ──
  searchTerm = '';

  // ── Ordenamiento ──
  sortField: string | null = null;
  sortDirection: 'asc' | 'desc' | null = null;

  // ── Paginación ──
  readonly pageSize = 7;
  currentPage = 1;
  totalRecords = 0;

  // ── Contador de favoritos ──
  currentFavorites = 0;
  maxFavorites = 0;

  // ── Estado de carga ──
  loadingWatchlist = false;

  constructor(
    private router: Router,
    private watchlistService: WatchlistService,
    private dialog: MatDialog,
    private notificationService: SnackBarService,
    private languageService: LanguageService,
    public viewportService: ViewportService
  ) { }

  // ── Ciclo de vida ──

  /** Carga la lista de favoritos al iniciar la pantalla. */
  ngOnInit(): void {
    this.loadWatchlist();
  }

  // ── Carga y recarga de la lista ──

  /**
   * Obtiene la página actual de favoritos del usuario junto con el contador
   * de favoritos usados/disponibles, y aplica la búsqueda activa sobre los
   * resultados. Si la petición falla o no devuelve datos, vacía la lista.
   */
  loadWatchlist(): void {
    this.loadingWatchlist = true;

    const filter = {
      page: this.currentPage,
      pageSize: this.pageSize,
      search: '',
      sortBy: this.sortField ?? undefined,
      sortDirection: this.sortDirection ?? undefined,
    };

    this.watchlistService.getFavorites(filter)
      .pipe(finalize(() => this.loadingWatchlist = false))
      .subscribe({
        next: response => {
          if (!response.success || !response.data) {
            this.resetList();
            return;
          }

          this.watchlistItems = response.data.items;
          this.totalRecords = response.data.total;
          this.currentFavorites = response.data.currentFavorites;
          this.maxFavorites = response.data.maxFavorites;

          this.applySearch();
        },
        error: err => {
          console.error('Error cargando favoritos', err);
          this.resetList();
        }
      });
  }

  // ── Búsqueda ──

  /** Vuelve a aplicar el filtro de búsqueda sobre la lista cargada (se dispara al escribir en el buscador). */
  onSearch(): void {
    this.applySearch();
  }

  /** Limpia el término de búsqueda y vuelve a mostrar la lista completa. */
  clearSearch(): void {
    this.searchTerm = '';
    this.applySearch();
  }

  // ── Ordenamiento ──

  /**
   * Ordena la tabla por la columna indicada, ciclando entre ascendente,
   * descendente y sin orden, y recarga la lista desde el servidor.
   * @param field Campo de ordenamiento (symbol | name | price | variationPercent).
   */
  onSort(field: string): void {
    if (this.sortField !== field) {
      this.sortField = field;
      this.sortDirection = 'asc';
    } else if (this.sortDirection === 'asc') {
      this.sortDirection = 'desc';
    } else {
      this.sortField = null;
      this.sortDirection = null;
    }

    this.currentPage = 1;
    this.loadWatchlist();
  }

  /** @returns El ícono de ordenamiento correspondiente al estado actual de la columna indicada. */
  getSortIcon(field: string): string {
    if (this.sortField !== field) return '↕';
    return this.sortDirection === 'asc' ? '↑' : '↓';
  }

  // ── Paginación ──

  /** @returns El número de registro inicial mostrado en la página actual (1-indexado, 0 si no hay registros). */
  getStartRecord(): number {
    return this.totalRecords === 0 ? 0 : (this.currentPage - 1) * this.pageSize + 1;
  }

  /** @returns El número de registro final mostrado en la página actual, sin exceder el total de registros. */
  getEndRecord(): number {
    return Math.min(this.currentPage * this.pageSize, this.totalRecords);
  }

  /** @returns El listado de números de página disponibles, de 1 al total de páginas. */
  getPageNumbers(): number[] {
    const total = Math.ceil(this.totalRecords / this.pageSize);
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  /**
   * Salta a una página específica de la lista y la recarga. No hace nada
   * si el número de página está fuera del rango válido.
   * @param page Número de página de destino.
   */
  goToPage(page: number): void {
    if (page < 1 || page > Math.ceil(this.totalRecords / this.pageSize)) return;

    this.currentPage = page;
    this.loadWatchlist();
  }

  /** Retrocede una página en la lista y la recarga. No hace nada si ya está en la primera página. */
  previousPage(): void {
    if (this.currentPage <= 1) return;

    this.currentPage--;
    this.loadWatchlist();
  }

  /** Avanza una página en la lista y la recarga. No hace nada si ya se muestra el último registro. */
  nextPage(): void {
    if (this.getEndRecord() >= this.totalRecords) return;

    this.currentPage++;
    this.loadWatchlist();
  }

  // ── Navegación a otras pantallas ──

  /**
   * Navega a la pantalla de mercado, mostrando el activo indicado.
   * @param symbol Símbolo del activo a mostrar en la pantalla de mercado.
   */
  goToMarket(symbol: string): void {
    const normalizedSymbol = symbol.trim().toUpperCase();
    this.router.navigate(['/market'], { queryParams: { ticker: normalizedSymbol } });
  }

  /**
   * Navega a la pantalla de alertas, precargando el activo indicado para crear una nueva alerta.
   * @param symbol Símbolo del activo para el cual crear la alerta.
   */
  goToAlerts(symbol: string): void {
    const normalizedSymbol = symbol.trim().toUpperCase();
    this.router.navigate(['/alerts'], { queryParams: { ticker: normalizedSymbol } });
  }

  // ── Acciones sobre favoritos ──

  /**
   * Elimina un activo de la lista de favoritos y, si la operación es exitosa,
   * notifica al usuario y vuelve a cargar la lista.
   * @param symbol Símbolo del activo a eliminar de favoritos.
   */
  async removeFromWatchlist(symbol: string): Promise<void> {
    const normalizedSymbol = symbol.trim().toUpperCase();

    const ConfirmDialog = await import('../../../../shared/components/confirm-dialog/confirm-dialog');
    const dialogRef = this.dialog.open(ConfirmDialog.ConfirmDialogComponent, {
      width: '380px',
      maxWidth: '95vw',
      backdropClass: 'blur-backdrop',
      data: {
        title: this.languageService.instant('WATCHLIST.DELETE_CONFIRM.TITLE'),
        message: this.languageService.instant('WATCHLIST.DELETE_CONFIRM.MESSAGE', { symbol: normalizedSymbol })
      }
    });

    const result = await dialogRef.afterClosed().toPromise();
    if (!result) return;

    this.watchlistService.removeFavorite(normalizedSymbol).subscribe({
      next: response => {
        if (response.success)
          this.notificationService.success(this.languageService.instant('MARKET.WATCHLIST_REMOVED', { symbol: normalizedSymbol }));
        else
          this.notificationService.fromResponse(false, response.code, response.message);
        if (response.success) this.loadWatchlist();
      },
      error: err => {
        this.notificationService.fromResponse(false, err.error?.code, err.error?.message);
      }
    });
  }

  /**
   * Abre el diálogo de búsqueda de activos y, si el usuario elige un símbolo,
   * lo agrega a favoritos, notifica el resultado y recarga la lista.
   */
  openAddFavoriteDialog(): void {
    const dialogRef = this.dialog.open(AddFavoriteDialog, {
      width: '420px',
      maxWidth: '95vw',
      panelClass: this.viewportService.isMobile() ? ['watchlist-dialog', 'mobile-fullscreen-dialog'] : 'watchlist-dialog',
      position: this.viewportService.isMobile() ? { top: '0' } : undefined,
      backdropClass: 'blur-backdrop',
    });

    dialogRef.afterClosed().subscribe(symbol => {
      if (!symbol) return;

      const normalizedSymbol = symbol.trim().toUpperCase();
      if (!normalizedSymbol) return;

      this.watchlistService.addFavorite(normalizedSymbol).subscribe({
        next: res => {
          if (res.success)
            this.notificationService.success(this.languageService.instant('MARKET.WATCHLIST_ADDED', { symbol: normalizedSymbol }));
          else
            this.notificationService.fromResponse(false, res.code, res.message);
          if (res.success) this.loadWatchlist();
        },
        error: err => {
          this.notificationService.fromResponse(false, err.error?.code, err.error?.message);
        }
      });
    });
  }

  // ── Helpers privados ──

  /**
   * Filtra la lista de favoritos cargada según el término de búsqueda actual
   * (por símbolo o nombre de la empresa, sin distinguir mayúsculas) y
   * recalcula las filas fantasma para mantener la altura de la tabla.
   */
  private applySearch(): void {
    const term = this.searchTerm.trim().toLowerCase();

    this.filteredItems = term
      ? this.watchlistItems.filter(item =>
          item.symbol.toLowerCase().includes(term) ||
          item.name.toLowerCase().includes(term)
        )
      : [...this.watchlistItems];

    this.updateGhostRows();
  }

  /** Recalcula las filas fantasma necesarias para que la tabla mantenga una altura constante. */
  private updateGhostRows(): void {
    const missing = this.pageSize - this.filteredItems.length;
    this.ghostRows = missing > 0 ? Array(missing).fill(null) : [];
  }

  /** Vacía la lista de favoritos y sus contadores, y recalcula las filas fantasma. */
  private resetList(): void {
    this.watchlistItems = [];
    this.filteredItems = [];
    this.totalRecords = 0;
    this.currentFavorites = 0;
    this.maxFavorites = 0;
    this.updateGhostRows();
  }
}
