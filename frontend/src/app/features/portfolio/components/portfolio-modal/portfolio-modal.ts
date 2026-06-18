import { Component, Inject, OnInit } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { PortfolioService } from '../../services/portfolio.service';
import { BuyData, SellData, PortfolioModalData, PortfolioPosition, PortfolioModalResult } from '../../models/portfolio.modal.model';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { LanguageService } from '../../../../core/services/language.service';
import { ActivePortfolioService } from '../../../../core/services/active-portfolio.service';
import { UserPortfolio } from '../../models/portfolio.model';
import { MaterialModule } from '../../../../shared/material.module';
import { InfoTooltipComponent } from '../../../../shared/components/info-tooltip/info-tooltip.component';

/**
 * Modal de operaciones del portfolio: permite comprar un nuevo activo o vender
 * una posición existente, según el modo con el que se abra el diálogo.
 *
 * En modo compra busca el precio de mercado del ticker ingresado y calcula el
 * costo total de la operación. En modo venta carga la posición actual del activo
 * y calcula el total de la venta junto con la ganancia o pérdida resultante.
 * Al confirmar, cierra el diálogo devolviendo los datos de la operación para que
 * el componente que lo abrió (`Portfolio`) ejecute la compra o venta.
 */
@Component({
  selector: 'app-portfolio-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, MaterialModule, InfoTooltipComponent, TranslateModule],
  templateUrl: './portfolio-modal.html',
  styleUrl: './portfolio-modal.css'
})
export class PortfolioModal implements OnInit {

  // ── Modo y activo seleccionado ──
  mode: 'buy' | 'sell' = 'buy';
  symbol = '';

  // ── Datos de compra ──
  buyTicker = '';
  buyQuantity = 0;
  marketPrice = 0;
  isLoadingPrice = false;

  // ── Datos de venta ──
  sellQuantity = 0;
  position?: PortfolioPosition;

  // ── Portfolio destino de la operación ──
  selectedPortfolioId!: number;
  availableBalance = 0;

  /**
   * Inicializa el modal a partir de los datos recibidos al abrir el diálogo
   * (modo de operación y, opcionalmente, el símbolo del activo precargado).
   * @param dialogRef Referencia al diálogo, usada para cerrarlo y devolver el resultado de la operación.
   * @param data Datos de configuración del modal (modo `buy`/`sell` y símbolo del activo).
   * @param portfolioService Servicio para obtener posiciones y precios de mercado.
   * @param snackBarService Servicio para mostrar notificaciones al usuario.
   */
  constructor(
    private dialogRef: MatDialogRef<PortfolioModal, PortfolioModalResult>,
    @Inject(MAT_DIALOG_DATA) public data: PortfolioModalData,
    private portfolioService: PortfolioService,
    private snackBarService: SnackBarService,
    private languageService: LanguageService,
    private activePortfolioService: ActivePortfolioService
  ) {
    this.mode = data.mode;
    this.symbol = data.symbol ?? '';
    this.selectedPortfolioId = this.activePortfolioService.activeId!;

    if (this.mode === 'buy' && this.symbol)
      this.buyTicker = this.symbol;
  }

  // ── Ciclo de vida ──

  /**
   * Carga los datos iniciales según el modo del modal: la posición actual
   * (modo venta) o el precio de mercado y el saldo disponible (modo compra),
   * ambos para el portfolio seleccionado.
   */
  ngOnInit(): void {
    if (this.mode === 'sell' && this.symbol)
      this.loadPosition();

    if (this.mode === 'buy') {
      this.loadBalance();
      if (this.buyTicker) this.loadMarketPrice();
    }
  }

  // ── Selección de portfolio ──

  /** @returns Los portfolios del usuario, para mostrar el selector cuando tiene más de uno. */
  get portfolios(): UserPortfolio[] {
    return this.activePortfolioService.portfolios;
  }

  /** @returns `true` si el usuario tiene más de un portfolio y debe elegir a cuál aplicar la operación. */
  get showPortfolioSelector(): boolean {
    return this.portfolios.length > 1;
  }

  /**
   * Cambia el portfolio destino de la operación y recarga los datos que dependen
   * de él (la posición en modo venta, el saldo disponible en modo compra).
   * @param portfolioId Identificador del portfolio elegido.
   */
  onPortfolioChange(portfolioId: number): void {
    if (portfolioId === this.selectedPortfolioId) return;
    this.selectedPortfolioId = portfolioId;

    if (this.mode === 'sell') this.loadPosition();
    if (this.mode === 'buy') this.loadBalance();
  }

  // ── Carga de datos ──

  /** Obtiene el saldo disponible del portfolio seleccionado para validar el costo de la compra. */
  loadBalance(): void {
    this.portfolioService.getBalanceCards(this.selectedPortfolioId).subscribe({
      next: response => {
        this.availableBalance = response.data?.currentBalance ?? 0;
      },
      error: error => {
        console.error('loadBalance error', error);
        this.availableBalance = 0;
      }
    });
  }

  /**
   * Obtiene la posición actual del activo a vender. Si la operación falla o no
   * existe la posición, notifica el error al usuario y cierra el modal.
   */
  loadPosition(): void {
    this.portfolioService.getPosition(this.selectedPortfolioId, this.symbol).subscribe({
      next: response => {
        if (!response.success || !response.data) {
          this.snackBarService.fromResponse(response.success, response.code, response.message || this.languageService.instant('PORTFOLIO.ERRORS.POSITION_NOT_FOUND'));
          this.onClose();
          return;
        }

        this.position = response.data;
        this.sellQuantity = 1;
      },
      error: error => {
        console.error('loadPosition error', error);
        this.snackBarService.error(this.languageService.instant('PORTFOLIO.ERRORS.POSITION_NOT_FOUND'));
        this.onClose();
      }
    });
  }

  /**
   * Busca el precio de mercado actual del ticker ingresado en `buyTicker`.
   * No hace nada si el campo está vacío. Si el activo no existe o falla la
   * petición, deja el precio en cero y notifica al usuario.
   */
  loadMarketPrice(): void {
    const symbol = this.buyTicker.trim().toUpperCase();
    if (!symbol) return;
    this.buyTicker = symbol;

    this.isLoadingPrice = true;

    this.portfolioService.getAssetPrice(symbol).subscribe({
      next: response => {
        this.isLoadingPrice = false;

        if (!response.success || !response.data) {
          this.marketPrice = 0;
          this.snackBarService.fromResponse(response.success, response.code, response.message || this.languageService.instant('PORTFOLIO.ERRORS.ASSET_NOT_FOUND'));
          return;
        }

        this.marketPrice = response.data.currentPrice;
      },
      error: error => {
        console.error('loadMarketPrice error', error);
        this.isLoadingPrice = false;
        this.marketPrice = 0;
        this.snackBarService.error(this.languageService.instant('PORTFOLIO.ERRORS.PRICE_FETCH_FAILED'));
      }
    });
  }

  // ── Getters de presentación ──

  /** @returns El título del modal según el modo de operación actual. */
  get modalTitle(): string {
    return this.mode === 'buy'
      ? this.languageService.instant('PORTFOLIO.MODAL.OP_TITLE_BUY')
      : this.languageService.instant('PORTFOLIO.MODAL.OP_TITLE_SELL');
  }

  /** @returns El saldo disponible del portfolio seleccionado para realizar compras. */
  get currentBalance(): number {
    return this.availableBalance;
  }

  // ── Cálculos de compra ──

  /** @returns `true` si la operación de compra es válida (ticker, cantidad, precio y saldo suficientes). */
  get canBuy(): boolean {
    return !!(this.buyTicker.trim() && this.buyQuantity > 0 && this.marketPrice > 0 && this.totalCost <= this.currentBalance);
  }

  /** @returns El costo total de la compra (cantidad × precio de mercado). */
  get totalCost(): number {
    return this.buyQuantity * this.marketPrice;
  }

  // ── Cálculos de venta ──

  /** @returns `true` si la operación de venta es válida (existe posición y la cantidad es mayor a cero y no supera la tenencia). */
  get canSell(): boolean {
    return !!(this.position && this.sellQuantity > 0 && this.sellQuantity <= this.position.quantity);
  }

  /** @returns El total a recibir por la venta (cantidad × precio actual de la posición). */
  get sellTotal(): number {
    if (!this.position) return 0;
    return this.sellQuantity * this.position.currentPrice;
  }

  /** @returns La ganancia o pérdida resultante de la venta, en valor monetario. */
  get profitAmount(): number {
    if (!this.position) return 0;
    return this.sellQuantity * (this.position.currentPrice - this.position.avgPrice);
  }

  /** @returns La ganancia o pérdida resultante de la venta, expresada como porcentaje sobre el precio promedio de compra. */
  get profitPercent(): number {
    if (!this.position || this.position.avgPrice === 0) return 0;
    return ((this.position.currentPrice - this.position.avgPrice) / this.position.avgPrice) * 100;
  }

  // ── Acciones del usuario ──

  /** Cierra el modal sin confirmar ninguna operación. */
  onClose(): void {
    this.dialogRef.close();
  }

  /**
   * Confirma la operación de compra: si los datos son válidos, cierra el modal
   * devolviendo el ticker y la cantidad para que el componente que lo abrió
   * ejecute la compra.
   */
  onBuy(): void {
    if (!this.canBuy) return;

    const data: BuyData = {
      ticker: this.buyTicker.trim().toUpperCase(),
      quantity: this.buyQuantity,
      portfolioId: this.selectedPortfolioId
    };

    this.dialogRef.close({ mode: 'buy', data });
  }

  /**
   * Confirma la operación de venta: si los datos son válidos, cierra el modal
   * devolviendo el símbolo y la cantidad para que el componente que lo abrió
   * ejecute la venta.
   */
  onSell(): void {
    if (!this.canSell || !this.position)
      return;

    const data: SellData = {
      symbol: this.symbol,
      quantity: this.sellQuantity,
      portfolioId: this.selectedPortfolioId
    };

    this.dialogRef.close({ mode: 'sell', data });
  }
}
