import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatBottomSheetRef } from '@angular/material/bottom-sheet';
import { MaterialModule } from '../../../material.module';
import { AuthSessionService } from '../../../../core/services/auth-session.service';
import { Subscription, timer } from 'rxjs';
import { MarketPriceStatusService } from '../../services/market-price-status.service';
import { MarketStatusService } from '../../services/market-status.service';
import { MarketStatus } from '../../../../features/market/models/market.model';
import { LanguageService } from '../../../../core/services/language.service';

/**
 * Bottom sheet del ítem "Más" del bottom nav: agrupa las secciones que no
 * tienen lugar propio en la barra inferior (Watchlist, Notificaciones,
 * Configuración), el estado del mercado (sin lugar propio en mobile) y el
 * cierre de sesión.
 */
@Component({
  selector: 'app-more-sheet',
  standalone: true,
  imports: [MaterialModule, TranslateModule],
  templateUrl: './more-sheet.html',
  styleUrl: './more-sheet.css'
})
export class MoreSheet implements OnInit, OnDestroy {

  private readonly sheetRef = inject(MatBottomSheetRef<MoreSheet>);
  private readonly router = inject(Router);
  private readonly authSessionService = inject(AuthSessionService);
  private readonly languageService = inject(LanguageService);
  private readonly marketPriceStatusService = inject(MarketPriceStatusService);
  readonly marketStatusService = inject(MarketStatusService);

  pricesUpdatedAt: string | null = null;
  updatedText = '';
  marketStatus: MarketStatus | null = null;

  private statusSub?: Subscription;
  private clockSub?: Subscription;
  private marketStatusSub?: Subscription;

  ngOnInit(): void {
    this.statusSub = this.marketPriceStatusService.watchStatus().subscribe(updatedAt => {
      this.pricesUpdatedAt = updatedAt;
      this.updatedText = this.getUpdatedAgoText(updatedAt);
    });

    this.clockSub = timer(0, 1000).subscribe(() => {
      this.updatedText = this.getUpdatedAgoText(this.pricesUpdatedAt);
    });

    this.marketStatusSub = this.marketStatusService.watchStatus().subscribe(status => {
      this.marketStatus = status;
    });
  }

  ngOnDestroy(): void {
    this.statusSub?.unsubscribe();
    this.clockSub?.unsubscribe();
    this.marketStatusSub?.unsubscribe();
  }

  getStatusTooltip(): string {
    return `${this.marketStatusService.getStatusText(this.marketStatus)} · ${this.updatedText}`;
  }

  navigateTo(path: string, queryParams?: Record<string, string>): void {
    this.sheetRef.dismiss();
    this.router.navigate([path], queryParams ? { queryParams } : undefined);
  }

  logout(): void {
    this.sheetRef.dismiss();
    this.authSessionService.logout();
  }

  private getUpdatedAgoText(updatedAt?: string | null): string {
    if (!updatedAt) return this.languageService.instant('SIDEBAR.PRICES_PENDING');

    const updatedDate = new Date(updatedAt);
    const diffSeconds = Math.max(0, Math.floor((new Date().getTime() - updatedDate.getTime()) / 1000));

    if (diffSeconds < 10) return this.languageService.instant('SIDEBAR.PRICES_JUST_NOW');
    if (diffSeconds < 60) return this.languageService.instant('SIDEBAR.PRICES_SECONDS_AGO', { seconds: diffSeconds });

    const diffMinutes = Math.floor(diffSeconds / 60);
    const remainingSeconds = diffSeconds % 60;
    if (diffMinutes < 60) return this.languageService.instant('SIDEBAR.PRICES_MINUTES_AGO', { minutes: diffMinutes, seconds: remainingSeconds });

    const diffHours = Math.floor(diffMinutes / 60);
    if (diffHours === 1) return this.languageService.instant('SIDEBAR.PRICES_1_HOUR_AGO');

    return this.languageService.instant('SIDEBAR.PRICES_HOURS_AGO', { hours: diffHours });
  }
}
