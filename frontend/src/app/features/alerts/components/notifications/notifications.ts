import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Notification } from '../../models/notifications.model';
import { MatDialog } from '@angular/material/dialog';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';
import { NotificationService } from '../../services/notification.service';
import { UnreadNotificationsService } from '../../services/unread-notifications.service';
import { MaterialModule } from '../../../../shared/material.module';
import { InfoTooltipComponent } from '../../../../shared/components/info-tooltip/info-tooltip.component';
import { LanguageService } from '../../../../core/services/language.service';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, FormsModule, MaterialModule, InfoTooltipComponent, TranslateModule],
  templateUrl: './notifications.html',
  styleUrl: './notifications.css'
})
export class Notifications implements OnInit {

  // ── Notificaciones ─────────────────────────────────────────────────────────
  /** Lista de notificaciones de la página actual. */
  notificationList: Notification[] = [];
  /** Total de notificaciones que coinciden con el filtro activo (sin paginar). */
  totalNotifications = 0;

  // ── Filtros ────────────────────────────────────────────────────────────────
  notificationSearch = '';
  notificationStatusFilter = '';
  notificationFromDate = '';
  notificationToDate = '';
  /** Controla la visibilidad del panel de filtros avanzados. */
  showNotificationFilters = false;

  // ── Paginación ─────────────────────────────────────────────────────────────
  notificationsPerPage = 8;
  currentNotificationPage = 1;
  totalNotificationPages = 1;

  // ── Estado de carga ────────────────────────────────────────────────────────
  loadingNotifications = false;

  constructor(
    private dialog: MatDialog,
    private notificationService: NotificationService,
    private unreadNotificationsService: UnreadNotificationsService,
    private languageService: LanguageService
  ) { }

  ngOnInit(): void {
    this.loadNotificationList();
  }

  // ── Contadores ─────────────────────────────────────────────────────────────

  /**
   * Cuenta las notificaciones no leídas dentro de la página actual.
   * Se usa para mostrar el chip de "sin leer" y controlar la visibilidad
   * del botón "Marcar todo como leído".
   * @returns Cantidad de notificaciones no leídas en la lista visible.
   */
  getUnreadHistoryCount(): number {
    return this.notificationList.filter(n => !n.isRead).length;
  }

  // ── Carga de datos ─────────────────────────────────────────────────────────

  /**
   * Carga la lista de notificaciones del usuario aplicando los filtros
   * y la paginación actuales.
   */
  loadNotificationList(): void {
    this.loadingNotifications = true;

    const filter = {
      page:     this.currentNotificationPage,
      pageSize: this.notificationsPerPage,
      search:   this.notificationSearch   || null,
      isRead:   this.notificationStatusFilter === 'read'
                  ? true
                  : this.notificationStatusFilter === 'unread'
                    ? false
                    : null,
      fromDate: this.notificationFromDate || null,
      toDate:   this.notificationToDate   || null
    };

    this.notificationService.search(filter)
      .pipe(finalize(() => this.loadingNotifications = false))
      .subscribe({
        next: response => {
          this.notificationList      = response.data.data;
          this.totalNotifications    = response.data.total;
          this.totalNotificationPages = Math.max(1, Math.ceil(response.data.total / this.notificationsPerPage));
        },
        error: error => {
          console.error('Error cargando notificaciones', error);
          this.notificationList       = [];
          this.totalNotifications     = 0;
          this.totalNotificationPages = 1;
        }
      });
  }

  // ── Paginación ─────────────────────────────────────────────────────────────

  /**
   * Navega a la página indicada si el número es válido dentro del rango disponible.
   * @param page Número de página destino (basado en 1).
   */
  goToNotificationPage(page: number): void {
    if (page < 1 || page > this.totalNotificationPages) return;
    this.currentNotificationPage = page;
    this.loadNotificationList();
  }

  /** Avanza a la página siguiente. */
  nextNotificationPage(): void {
    this.goToNotificationPage(this.currentNotificationPage + 1);
  }

  /** Retrocede a la página anterior. */
  previousNotificationPage(): void {
    this.goToNotificationPage(this.currentNotificationPage - 1);
  }

  /**
   * Calcula el rango de números de página visibles en el paginador.
   * Muestra hasta 5 páginas centradas alrededor de la página actual.
   * @returns Arreglo de números de página a renderizar.
   */
  getNotificationPageNumbers(): number[] {
    const pages: number[] = [];
    const maxVisiblePages = 5;

    if (this.totalNotificationPages <= maxVisiblePages) {
      for (let i = 1; i <= this.totalNotificationPages; i++) pages.push(i);
      return pages;
    }

    const startPage = Math.max(1, this.currentNotificationPage - 2);
    const endPage   = Math.min(this.totalNotificationPages, startPage + maxVisiblePages - 1);

    for (let i = startPage; i <= endPage; i++) pages.push(i);

    return pages;
  }

  // ── Filtros ────────────────────────────────────────────────────────────────

  /**
   * Aplica los filtros actuales reiniciando la paginación a la primera página.
   * Se invoca cada vez que cambia cualquier campo del formulario de filtros.
   */
  applyNotificationFilter(): void {
    this.currentNotificationPage = 1;
    this.loadNotificationList();
  }

  /**
   * Limpia todos los campos de filtro y recarga la lista desde el inicio.
   */
  clearNotificationFilters(): void {
    this.notificationSearch       = '';
    this.notificationStatusFilter = '';
    this.notificationFromDate     = '';
    this.notificationToDate       = '';
    this.applyNotificationFilter();
  }

  // ── Acciones sobre notificaciones ─────────────────────────────────────────

  /**
   * Marca todas las notificaciones visibles como leídas llamando al endpoint
   * correspondiente. Actualiza el estado local para reflejar el cambio sin
   * necesidad de recargar la lista.
   */
  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe({
      next: () => {
        this.notificationList.forEach(n => n.isRead = true);
        this.unreadNotificationsService.refresh();
      },
      error: error => {
        console.error('Error marcando todas las notificaciones', error);
      }
    });
  }

  /**
   * Marca una notificación individual como leída.
   * Si ya está leída, no realiza ninguna llamada al servidor.
   * Actualiza el estado local directamente al recibir confirmación.
   * @param notification Notificación a marcar como leída.
   */
  markAsRead(notification: Notification): void {
    if (notification.isRead) return;

    this.notificationService.markAsRead(notification.id).subscribe({
      next: () => {
        notification.isRead = true;
        this.unreadNotificationsService.refresh();
      },
      error: error => {
        console.error('Error marcando notificación', error);
      }
    });
  }

  /**
   * Muestra un diálogo de confirmación y, si el usuario acepta, elimina
   * la notificación del historial. Recarga la lista al completarse.
   * @param notification Notificación a eliminar.
   */
  async deleteNotification(notification: Notification): Promise<void> {
    const ConfirmDialog = await import('../../../../shared/confirm-dialog/confirm-dialog.component');

    const dialogRef = this.dialog.open(ConfirmDialog.ConfirmDialogComponent, {
      width: '350px',
      backdropClass: 'blur-backdrop',
      data: {
        title:   this.languageService.instant('NOTIFICATIONS.DELETE_CONFIRM.TITLE'),
        message: this.languageService.instant('NOTIFICATIONS.DELETE_CONFIRM.MESSAGE')
      }
    });

    const result = await dialogRef.afterClosed().toPromise();
    if (!result) return;

    this.notificationService.delete(notification.id).subscribe({
      next: () => {
        this.loadNotificationList();
        this.unreadNotificationsService.refresh();
      },
      error: error => {
        console.error('Error eliminando notificación', error);
      }
    });
  }
}
