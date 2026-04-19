import { Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Alert, AlertHistory, ALERT_CONDITIONS } from '../../models/alert.model';
import { mockAlerts, mockAlertHistory } from '../../models/alert.model';

// Import the component class without importing the type
const ConfirmDialogComponent = () => import('../../../../shared/confirm-dialog/confirm-dialog.component')
  .then(m => m.ConfirmDialogComponent);

@Component({
  selector: 'app-alerts',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DatePipe
  ],
  templateUrl: './alerts.html',
  styleUrls: ['./alerts.css']
})
export class Alerts implements OnInit {
  activeView: 'alerts' | 'history' = 'alerts';
  alerts: Alert[] = [];
  filteredAlerts: Alert[] = [];
  alertHistory: AlertHistory[] = [];
  conditions = ALERT_CONDITIONS;
  searchTerm = '';
  statusFilter = '';
  isEditing = false;
  currentAlertId: string | null = null;

  // Pagination
  alertsPerPage = 8;
  currentPage = 1;
  totalPages = 1;
  paginatedAlerts: Alert[] = [];

  // Notifications Pagination
  notificationsPerPage = 8;
  currentNotificationPage = 1;
  totalNotificationPages = 1;
  paginatedNotifications: AlertHistory[] = [];

  // Alert management
  usedAlerts = 0;

  constructor(
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) { }

  ngOnInit() {
    this.loadAlerts();
    this.loadAlertHistory();
    this.updateUsedAlerts();
  }

  setActiveView(view: 'alerts' | 'history') {
    this.activeView = view;
  }

  getActiveAlertsCount(): number {
    return this.alerts.filter(alert => alert.isActive).length;
  }

  getUnreadHistoryCount(): number {
    return this.alertHistory.filter(history => !history.isRead).length;
  }

  getTriggeredAlertsCount(): number {
    return this.alertHistory.filter(history => history.triggered && !history.isRead).length;
  }

  getTodayTriggeredCount(): number {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return this.alertHistory.filter(history => {
      const historyDate = new Date(history.timestamp);
      historyDate.setHours(0, 0, 0, 0);
      return history.triggered && historyDate.getTime() === today.getTime();
    }).length;
  }

  getPausedAlertsCount(): number {
    return this.alerts.filter(alert => !alert.isActive).length;
  }

  onToggleSwitch(alert: Alert, event: Event) {
    const target = event.target as HTMLInputElement;
    if (target) {
      this.toggleAlert(alert, target.checked);
    }
  }

  applyFilter() {
    this.filteredAlerts = this.alerts.filter(alert => {
      const matchesSearch = !this.searchTerm || 
        alert.symbol.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        this.getConditionDisplay(alert.condition).toLowerCase().includes(this.searchTerm.toLowerCase());
      
      const matchesStatus = !this.statusFilter || 
        (this.statusFilter === 'active' && alert.isActive) ||
        (this.statusFilter === 'paused' && !alert.isActive);
      
      return matchesSearch && matchesStatus;
    });
    
    // Reset pagination when filter changes
    this.currentPage = 1;
    this.updatePagination();
  }

  updatePagination() {
    this.totalPages = Math.ceil(this.filteredAlerts.length / this.alertsPerPage);
    const startIndex = (this.currentPage - 1) * this.alertsPerPage;
    const endIndex = startIndex + this.alertsPerPage;
    this.paginatedAlerts = this.filteredAlerts.slice(startIndex, endIndex);
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.updatePagination();
    }
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
    this.applyFilter();
  }

  loadAlerts() {
    this.alerts = [...mockAlerts];
    this.filteredAlerts = [...this.alerts];
    this.updatePagination();
    this.updateUsedAlerts();
    console.log('Alerts loaded:', this.alerts.length);
    console.log('Filtered alerts:', this.filteredAlerts.length);
  }

  loadAlertHistory() {
    this.alertHistory = mockAlertHistory.map(history => ({
      ...history,
      priceChange: history.priceChange || 0,
      triggered: history.triggered || false
    }));
    this.updateNotificationPagination();
  }

  updateNotificationPagination() {
    this.totalNotificationPages = Math.ceil(this.alertHistory.length / this.notificationsPerPage);
    const startIndex = (this.currentNotificationPage - 1) * this.notificationsPerPage;
    const endIndex = startIndex + this.notificationsPerPage;
    this.paginatedNotifications = this.alertHistory.slice(startIndex, endIndex);
  }

  goToNotificationPage(page: number) {
    if (page >= 1 && page <= this.totalNotificationPages) {
      this.currentNotificationPage = page;
      this.updateNotificationPagination();
    }
  }

  nextNotificationPage() {
    this.goToNotificationPage(this.currentNotificationPage + 1);
  }

  previousNotificationPage() {
    this.goToNotificationPage(this.currentNotificationPage - 1);
  }

  getNotificationPageNumbers(): number[] {
    const pages: number[] = [];
    const maxVisiblePages = 5;
    
    if (this.totalNotificationPages <= maxVisiblePages) {
      for (let i = 1; i <= this.totalNotificationPages; i++) {
        pages.push(i);
      }
    } else {
      const startPage = Math.max(1, this.currentNotificationPage - 2);
      const endPage = Math.min(this.totalNotificationPages, startPage + maxVisiblePages - 1);
      
      for (let i = startPage; i <= endPage; i++) {
        pages.push(i);
      }
    }
    
    return pages;
  }

  updateUsedAlerts() {
    this.usedAlerts = this.alerts.length;
  }

  async markAllAsRead() {
    this.alertHistory.forEach(history => {
      if (!history.isRead) {
        history.isRead = true;
        history.readAt = new Date();
      }
    });
  }

  async markAsRead(history: AlertHistory) {
    if (!history.isRead) {
      history.isRead = true;
      history.readAt = new Date();
    }
  }

  async deleteHistory(history: AlertHistory) {
    const ConfirmDialog = await import('../../../../shared/confirm-dialog/confirm-dialog.component');
    const dialogRef = this.dialog.open(ConfirmDialog.ConfirmDialogComponent, {
      width: '350px',
      data: {
        title: 'Eliminar alerta',
        message: '¿Estás seguro de que deseas eliminar este registro del historial?'
      }
    });

    const result = await dialogRef.afterClosed().toPromise();
    if (result) {
      const index = this.alertHistory.findIndex(h => h.id === history.id);
      if (index > -1) {
        this.alertHistory.splice(index, 1);
      }
    }
  }

  async createNewAlert() {
    try {
      // Abrir modal para crear nueva alerta
      this.isEditing = false;
      this.currentAlertId = null;
      
      // Importar dinámicamente el componente de crear alerta
      const module = await import('../create-alert/create-alert');
      const ModalComponent = module.CreateAlertComponent;
      
      const dialogRef = this.dialog.open(ModalComponent, {
        width: '600px',
        data: {
          isEditing: false,
          alert: null
        }
      });
      
      const result = await dialogRef.afterClosed().toPromise();
      if (result) {
        // Si el modal retorna una nueva alerta, agregarla a la lista
        this.alerts.push(result);
        this.applyFilter();
        this.updateUsedAlerts();
        this.snackBar.open('Alerta creada exitosamente', 'Cerrar', { duration: 3000 });
      }
    } catch (error) {
      console.error('Error al abrir el modal de crear alerta:', error);
      this.snackBar.open('No se pudo abrir el modal de crear alerta', 'Cerrar', { duration: 3000 });
    }
  }

  async editAlert(alert: Alert) {
    // Para implementar el modal de editar alerta
    this.snackBar.open('Función de editar alerta en desarrollo', 'Cerrar', { duration: 3000 });
  }

  async deleteAlert(alert: Alert) {
    const ConfirmDialog = await import('../../../../shared/confirm-dialog/confirm-dialog.component');
    
    const dialogRef = this.dialog.open(ConfirmDialog.ConfirmDialogComponent, {
      width: '350px',
      data: {
        title: 'Eliminar alerta',
        message: `¿Estás seguro de que deseas eliminar la alerta para ${alert.symbol}?`
      }
    });

    const result = await dialogRef.afterClosed().toPromise();
    if (result) {
      this.alerts = this.alerts.filter(a => a.id !== alert.id);
      this.filteredAlerts = this.filteredAlerts.filter(a => a.id !== alert.id);
      this.updateUsedAlerts();
      this.snackBar.open('Alerta eliminada correctamente', 'Cerrar', { duration: 3000 });
    }
  }

  async toggleAlert(alert: Alert, isActive: boolean) {
    const alertToUpdate = this.alerts.find(a => a.id === alert.id);
    if (alertToUpdate) {
      alertToUpdate.isActive = isActive;
      alertToUpdate.updatedAt = new Date();
      this.filteredAlerts = [...this.alerts];
    }
  }

  getConditionDisplay(condition: string): string {
    const cond = this.conditions.find(c => c.value === condition);
    return cond ? cond.label : condition;
  }

  getTargetDisplay(alert: Alert): string {
    if (alert.condition === '%>' || alert.condition === '%<') {
      return `${alert.percentChange}%`;
    } else {
      return `$${alert.price?.toFixed(2)}`;
    }
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

  

}
