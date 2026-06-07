import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { MatDialog } from '@angular/material/dialog';

import { MaterialModule } from '../../../../shared/material.module';
import { WatchlistService } from '../../services/watchlist.service';
import { FavoriteItem } from '../../models/watchlist-item';
import { AddFavoriteDialog } from '../add-favorite-dialog/add-favorite-dialog';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { InfoTooltipComponent } from '../../../../shared/components/info-tooltip/info-tooltip.component';

/**
 * Pantalla de lista de favoritos (watchlist).
 *
 * Muestra los activos que el usuario sigue, con su precio actual y variación,
 * permite buscarlos, ordenarlos, paginarlos, navegar a sus pantallas de mercado
 * o alertas, eliminarlos de la lista y agregar nuevos activos a través de un
 * diálogo de búsqueda.
 */
@Component({
  selector: 'app-watchlist',
  standalone: true,
  imports: [CommonModule, FormsModule, MaterialModule, InfoTooltipComponent],
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
    private notificationService: SnackBarService
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
      search: ''
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

  /** Ordena la lista de favoritos por símbolo, de la A a la Z. */
  sortBySymbol(): void {
    this.watchlistItems.sort((a, b) => a.symbol.localeCompare(b.symbol));
    this.applySearch();
  }

  /** Ordena la lista de favoritos por precio, de mayor a menor. */
  sortByPriceDesc(): void {
    this.watchlistItems.sort((a, b) => b.price - a.price);
    this.applySearch();
  }

  /** Ordena la lista de favoritos por precio, de menor a mayor. */
  sortByPriceAsc(): void {
    this.watchlistItems.sort((a, b) => a.price - b.price);
    this.applySearch();
  }

  /** Ordena la lista de favoritos por variación porcentual, de mayor a menor. */
  sortByVariationDesc(): void {
    this.watchlistItems.sort((a, b) => b.variationPercent - a.variationPercent);
    this.applySearch();
  }

  /** Ordena la lista de favoritos por variación porcentual, de menor a mayor. */
  sortByVariationAsc(): void {
    this.watchlistItems.sort((a, b) => a.variationPercent - b.variationPercent);
    this.applySearch();
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
    this.router.navigate(['/market'], { queryParams: { ticker: symbol } });
  }

  /**
   * Navega a la pantalla de alertas, precargando el activo indicado para crear una nueva alerta.
   * @param symbol Símbolo del activo para el cual crear la alerta.
   */
  goToAlerts(symbol: string): void {
    this.router.navigate(['/alerts'], { queryParams: { ticker: symbol } });
  }

  // ── Acciones sobre favoritos ──

  /**
   * Elimina un activo de la lista de favoritos y, si la operación es exitosa,
   * notifica al usuario y vuelve a cargar la lista.
   * @param symbol Símbolo del activo a eliminar de favoritos.
   */
  removeFromWatchlist(symbol: string): void {
    this.watchlistService.removeFavorite(symbol).subscribe({
      next: () => {
        this.notificationService.success('Favorito eliminado correctamente');
        this.loadWatchlist();
      },
      error: err => {
        this.notificationService.error(err.error?.message ?? 'Error al eliminar favorito');
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
