import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Alert, ALERT_CONDITIONS } from '../../models/alert.model';
import { Notifications } from '../notifications/notifications';
import { Notification } from '../../models/notifications.model';
import { NotificationService } from '../../services/notification.service';
import { AlertService } from '../../services/alert.service';
import { AlertFilterDto, AlertDto } from '../../models/alert.model';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { ActivatedRoute, Router } from '@angular/router';
import { MaterialModule } from '../../../../shared/material.module';

// Import the component class without importing the type
const ConfirmDialogComponent = () => import('../../../../shared/confirm-dialog/confirm-dialog.component')
  .then(m => m.ConfirmDialogComponent);

@Component({
  selector: 'app-alerts',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DatePipe,
    Notifications,
    MaterialModule
  ],
  templateUrl: './alerts.html',
  styleUrls: ['./alerts.css']
})
export class Alerts implements OnInit {
  activeView: 'alerts' | 'history' = 'alerts';
  alerts: Alert[] = [];
  totalAlerts = 0;
  notificationHistory: Notification[] = [];
  conditions = ALERT_CONDITIONS;
  searchTerm = '';
  statusFilter = '';
  createdFrom: string = '';
  createdTo: string = '';
  isEditing = false;
  currentAlertId: string | null = null;
  unreadNotificationsCount = 0;

  // Paginacion
  alertsPerPage = 10;
  currentPage = 1;

  // Alert management
  usedAlerts = 0;
  activeAlerts = 0;
  pausedAlerts = 0;
  triggeredToday = 0;
  limitAlerts = 10;

  constructor(
    private dialog: MatDialog,
    private notificationService: NotificationService,
    private alertService: AlertService,
    private snackBarService: SnackBarService,
    private route: ActivatedRoute,
    private router: Router
  ) { }

  ngOnInit() {
    this.loadAlerts();
    this.loadStats();
    this.loadUnreadNotificationsCount();

    this.route.queryParams.subscribe(params => {

      const ticker = params['ticker'];
      if (ticker) {

        this.createNewAlert(ticker);

        this.router.navigate(
          [],
          {
            relativeTo: this.route,
            queryParams: {},
            replaceUrl: true
          }
        );
      }

    });
  }

  setActiveView(view: 'alerts' | 'history') {
    this.activeView = view;

    if (view === 'history')
      this.loadUnreadNotificationsCount();
  }

  onToggleSwitch(alert: Alert, event: Event) {
    const target = event.target as HTMLInputElement;
    if (target) {
      this.toggleAlert(alert, target.checked);
    }
  }

  applyFilter() {
    this.currentPage = 1;
    this.loadAlerts();
  }

  goToPage(page: number) {

    if (page < 1 || page > this.totalPages)
      return;

    this.currentPage = page;
    this.loadAlerts();
  }

  nextPage() {
    this.goToPage(this.currentPage + 1);
  }

  previousPage() {
    this.goToPage(this.currentPage - 1);
  }

  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxVisiblePages = 5;

    if (this.totalPages <= maxVisiblePages) {
      // Show all pages if total is small
      for (let i = 1; i <= this.totalPages; i++) {
        pages.push(i);
      }
    } else {
      // Show pages around current page
      const startPage = Math.max(1, this.currentPage - 2);
      const endPage = Math.min(this.totalPages, startPage + maxVisiblePages - 1);

      for (let i = startPage; i <= endPage; i++) {
        pages.push(i);
      }
    }

    return pages;
  }

  clearSearch() {
    this.searchTerm = '';
    this.statusFilter = '';
    this.createdFrom = '';
    this.createdTo = '';
    this.applyFilter();
  }

  loadAlerts() {

    const filter: AlertFilterDto = {
      search: this.searchTerm || undefined,
      isActive:
        this.statusFilter === ''
          ? undefined
          : this.statusFilter === 'active',
      createdFrom: this.createdFrom || undefined,
      createdTo: this.createdTo || undefined,
      page: this.currentPage,
      pageSize: this.alertsPerPage
    };

    this.alertService.search(filter)
      .subscribe({
        next: (response) => {

          this.alerts = response.data!.data.map((a: AlertDto) => ({
            id: a.id,
            symbol: a.symbol,
            condition: this.mapCondition(a),
            price: a.conditionType === 1 ? a.value : undefined,
            percentChange: a.conditionType === 2 ? a.value : undefined,
            isActive: a.isActive,
            createdAt: new Date(a.createdAt),
            updatedAt: new Date(a.createdAt),
            lastTriggered: a.lastTriggered
              ? new Date(a.lastTriggered)
              : undefined,
            userId: ''
          }));

          this.totalAlerts = response.data!.total;
          this.usedAlerts = this.totalAlerts;
        },
        error: (error) => {
          console.error('Error cargando alertas', error);
        }
      });
  }

  private mapCondition(alert: AlertDto): Alert['condition'] {

    if (alert.conditionType === 2) {
      return alert.operator === 1 ? '%>' : '%<';
    }

    switch (alert.operator) {
      case 1:
        return '>';
      case 2:
        return '<';
      case 3:
        return '>=';
      case 4:
        return '<=';
      case 5:
        return '=';
      default:
        return '>';
    }
  }

  async createNewAlert(symbol?: string) {
    try {
      // Abrir modal para crear nueva alerta
      this.isEditing = false;
      this.currentAlertId = null;

      // Importar dinámicamente el componente de crear alerta
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
      if (result) {
        this.loadAlerts();
        this.loadStats();
      }

    } catch (error) {
      console.error('Error al abrir el modal de crear alerta:', error);
    }
  }

  async editAlert(alert: Alert) {

    const module = await import('../create-alert/create-alert');
    const ModalComponent = module.CreateAlertComponent;

    const dialogRef = this.dialog.open(ModalComponent, {
      width: '600px',
      backdropClass: 'blur-backdrop',
      data: {
        alert
      }
    });

    const result = await dialogRef.afterClosed().toPromise();
    if (result) {
      this.loadAlerts();
      this.loadStats();
    }
  }

  async deleteAlert(alert: Alert) {
    const ConfirmDialog = await import('../../../../shared/confirm-dialog/confirm-dialog.component');

    const dialogRef = this.dialog.open(ConfirmDialog.ConfirmDialogComponent, {
      width: '350px',
      backdropClass: 'blur-backdrop',
      data: {
        title: 'Eliminar alerta',
        message: `¿Estás seguro de que deseas eliminar la alerta para ${alert.symbol}?`
      }
    });

    const result = await dialogRef.afterClosed().toPromise();
    if (result) {

      this.alertService.delete(alert.id)
        .subscribe({
          next: () => {

            this.snackBarService.success(
              'Alerta eliminada correctamente'
            );

            this.loadAlerts();
            this.loadStats();
          },
          error: (error) => {

            this.snackBarService.error(
              error?.error?.message ??
              'Error al eliminar la alerta'
            );
          }
        });
    }
  }

  toggleAlert(alert: Alert, isActive: boolean) {

    this.alertService.toggle(alert.id)
      .subscribe({
        next: () => {

          this.loadAlerts();
          this.loadStats();

          this.snackBarService.success(
            isActive
              ? 'Alerta activada'
              : 'Alerta pausada'
          );
        },
        error: (error) => {

          alert.isActive = !isActive;

          this.snackBarService.error(
            error?.error?.message ??
            'Error al actualizar la alerta'
          );
        }
      });
  }

  get totalPages(): number {
    return Math.ceil(this.totalAlerts / this.alertsPerPage);
  }

  getConditionDisplay(condition: string): string {
    const cond = this.conditions.find(c => c.value === condition);
    return cond ? cond.label : condition;
  }

  getTargetDisplay(alert: Alert): string {
    if (alert.condition === '%>' || alert.condition === '%<') {
      return `${alert.percentChange}%`;
    }

    return `$${alert.price?.toFixed(2)}`;
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'active':
        return 'status-active';
      case 'paused':
        return 'status-paused';
      case 'triggered':
        return 'status-triggered';
      default:
        return '';
    }
  }

  loadUnreadNotificationsCount() {

    this.notificationService.getUnreadCount()
      .subscribe({
        next: (response) => {
          this.unreadNotificationsCount = response.data!.count;
        },
        error: (error) => {
          console.error('Error obteniendo notificaciones no leídas', error);
        }
      });
  }

  loadStats() {

    this.alertService.getStats()
      .subscribe({
        next: (response) => {

          this.activeAlerts = response.data!.active;
          this.pausedAlerts = response.data!.paused;
          this.triggeredToday = response.data!.triggeredToday;
          this.usedAlerts = response.data!.totalUsed;
          this.limitAlerts = response.data!.limitAlerts;
        },
        error: (error) => {
          console.error('Error cargando estadísticas', error);
        }
      });
  }
}
