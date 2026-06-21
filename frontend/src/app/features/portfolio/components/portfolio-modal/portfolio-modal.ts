import { Component, Inject, OnInit } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { PortfolioService } from '../../services/portfolio.service';
import { BuyData, SellData, PortfolioModalData, PortfolioPosition, PortfolioModalResult } from '../../models/portfolio.modal.model';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { LanguageService } from '../../../../core/services/language.service';
import { ActivePortfolioService } from '../../../../core/services/active-portfolio.service';
import { UserPortfolio } from '../../models/portfolio.model';
import { MaterialModule } from '../../../../shared/material.module';
import { InfoTooltipComponent } from '../../../../shared/components/info-tooltip/info-tooltip.component';

/**
 * Códigos de error que el backend devuelve cuando un portfolio puntual
 * genuinamente no tiene una posición vendible del activo (sin posición, sin
 * portfolio o activo inexistente). Cualquier otro código (p. ej. el precio de
 * mercado no disponible) es una falla real al verificar, no una ausencia de
 * posición, y no debe interpretarse como "no tenés este activo".
 */
const NO_POSITION_CODES = ['POSITION_NOT_FOUND', 'PORTFOLIO_NOT_FOUND', 'ASSET_NOT_FOUND'];

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
  loadingPosition = false;

  // ── Portfolio destino de la operación ──
  selectedPortfolioId!: number;
  availableBalance = 0;

  /** Portfolios donde el usuario tiene una posición abierta (cantidad > 0) del activo a vender. */
  private sellablePortfolios: UserPortfolio[] = [];
  private positionsBySellablePortfolio = new Map<number, PortfolioPosition>();

  /**
   * `true` cuando la venta llega con un portfolio ya fijado (p. ej. desde la
   * pantalla Portfolio, parado sobre uno): la operación es directamente sobre
   * ese portfolio, sin buscar entre el resto ni mostrar el selector.
   */
  private scopedToPortfolio = false;

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
    this.scopedToPortfolio = this.mode === 'sell' && data.portfolioId != null;
    this.selectedPortfolioId = data.portfolioId ?? this.activePortfolioService.activeId!;

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
    if (this.mode === 'sell' && this.symbol) {
      if (this.scopedToPortfolio) this.loadScopedPosition();
      else this.loadSellablePositions();
    }

    if (this.mode === 'buy') {
      this.ensurePortfoliosLoaded().subscribe(portfolios => {
        if (portfolios.length === 0) return;

        if (!this.selectedPortfolioId) {
          const active = portfolios.find(p => p.id === this.activePortfolioService.activeId);
          this.selectedPortfolioId = (active ?? portfolios[0]).id;
        }
        this.loadBalance();
      });

      if (this.buyTicker) this.loadMarketPrice();
    }
  }

  /**
   * Asegura que la lista de portfolios del usuario esté cargada antes de operar con
   * ella. `activePortfolioService.portfolios` solo se carga al visitar Dashboard o
   * Portfolio en la sesión — si el modal se abre desde otro lado (p. ej. el FAB Operar
   * en mobile) puede estar vacía todavía.
   */
  private ensurePortfoliosLoaded() {
    return this.activePortfolioService.portfolios.length > 0
      ? of(this.activePortfolioService.portfolios)
      : this.activePortfolioService.loadPortfolios().pipe(map(() => this.activePortfolioService.portfolios));
  }

  // ── Selección de portfolio ──

  /**
   * @returns Los portfolios entre los que el usuario puede elegir para esta operación:
   * todos en modo compra, o solo aquellos donde tiene una posición abierta del activo
   * en modo venta.
   */
  get portfolios(): UserPortfolio[] {
    return this.mode === 'sell' ? this.sellablePortfolios : this.activePortfolioService.portfolios;
  }

  /**
   * @returns `true` si corresponde mostrar el selector de portfolio: en compra, solo si hay
   * más de uno para elegir; en venta, siempre que se haya buscado el activo entre todos los
   * portfolios (aunque el resultado sea uno solo) — nunca si la venta ya viene fija a uno.
   */
  get showPortfolioSelector(): boolean {
    if (this.mode === 'sell') return !this.scopedToPortfolio;
    return this.portfolios.length > 1;
  }

  /**
   * Cambia el portfolio destino de la operación y actualiza los datos que dependen
   * de él (la posición en modo venta, el saldo disponible en modo compra).
   * @param portfolioId Identificador del portfolio elegido.
   */
  onPortfolioChange(portfolioId: number): void {
    if (portfolioId === this.selectedPortfolioId) return;
    this.selectedPortfolioId = portfolioId;

    if (this.mode === 'sell') {
      this.position = this.positionsBySellablePortfolio.get(portfolioId);
      this.sellQuantity = 1;
    }

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
   * Busca, entre todos los portfolios del usuario, en cuáles tiene una posición abierta
   * (cantidad > 0) del activo a vender. Si no la tiene en ninguno, notifica el error y
   * cierra el modal; en caso contrario, preselecciona el portfolio activo (si tiene
   * posición ahí) o el primero disponible.
   *
   * `activePortfolioService.portfolios` solo se carga cuando se visitó Dashboard o
   * Portfolio en la sesión — si se llega acá desde otro lado (p. ej. el FAB Operar en
   * mobile, sin haber pasado por esas pantallas) puede estar vacío todavía, así que lo
   * cargamos primero en vez de asumir que ya está listo.
   */
  private loadSellablePositions(): void {
    this.loadingPosition = true;

    this.ensurePortfoliosLoaded().subscribe(portfolios => {
      if (portfolios.length === 0) {
        this.loadingPosition = false;
        this.snackBarService.error(this.languageService.instant('PORTFOLIO.ERRORS.POSITION_NOT_FOUND'));
        this.onClose();
        return;
      }

      forkJoin(
        portfolios.map(portfolio =>
          this.portfolioService.getPosition(portfolio.id, this.symbol).pipe(
            map(response => ({ portfolio, position: response.success ? response.data ?? null : null, checkFailed: false })),
            catchError((err: HttpErrorResponse) => {
              const checkFailed = !NO_POSITION_CODES.includes(err.error?.code);
              return of({ portfolio, position: null as PortfolioPosition | null, checkFailed });
            })
          )
        )
      ).subscribe(results => {
        this.loadingPosition = false;

        for (const { portfolio, position } of results) {
          if (position && position.quantity > 0) {
            this.sellablePortfolios.push(portfolio);
            this.positionsBySellablePortfolio.set(portfolio.id, position);
          }
        }

        if (this.sellablePortfolios.length === 0) {
          // Si no encontramos posición vendible pero alguna verificación falló por una razón
          // real (no "no tiene posición"), no afirmamos que el usuario no tiene el activo:
          // avisamos que no pudimos confirmarlo y lo invitamos a reintentar.
          const anyCheckFailed = results.some(r => r.checkFailed);
          const messageKey = anyCheckFailed ? 'PORTFOLIO.ERRORS.POSITION_CHECK_FAILED' : 'PORTFOLIO.ERRORS.POSITION_NOT_FOUND';
          this.snackBarService.error(this.languageService.instant(messageKey));
          this.onClose();
          return;
        }

        const preferred = this.sellablePortfolios.find(p => p.id === this.selectedPortfolioId);
        this.selectedPortfolioId = (preferred ?? this.sellablePortfolios[0]).id;
        this.position = this.positionsBySellablePortfolio.get(this.selectedPortfolioId);
        this.sellQuantity = 1;
      });
    });
  }

  /**
   * Carga la posición del activo directamente en el portfolio ya fijado (sin buscar
   * en el resto). Si no tiene una posición abierta ahí, notifica el error y cierra el modal.
   */
  private loadScopedPosition(): void {
    this.loadingPosition = true;

    this.portfolioService.getPosition(this.selectedPortfolioId, this.symbol).pipe(
      map(response => ({ position: response.success ? response.data ?? null : null, checkFailed: false })),
      catchError((err: HttpErrorResponse) => {
        const checkFailed = !NO_POSITION_CODES.includes(err.error?.code);
        return of({ position: null as PortfolioPosition | null, checkFailed });
      })
    ).subscribe(({ position, checkFailed }) => {
      this.loadingPosition = false;

      if (!position || position.quantity <= 0) {
        const messageKey = checkFailed ? 'PORTFOLIO.ERRORS.POSITION_CHECK_FAILED' : 'PORTFOLIO.ERRORS.POSITION_NOT_FOUND';
        this.snackBarService.error(this.languageService.instant(messageKey));
        this.onClose();
        return;
      }

      this.position = position;
      this.sellQuantity = 1;
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
