import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize, Subscription } from 'rxjs';
import { Alert, AlertDto, AlertFilterDto, ALERT_CONDITIONS } from '../../models/alert.model';
import { Notifications } from '../notifications/notifications';
import { UnreadNotificationsService } from '../../services/unread-notifications.service';
import { AlertService } from '../../services/alert.service';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { ActivatedRoute, Router } from '@angular/router';
import { MaterialModule } from '../../../../shared/material.module';
import { InfoTooltipComponent } from '../../../../shared/components/info-tooltip/info-tooltip.component';

@Component({
  selector: 'app-alerts',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DatePipe,
    Notifications,
    MaterialModule,
    InfoTooltipComponent
  ],
  templateUrl: './alerts.html',
  styleUrls: ['./alerts.css']
})
export class Alerts implements OnInit, OnDestroy {

  // ── Vista ──────────────────────────────────────────────────────────────────
  /** Tab activo: lista de alertas o historial de notificaciones. */
  activeView: 'alerts' | 'history' = 'alerts';

  // ── Alertas ────────────────────────────────────────────────────────────────
  /** Lista de alertas cargadas según los filtros actuales. */
  alerts: Alert[] = [];
  /** Total de alertas que coinciden con el filtro (sin paginar). */
  totalAlerts = 0;
  /** Catálogo de condiciones disponibles para mostrar etiquetas legibles. */
  conditions = ALERT_CONDITIONS;

  // ── Filtros ────────────────────────────────────────────────────────────────
  searchTerm = '';
  statusFilter = '';
  createdFrom = '';
  createdTo = '';
  /** Controla la visibilidad del panel de filtros avanzados. */
  showAlertFilters = false;

  // ── Estadísticas del panel superior ───────────────────────────────────────
  usedAlerts = 0;
  activeAlerts = 0;
  pausedAlerts = 0;
  triggeredToday = 0;
  limitAlerts = 10;
  /** Cantidad de notificaciones no leídas para el badge del tab, sincronizada con el servicio compartido. */
  unreadNotificationsCount = 0;

  // ── Estados de carga ───────────────────────────────────────────────────────
  loadingAlerts = false;
  loadingStats = false;

  // ── Paginación ─────────────────────────────────────────────────────────────
  alertsPerPage = 10;
  currentPage = 1;

  /** Suscripción al contador compartido de notificaciones no leídas. */
  private unreadNotificationsSub?: Subscription;

  constructor(
    private dialog: MatDialog,
    private unreadNotificationsService: UnreadNotificationsService,
    private alertService: AlertService,
    private snackBarService: SnackBarService,
    private route: ActivatedRoute,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.loadAlerts();
    this.loadStats();

    this.unreadNotificationsSub = this.unreadNotificationsService.watchCount().subscribe(count => {
      this.unreadNotificationsCount = count;
    });

    // Abre el modal de nueva alerta si viene con un ticker por query param,
    // o el historial de notificaciones si viene con view=history (p. ej. desde la campana del Header)
    this.route.queryParams.subscribe(params => {
      const ticker = params['ticker'];
      const view = params['view'];

      if (view === 'history') this.setActiveView('history');
      if (!ticker) return;

      this.createNewAlert(ticker);

      this.router.navigate([], {
        relativeTo: this.route,
        queryParams: {},
        replaceUrl: true
      });
    });
  }

  ngOnDestroy(): void {
    this.unreadNotificationsSub?.unsubscribe();
  }

  // ── Navegación ─────────────────────────────────────────────────────────────

  /**
   * Cambia el tab activo entre la lista de alertas y el historial de notificaciones.
   * Al cambiar a historial, refresca el contador compartido de notificaciones no
   * leídas para mantener el badge actualizado.
   * @param view Tab de destino.
   */
  setActiveView(view: 'alerts' | 'history'): void {
    this.activeView = view;
    if (view === 'history') this.unreadNotificationsService.refresh();
  }

  // ── Filtros y paginación ───────────────────────────────────────────────────

  /**
   * Aplica los filtros actuales reiniciando la paginación a la primera página.
   * Se invoca cada vez que cambia cualquier campo del formulario de filtros.
   */
  applyFilter(): void {
    this.currentPage = 1;
    this.loadAlerts();
  }

  /**
   * Limpia todos los campos de filtro y recarga la lista desde el inicio.
   */
  clearSearch(): void {
    this.searchTerm = '';
    this.statusFilter = '';
    this.createdFrom = '';
    this.createdTo = '';
    this.applyFilter();
  }

  /**
   * Navega a la página indicada si el número es válido dentro del rango disponible.
   * @param page Número de página destino (basado en 1).
   */
  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadAlerts();
  }

  /** Avanza a la página siguiente. */
  nextPage(): void {
    this.goToPage(this.currentPage + 1);
  }

  /** Retrocede a la página anterior. */
  previousPage(): void {
    this.goToPage(this.currentPage - 1);
  }

  /**
   * Calcula el rango de números de página visibles en el paginador.
   * Muestra hasta 5 páginas centradas alrededor de la página actual.
   * @returns Arreglo de números de página a renderizar.
   */
  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxVisiblePages = 5;

    if (this.totalPages <= maxVisiblePages) {
      for (let i = 1; i <= this.totalPages; i++) pages.push(i);
      return pages;
    }

    const startPage = Math.max(1, this.currentPage - 2);
    const endPage = Math.min(this.totalPages, startPage + maxVisiblePages - 1);

    for (let i = startPage; i <= endPage; i++) pages.push(i);

    return pages;
  }

  /** Total de páginas calculado a partir del total de alertas y el tamaño de página. */
  get totalPages(): number {
    return Math.ceil(this.totalAlerts / this.alertsPerPage);
  }

  // ── Carga de datos ─────────────────────────────────────────────────────────

  /**
   * Carga la lista de alertas del usuario aplicando los filtros y la paginación actuales.
   * Mapea los DTOs del backend al modelo de dominio `Alert`.
   */
  loadAlerts(): void {
    this.loadingAlerts = true;

    const filter: AlertFilterDto = {
      search: this.searchTerm || undefined,
      isActive: this.statusFilter === '' ? undefined : this.statusFilter === 'active',
      createdFrom: this.createdFrom || undefined,
      createdTo: this.createdTo || undefined,
      page: this.currentPage,
      pageSize: this.alertsPerPage
    };

    this.alertService.search(filter)
      .pipe(finalize(() => this.loadingAlerts = false))
      .subscribe({
        next: response => {
          if (!response.success || !response.data) {
            this.alerts = [];
            this.totalAlerts = 0;
            this.usedAlerts = 0;
            return;
          }

          this.alerts = response.data.data.map((a: AlertDto) => ({
            id: a.id,
            symbol: a.symbol,
            condition: this.mapCondition(a),
            price: a.conditionType === 1 ? a.value : undefined,
            percentChange: a.conditionType === 2 ? a.value : undefined,
            isActive: a.isActive,
            createdAt: new Date(a.createdAt),
            updatedAt: new Date(a.createdAt),
            lastTriggered: a.lastTriggered ? new Date(a.lastTriggered) : undefined,
            userId: ''
          }));

          this.totalAlerts = response.data.total;
          this.usedAlerts = this.totalAlerts;
        },
        error: error => {
          console.error('Error cargando alertas', error);
          this.alerts = [];
          this.totalAlerts = 0;
          this.usedAlerts = 0;
        }
      });
  }

  /**
   * Carga las estadísticas del panel superior: alertas activas, pausadas,
   * disparadas hoy y el límite de alertas del plan del usuario.
   */
  loadStats(): void {
    this.loadingStats = true;

    this.alertService.getStats()
      .pipe(finalize(() => this.loadingStats = false))
      .subscribe({
        next: response => {
          if (!response.success || !response.data) return;

          this.activeAlerts = response.data.active;
          this.pausedAlerts = response.data.paused;
          this.triggeredToday = response.data.triggeredToday;
          this.usedAlerts = response.data.totalUsed;
          this.limitAlerts = response.data.limitAlerts;
        },
        error: error => {
          console.error('Error cargando estadisticas de alertas', error);
        }
      });
  }

  // ── Acciones sobre alertas ─────────────────────────────────────────────────

  /**
   * Abre el modal de creación de alerta.
   * Si se proporciona un símbolo (por ejemplo al navegar desde la pantalla de mercado
   * con el query param `ticker`), lo pre-carga en el formulario.
   * @param symbol Ticker del activo a pre-seleccionar (opcional).
   */
  async createNewAlert(symbol?: string): Promise<void> {
    try {
      const module = await import('../create-alert/create-alert');
      const ModalComponent = module.CreateAlertComponent;

      const dialogRef = this.dialog.open(ModalComponent, {
        width: '600px',
        backdropClass: 'blur-backdrop',
        data: {
          isEditing: false,
          alert: symbol ? { symbol } : null
        }
      });

      const result = await dialogRef.afterClosed().toPromise();
      if (!result) return;

      this.loadAlerts();
      this.loadStats();
    } catch (error) {
      console.error('Error al abrir el modal de crear alerta:', error);
      this.snackBarService.error('No se pudo abrir el modal de alerta');
    }
  }

  /**
   * Abre el modal de edición con los datos de la alerta seleccionada.
   * Recarga la lista y las estadísticas si el usuario confirma los cambios.
   * @param alert Alerta a editar.
   */
  async editAlert(alert: Alert): Promise<void> {
    const module = await import('../create-alert/create-alert');
    const ModalComponent = module.CreateAlertComponent;

    const dialogRef = this.dialog.open(ModalComponent, {
      width: '600px',
      backdropClass: 'blur-backdrop',
      data: { alert }
    });

    const result = await dialogRef.afterClosed().toPromise();
    if (!result) return;

    this.loadAlerts();
    this.loadStats();
  }

  /**
   * Muestra un diálogo de confirmación y, si el usuario acepta, elimina la alerta.
   * @param alert Alerta a eliminar.
   */
  async deleteAlert(alert: Alert): Promise<void> {
    const ConfirmDialog = await import('../../../../shared/confirm-dialog/confirm-dialog.component');

    const dialogRef = this.dialog.open(ConfirmDialog.ConfirmDialogComponent, {
      width: '350px',
      backdropClass: 'blur-backdrop',
      data: {
        title: 'Eliminar alerta',
        message: `¿Estas seguro de que deseas eliminar la alerta para ${alert.symbol}?`
      }
    });

    const result = await dialogRef.afterClosed().toPromise();
    if (!result) return;

    this.alertService.delete(alert.id)
      .subscribe({
        next: () => {
          this.snackBarService.success('Alerta eliminada correctamente');
          this.loadAlerts();
          this.loadStats();
        },
        error: error => {
          this.snackBarService.error(error?.error?.message ?? 'Error al eliminar la alerta');
        }
      });
  }

  /**
   * Activa o pausa una alerta llamando al endpoint de toggle.
   * Si la llamada falla, revierte el estado local para mantener la UI consistente.
   * @param alert Alerta sobre la que se ejecuta la acción.
   * @param isActive Nuevo estado deseado (`true` = activar, `false` = pausar).
   */
  toggleAlert(alert: Alert, isActive: boolean): void {
    this.alertService.toggle(alert.id)
      .subscribe({
        next: () => {
          this.loadAlerts();
          this.loadStats();
          this.snackBarService.success(isActive ? 'Alerta activada' : 'Alerta pausada');
        },
        error: error => {
          alert.isActive = !isActive;
          this.snackBarService.error(error?.error?.message ?? 'Error al actualizar la alerta');
        }
      });
  }

  /**
   * Captura el evento `change` del toggle switch y delega en `toggleAlert`.
   * Necesario para extraer el valor booleano del `HTMLInputElement` nativo.
   * @param alert Alerta asociada al switch.
   * @param event Evento nativo del input checkbox.
   */
  onToggleSwitch(alert: Alert, event: Event): void {
    const target = event.target as HTMLInputElement;
    if (!target) return;
    this.toggleAlert(alert, target.checked);
  }

  // ── Helpers de presentación ────────────────────────────────────────────────

  /**
   * Devuelve la etiqueta legible para un valor de condición
   * (por ejemplo: `'>'` → `'Mayor que'`).
   * @param condition Valor interno de la condición.
   * @returns Etiqueta para mostrar en la UI; si no hay coincidencia, devuelve el valor tal cual.
   */
  getConditionDisplay(condition: string): string {
    const cond = this.conditions.find(c => c.value === condition);
    return cond ? cond.label : condition;
  }

  /**
   * Formatea el valor objetivo de la alerta según su tipo.
   * Las condiciones de variación porcentual se muestran como `X%`,
   * las de precio exacto como `$X.XX`.
   * @param alert Alerta a formatear.
   * @returns Cadena con el valor formateado para mostrar en la tabla.
   */
  getTargetDisplay(alert: Alert): string {
    if (alert.condition === '%>' || alert.condition === '%<') return `${alert.percentChange}%`;
    return `$${alert.price?.toFixed(2)}`;
  }

  // ── Métodos privados ───────────────────────────────────────────────────────

  /**
   * Convierte los campos `conditionType` y `operator` del DTO del backend
   * al tipo de condición unificado que usa el modelo `Alert` en el frontend.
   * @param alert DTO recibido del backend.
   * @returns Condición en formato interno (`'>'`, `'<'`, `'>='`, `'<='`, `'='`, `'%>'`, `'%<'`).
   */
  private mapCondition(alert: AlertDto): Alert['condition'] {
    if (alert.conditionType === 2) return alert.operator === 1 ? '%>' : '%<';

    switch (alert.operator) {
      case 1: return '>';
      case 2: return '<';
      case 3: return '>=';
      case 4: return '<=';
      case 5: return '=';
      default: return '>';
    }
  }
}
