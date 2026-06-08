import { Component, OnDestroy } from '@angular/core';
import { SidebarService } from '../../../core/services/sidebar.service';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MaterialModule } from '../../material.module';
import { AuthSessionService } from '../../../core/services/auth-session.service';
import { Subscription, timer } from 'rxjs';
import { MarketPriceStatusService } from '../../../shared/components/services/market-price-status.service';
import { MarketStatusService } from '../../../shared/components/services/market-status.service';
import { MarketStatus } from '../../../features/market/models/market.model';
import { UnreadNotificationsService } from '../../../features/alerts/services/unread-notifications.service';

/**
 * Barra lateral de navegación principal de la aplicación.
 *
 * Muestra los accesos a las distintas secciones (dashboard, mercado, portfolio,
 * alertas, watchlist, configuración y cierre de sesión), puede colapsarse para
 * ocupar menos espacio, y exhibe un indicador en tiempo real de hace cuánto se
 * actualizaron por última vez los precios del mercado.
 */
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

  // ── Estado de la barra lateral ──
  isCollapsed = false;

  // ── Estado de actualización de precios ──
  pricesUpdatedAt: string | null = null;
  updatedText = 'Precios pendientes de actualización';

  // ── Estado global del mercado ──
  marketStatus: MarketStatus | null = null;

  // ── Notificaciones sin leer ──
  unreadNotificationsCount = 0;

  // ── Suscripciones ──
  private sidebarSub?: Subscription;
  private statusSub?: Subscription;
  private clockSub?: Subscription;
  private marketStatusSub?: Subscription;
  private unreadNotificationsSub?: Subscription;

  /**
   * Suscribe la barra lateral al estado de colapso, al estado de actualización
   * de precios del mercado, al estado global del mercado (abierto/cerrado), y
   * arranca un reloj que recalcula cada segundo el texto de "hace cuánto se
   * actualizaron los precios".
   * @param sidebarService Servicio que expone y controla el estado de colapso de la barra lateral.
   * @param authSessionService Servicio de sesión, usado para cerrar sesión.
   * @param marketPriceStatusService Servicio que informa la fecha de la última actualización de precios.
   * @param marketStatusService Servicio compartido que informa el estado global del mercado (abierto/cerrado).
   */
  constructor(
    private sidebarService: SidebarService,
    private authSessionService: AuthSessionService,
    private marketPriceStatusService: MarketPriceStatusService,
    public marketStatusService: MarketStatusService,
    private unreadNotificationsService: UnreadNotificationsService
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

    this.unreadNotificationsSub = this.unreadNotificationsService.watchCount().subscribe(count => {
      this.unreadNotificationsCount = count;
    });
  }

  // ── Ciclo de vida ──

  /** Cancela las suscripciones activas (colapso, estado de precios, estado de mercado y reloj) al destruir el componente. */
  ngOnDestroy(): void {
    this.sidebarSub?.unsubscribe();
    this.statusSub?.unsubscribe();
    this.marketStatusSub?.unsubscribe();
    this.unreadNotificationsSub?.unsubscribe();
    this.clockSub?.unsubscribe();
  }

  /** @returns El texto combinado (estado del mercado + actualización de precios) a mostrar como tooltip cuando la barra está colapsada. */
  getStatusTooltip(): string {
    return `${this.marketStatusService.getStatusText(this.marketStatus)} · ${this.updatedText}`;
  }

  /** @returns La hora de la última actualización de precios formateada como reloj (p. ej. "17:00 hs"), o un texto de respaldo si todavía no hay datos. */
  getLastUpdateTimeText(): string {
    if (!this.pricesUpdatedAt) return 'Sin datos disponibles';

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

  /**
   * Construye el texto descriptivo de "hace cuánto" se actualizaron los precios
   * del mercado, en español y con la unidad de tiempo más adecuada (segundos,
   * minutos u horas) según el tiempo transcurrido desde `updatedAt`.
   * @param updatedAt Fecha (ISO) de la última actualización de precios, o `null`/`undefined` si todavía no hay datos.
   * @returns El texto a mostrar junto al ícono de estado de precios en la barra lateral.
   */
  private getUpdatedAgoText(updatedAt?: string | null): string {
    if (!updatedAt) return 'Precios pendientes de actualización';

    const updatedDate = new Date(updatedAt);
    const diffSeconds = Math.max(0, Math.floor((new Date().getTime() - updatedDate.getTime()) / 1000));

    if (diffSeconds < 10) return 'Precios actualizados hace unos segundos';
    if (diffSeconds < 60) return `Precios actualizados hace ${diffSeconds} segundos`;

    const diffMinutes = Math.floor(diffSeconds / 60);
    const remainingSeconds = diffSeconds % 60;
    const minutesText = diffMinutes === 1 ? '1 minuto' : `${diffMinutes} minutos`;
    const secondsText = remainingSeconds === 1 ? '1 segundo' : `${remainingSeconds} segundos`;
    if (diffMinutes < 60) return `Precios actualizados hace ${minutesText} y ${secondsText}`;

    const diffHours = Math.floor(diffMinutes / 60);
    if (diffHours === 1) return 'Precios actualizados hace 1 hora';

    return `Precios actualizados hace ${diffHours} horas`;
  }
}
