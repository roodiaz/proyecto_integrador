import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { Subscription, finalize } from 'rxjs';
import { MaterialModule } from '../../material.module';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { environment } from '../../../../environments/environment';
import { UnreadNotificationsService } from '../../../features/alerts/services/unread-notifications.service';
import { NotificationService } from '../../../features/alerts/services/notification.service';
import { Notification } from '../../../features/alerts/models/notifications.model';
import { UserSessionService } from '../../../core/services/user-session.service';
import { AuthSessionService } from '../../../core/services/auth-session.service';
import { ProfileData } from '../../../features/settings/models/user-profile.model';
import { ViewportService } from '../../../core/services/viewport.service';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule, MaterialModule, MatProgressSpinnerModule, TranslateModule],
  templateUrl: './header.html',
  styleUrl: './header.css'
})
export class Header implements OnInit, OnDestroy {

  // ── Notificaciones ─────────────────────────────────────────────────────────
  /** Cantidad de notificaciones sin leer, sincronizada con el servicio compartido. */
  unreadCount = 0;
  /** Últimas notificaciones mostradas en el desplegable de la campana. */
  recentNotifications: Notification[] = [];
  /** Indica si las últimas notificaciones todavía se están cargando. */
  loadingRecentNotifications = false;

  // ── Usuario ────────────────────────────────────────────────────────────────
  /** Datos básicos del usuario autenticado, mostrados en el menú del avatar. */
  profile: ProfileData | null = null;
  /** URL absoluta de la imagen de perfil, o cadena vacía si el usuario no tiene una. */
  profileImageUrl = '';

  // ── Suscripciones ──────────────────────────────────────────────────────────
  private unreadSub?: Subscription;
  private profileSub?: Subscription;

  /**
   * @param unreadNotificationsService Servicio compartido que informa la cantidad de notificaciones sin leer.
   * @param notificationService Servicio de notificaciones, usado para obtener las últimas para el desplegable.
   * @param userSessionService Servicio compartido que informa los datos básicos del usuario autenticado.
   * @param authSessionService Servicio de sesión, usado para cerrar sesión (misma lógica que el Sidebar).
   * @param router Router de Angular, usado para navegar al historial de notificaciones.
   */
  constructor(
    private unreadNotificationsService: UnreadNotificationsService,
    private notificationService: NotificationService,
    private userSessionService: UserSessionService,
    private authSessionService: AuthSessionService,
    private router: Router,
    public viewportService: ViewportService
  ) {}

  ngOnInit(): void {
    this.unreadSub = this.unreadNotificationsService.watchCount().subscribe(count => {
      this.unreadCount = count;
    });

    this.profileSub = this.userSessionService.watchProfile().subscribe(profile => {
      this.profile = profile;
      this.profileImageUrl = profile?.profileImageUrl ? environment.serverUrl + profile.profileImageUrl : '';
    });
  }

  ngOnDestroy(): void {
    this.unreadSub?.unsubscribe();
    this.profileSub?.unsubscribe();
    document.body.classList.remove('app-menu-backdrop');
  }

  // ── Backdrop de los desplegables (notificaciones / usuario) ───────────────

  /**
   * mat-menu no expone un backdrop oscuro/difuminado como MatDialog — esta
   * clase en el `<body>` activa ese efecto vía CSS global (sección
   * "Backdrop de mat-menu" en styles.css) mientras cualquiera de los dos
   * desplegables del header esté abierto.
   */
  onMenuOpened(): void {
    document.body.classList.add('app-menu-backdrop');
  }

  /** Quita el backdrop al cerrar el desplegable. */
  onMenuClosed(): void {
    document.body.classList.remove('app-menu-backdrop');
  }

  // ── Notificaciones ─────────────────────────────────────────────────────────

  /**
   * Carga las últimas 5 notificaciones para mostrarlas en el desplegable de
   * la campana. Se invoca al abrir el menú para no consultar al backend
   * innecesariamente mientras está cerrado.
   */
  loadRecentNotifications(): void {
    this.loadingRecentNotifications = true;

    this.notificationService.search({ page: 1, pageSize: 5, search: null, isRead: null, fromDate: null, toDate: null })
      .pipe(finalize(() => this.loadingRecentNotifications = false))
      .subscribe({
        next: response => this.recentNotifications = response.data?.data ?? [],
        error: () => this.recentNotifications = []
      });
  }

  /** Navega al historial de notificaciones dentro del módulo de Alertas. */
  goToNotifications(): void {
    this.router.navigate(['/alerts'], { queryParams: { view: 'history' } });
  }

  // ── Usuario ────────────────────────────────────────────────────────────────

  /** @returns La inicial del nombre del usuario en mayúscula, usada como avatar cuando no tiene imagen de perfil. */
  get userInitial(): string {
    return this.profile?.username?.trim().charAt(0).toUpperCase() || '?';
  }

  /** Cierra la sesión del usuario actual, reutilizando la misma lógica que el botón "Cerrar Sesión" del Sidebar. */
  logout(): void {
    this.authSessionService.logout();
  }
}
