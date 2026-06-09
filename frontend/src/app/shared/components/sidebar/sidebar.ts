import { Component, OnDestroy, inject } from '@angular/core';
import { SidebarService } from '../../../core/services/sidebar.service';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MaterialModule } from '../../material.module';
import { AuthSessionService } from '../../../core/services/auth-session.service';
import { Subscription, timer } from 'rxjs';
import { MarketPriceStatusService } from '../../../shared/components/services/market-price-status.service';
import { MarketStatusService } from '../../../shared/components/services/market-status.service';
import { MarketStatus } from '../../../features/market/models/market.model';
import { TranslateModule } from '@ngx-translate/core';
import { LanguageService } from '../../../core/services/language.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MaterialModule,
    TranslateModule
  ],
  templateUrl: './sidebar.html',
  styleUrls: ['./sidebar.css']
})
export class Sidebar implements OnDestroy {

  isCollapsed = false;
  pricesUpdatedAt: string | null = null;
  updatedText = '';
  marketStatus: MarketStatus | null = null;

  private readonly languageService = inject(LanguageService);
  private sidebarSub?: Subscription;
  private statusSub?: Subscription;
  private clockSub?: Subscription;
  private marketStatusSub?: Subscription;

  constructor(
    private sidebarService: SidebarService,
    private authSessionService: AuthSessionService,
    private marketPriceStatusService: MarketPriceStatusService,
    public marketStatusService: MarketStatusService
  ) {
    this.sidebarSub = this.sidebarService.isCollapsed$.subscribe(isCollapsed => {
      this.isCollapsed = isCollapsed;
    });

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

  // ── Ciclo de vida ──

  /** Cancela las suscripciones activas (colapso, estado de precios, estado de mercado y reloj) al destruir el componente. */
  ngOnDestroy(): void {
    this.sidebarSub?.unsubscribe();
    this.statusSub?.unsubscribe();
    this.marketStatusSub?.unsubscribe();
    this.clockSub?.unsubscribe();
  }

  getStatusTooltip(): string {
    return `${this.marketStatusService.getStatusText(this.marketStatus)} · ${this.updatedText}`;
  }

  getLastUpdateTimeText(): string {
    if (!this.pricesUpdatedAt) return this.languageService.instant('SIDEBAR.NO_DATA');

    const updatedDate = new Date(this.pricesUpdatedAt);
    const hours = updatedDate.getHours().toString().padStart(2, '0');
    const minutes = updatedDate.getMinutes().toString().padStart(2, '0');
    return `${hours}:${minutes} hs`;
  }

  // ── Acciones del usuario ──

  /** Alterna el estado de colapso de la barra lateral a través del servicio compartido. */
  toggleSidebar() {
    this.sidebarService.toggle();
  }

  /** Cierra la sesión del usuario actual. */
  logout() {
    this.authSessionService.logout();
  }

  // ── Helpers privados ──

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
