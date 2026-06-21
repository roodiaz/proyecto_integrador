import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatBottomSheetRef } from '@angular/material/bottom-sheet';
import { MatDialog } from '@angular/material/dialog';
import { MaterialModule } from '../../../material.module';
import { ViewportService } from '../../../../core/services/viewport.service';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { LanguageService } from '../../../../core/services/language.service';
import { PortfolioService } from '../../../../features/portfolio/services/portfolio.service';
import { PortfolioModal } from '../../../../features/portfolio/components/portfolio-modal/portfolio-modal';
import { BuyData, SellData, PortfolioModalResult } from '../../../../features/portfolio/models/portfolio.modal.model';
import { SellTickerModal } from '../../sell-ticker-modal/sell-ticker-modal';

/**
 * Bottom sheet de la acción central "Operar" del bottom nav. Comprar abre
 * directamente el modal de compra (igual que "Nueva Operación" en Portfolio),
 * sin precargar ningún ticker — el usuario lo busca dentro del modal. Vender
 * primero pide el ticker (acá no hay ningún activo preseleccionado) y luego
 * abre el modal de Vender existente, que ya sabe buscar en qué portfolio(s)
 * el usuario tiene esa posición. Crear alerta abre su propio modal directo,
 * porque tampoco depende de un activo preexistente.
 */
@Component({
  selector: 'app-operate-sheet',
  standalone: true,
  imports: [MaterialModule, TranslateModule],
  templateUrl: './operate-sheet.html',
  styleUrl: './operate-sheet.css'
})
export class OperateSheet {

  private readonly sheetRef = inject(MatBottomSheetRef<OperateSheet>);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly viewportService = inject(ViewportService);
  private readonly portfolioService = inject(PortfolioService);
  private readonly snackBarService = inject(SnackBarService);
  private readonly languageService = inject(LanguageService);

  /** Abre el modal de compra directamente, sin ticker precargado (el usuario lo busca adentro). */
  openBuy(): void {
    this.sheetRef.dismiss();

    const dialogRef = this.dialog.open(PortfolioModal, {
      width: '560px',
      maxWidth: '95vw',
      autoFocus: false,
      restoreFocus: false,
      backdropClass: 'blur-backdrop',
      panelClass: this.viewportService.isMobile() ? ['portfolio-dialog-panel', 'mobile-fullscreen-dialog'] : 'portfolio-dialog-panel',
      position: this.viewportService.isMobile() ? { top: '0' } : undefined,
      data: { mode: 'buy' },
    });

    dialogRef.afterClosed().subscribe((result?: PortfolioModalResult) => {
      if (result?.mode === 'buy') this.onBuyComplete(result.data);
    });
  }

  /** Pide el ticker a vender y, con él, abre el modal de Vender existente. */
  goToSell(): void {
    this.sheetRef.dismiss();

    const promptRef = this.dialog.open(SellTickerModal, {
      width: '420px',
      maxWidth: '95vw',
      autoFocus: false,
      restoreFocus: false,
      backdropClass: 'blur-backdrop',
      panelClass: this.viewportService.isMobile() ? 'mobile-fullscreen-dialog' : undefined,
      position: this.viewportService.isMobile() ? { top: '0' } : undefined,
    });

    promptRef.afterClosed().subscribe((symbol?: string) => {
      if (symbol) this.openSell(symbol);
    });
  }

  createAlert(): void {
    this.sheetRef.dismiss();
    this.router.navigate(['/alerts'], { queryParams: { action: 'create' } });
  }

  /**
   * Abre el modal de Vender para el símbolo indicado. Ese modal busca por su
   * cuenta en qué portfolio(s) el usuario tiene esa posición — si no la tiene
   * en ninguno, muestra el error y se cierra solo.
   */
  private openSell(symbol: string): void {
    const dialogRef = this.dialog.open(PortfolioModal, {
      width: '560px',
      maxWidth: '95vw',
      autoFocus: false,
      restoreFocus: false,
      backdropClass: 'blur-backdrop',
      panelClass: this.viewportService.isMobile() ? ['portfolio-dialog-panel', 'mobile-fullscreen-dialog'] : 'portfolio-dialog-panel',
      position: this.viewportService.isMobile() ? { top: '0' } : undefined,
      data: { mode: 'sell', symbol },
    });

    dialogRef.afterClosed().subscribe((result?: PortfolioModalResult) => {
      if (result?.mode === 'sell') this.onSellComplete(result.data);
    });
  }

  private onBuyComplete(data: BuyData): void {
    this.portfolioService.buyAsset(data.portfolioId, data.ticker, data.quantity).subscribe({
      next: response => {
        this.snackBarService.fromResponse(response.success, response.code, response.message);
      },
      error: error => {
        this.snackBarService.fromResponse(false, error?.error?.code, error?.error?.message || this.languageService.instant('PORTFOLIO.ERRORS.BUY_ERROR'));
      }
    });
  }

  private onSellComplete(data: SellData): void {
    this.portfolioService.sell(data.portfolioId, data).subscribe({
      next: response => {
        this.snackBarService.fromResponse(response.success, response.code, response.message);
      },
      error: error => {
        this.snackBarService.fromResponse(false, error?.error?.code, error?.error?.message || this.languageService.instant('PORTFOLIO.ERRORS.SELL_ERROR'));
      }
    });
  }
}
