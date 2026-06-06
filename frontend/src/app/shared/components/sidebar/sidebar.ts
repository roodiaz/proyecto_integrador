import { Component, OnDestroy } from '@angular/core';
import { SidebarService } from '../../../core/services/sidebar.service';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { MaterialModule } from '../../material.module';
import { AuthSessionService } from '../../../core/services/auth-session.service';
import { Subscription, timer } from 'rxjs';
import { MarketPriceStatusService } from '../../../shared/components/services/market-price-status.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MaterialModule
  ],
  templateUrl: './sidebar.html',
  styleUrls: ['./sidebar.css']
})
export class Sidebar implements OnDestroy {
  isCollapsed = false;
  pricesUpdatedAt: string | null = null;
  updatedText = 'Precios pendientes de actualización';

  private sidebarSub?: Subscription;
  private statusSub?: Subscription;
  private clockSub?: Subscription;

  constructor(
    private sidebarService: SidebarService,
    private router: Router,
    private authSessionService: AuthSessionService,
    private marketPriceStatusService: MarketPriceStatusService
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
  }

  ngOnDestroy(): void {
    this.sidebarSub?.unsubscribe();
    this.statusSub?.unsubscribe();
    this.clockSub?.unsubscribe();
  }

  toggleSidebar() {
    this.sidebarService.toggle();
  }

  logout() {
    this.authSessionService.logout();
  }

  private getUpdatedAgoText(updatedAt?: string | null): string {
    if (!updatedAt) return 'Precios pendientes de actualización';

    const updatedDate = new Date(updatedAt);
    const diffSeconds = Math.max(0, Math.floor((new Date().getTime() - updatedDate.getTime()) / 1000));

    if (diffSeconds < 10) return 'Precios actualizados hace unos segundos';
    if (diffSeconds < 60) return `Precios actualizados hace ${diffSeconds} segundos`;

    const diffMinutes = Math.floor(diffSeconds / 60);
    if (diffMinutes === 1) return 'Precios actualizados hace 1 minuto';
    if (diffMinutes < 60) return `Precios actualizados hace ${diffMinutes} minutos`;

    const diffHours = Math.floor(diffMinutes / 60);
    if (diffHours === 1) return 'Precios actualizados hace 1 hora';

    return `Precios actualizados hace ${diffHours} horas`;
  }
}