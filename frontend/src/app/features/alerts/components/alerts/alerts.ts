import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { Alert, ALERT_CONDITIONS } from '../../models/alert.model';
import { Notifications } from '../notifications/notifications';
import { Notification } from '../../models/notifications.model';
import { NotificationService } from '../../services/notification.service';
import { AlertService } from '../../services/alert.service';
import { AlertFilterDto, AlertDto } from '../../models/alert.model';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { ActivatedRoute, Router } from '@angular/router';
import { MaterialModule } from '../../../../shared/material.module';

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
  createdFrom = '';
  createdTo = '';
  isEditing = false;
  currentAlertId: string | null = null;
  unreadNotificationsCount = 0;

  loadingAlerts = false;
  loadingStats = false;
  loadingUnreadNotifications = false;

  alertsPerPage = 10;
  currentPage = 1;

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

  ngOnInit(): void {
    this.loadAlerts();
    this.loadStats();
    this.loadUnreadNotificationsCount();

    this.route.queryParams.subscribe(params => {
      const ticker = params['ticker'];

      if (!ticker) return;

      this.createNewAlert(ticker);

      this.router.navigate([], {
        relativeTo: this.route,
        queryParams: {},
        replaceUrl: true
      });
    });
  }

  setActiveView(view: 'alerts' | 'history'): void {
    this.activeView = view;

    if (view === 'history') this.loadUnreadNotificationsCount();
  }

  onToggleSwitch(alert: Alert, event: Event): void {
    const target = event.target as HTMLInputElement;
    if (!target) return;

    this.toggleAlert(alert, target.checked);
  }

  applyFilter(): void {
    this.currentPage = 1;
    this.loadAlerts();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;

    this.currentPage = page;
    this.loadAlerts();
  }

  nextPage(): void {
    this.goToPage(this.currentPage + 1);
  }

  previousPage(): void {
    this.goToPage(this.currentPage - 1);
  }

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

  clearSearch(): void {
    this.searchTerm = '';
    this.statusFilter = '';
    this.createdFrom = '';
    this.createdTo = '';
    this.applyFilter();
  }

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

  private mapCondition(alert: AlertDto): Alert['condition'] {
    if (alert.conditionType === 2) return alert.operator === 1 ? '%>' : '%<';

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

  async createNewAlert(symbol?: string): Promise<void> {
    try {
      this.isEditing = false;
      this.currentAlertId = null;

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

  async editAlert(alert: Alert): Promise<void> {
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

    if (!result) return;

    this.loadAlerts();
    this.loadStats();
  }

  async deleteAlert(alert: Alert): Promise<void> {
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

  get totalPages(): number {
    return Math.ceil(this.totalAlerts / this.alertsPerPage);
  }

  getConditionDisplay(condition: string): string {
    const cond = this.conditions.find(c => c.value === condition);
    return cond ? cond.label : condition;
  }

  getTargetDisplay(alert: Alert): string {
    if (alert.condition === '%>' || alert.condition === '%<') return `${alert.percentChange}%`;
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

  loadUnreadNotificationsCount(): void {
    this.loadingUnreadNotifications = true;

    this.notificationService.getUnreadCount()
      .pipe(finalize(() => this.loadingUnreadNotifications = false))
      .subscribe({
        next: response => {
          this.unreadNotificationsCount = response.data!.count;
        },
        error: error => {
          console.error('Error obteniendo notificaciones no leídas', error);
        }
      });
  }

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
          console.error('Error cargando estadísticas de alertas', error);
        }
      });
  }
}