import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Alert, AlertHistory, ALERT_CONDITIONS, mockAlerts, mockAlertHistory } from '../../models/alert.model';
import { MaterialModule } from '../../shared/material.module';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';

// Import the component class without importing the type
const ConfirmDialogComponent = () => import('../../shared/confirm-dialog/confirm-dialog.component')
  .then(m => m.ConfirmDialogComponent);

@Component({
  selector: 'app-alerts',
  standalone: true,
  imports: [
    CommonModule,
    MaterialModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSlideToggleModule,
    MatTooltipModule
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
  displayedColumns: string[] = ['symbol', 'condition', 'target', 'status', 'actions'];
  historyColumns: string[] = ['symbol', 'message', 'price', 'timestamp', 'read'];
  searchTerm = '';
  isEditing = false;
  currentAlertId: string | null = null;


  constructor(
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) { }

  ngOnInit() {
    this.loadAlerts();
    this.loadAlertHistory();
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

  applyFilter(event: Event) {
    this.searchTerm = (event.target as HTMLInputElement).value.toLowerCase();
    this.filteredAlerts = this.alerts.filter(alert => 
      alert.symbol.toLowerCase().includes(this.searchTerm) ||
      this.getConditionDisplay(alert.condition).toLowerCase().includes(this.searchTerm) ||
      this.getTargetDisplay(alert).toLowerCase().includes(this.searchTerm)
    );
  }

  clearSearch(input: HTMLInputElement) {
    input.value = '';
    this.searchTerm = '';
    this.filteredAlerts = [...this.alerts];
  }

  loadAlerts() {
    // En una aplicación real, esto vendría de un servicio
    this.alerts = [...mockAlerts];
    this.filteredAlerts = [...this.alerts];
  }

  loadAlertHistory() {
    // En una aplicación real, esto vendría de un servicio
    this.alertHistory = mockAlertHistory.map(history => ({
      ...history,
      priceChange: history.priceChange || 0,
      triggered: history.triggered || false
    }));
  }

  private showNotification(message: string) {
    this.snackBar.open(message, 'Cerrar', {
      duration: 3000,
      horizontalPosition: 'right',
      verticalPosition: 'top',
    });
  }

  async markAllAsRead() {
    this.alertHistory.forEach(history => {
      if (!history.isRead) {
        history.isRead = true;
      }
    });
    this.showNotification('Todas las alertas marcadas como leídas');
  }

  async markAsRead(history: AlertHistory) {
    if (!history.isRead) {
      history.isRead = true;
      history.readAt = new Date();
      this.showNotification('Alerta marcada como leída');
    }
  }

  async deleteHistory(history: AlertHistory) {
    const ConfirmDialog = await import('../../shared/confirm-dialog/confirm-dialog.component');
    const dialogRef = this.dialog.open(ConfirmDialog.ConfirmDialogComponent, {
      width: '350px',
      data: {
        title: 'Eliminar Historial',
        message: '¿Estás seguro de que deseas eliminar este registro del historial?'
      }
    });

    const result = await dialogRef.afterClosed().toPromise();
    if (result) {
      const index = this.alertHistory.findIndex(h => h.id === history.id);
      if (index > -1) {
        this.alertHistory.splice(index, 1);
        this.showNotification('Registro de alerta eliminado');
      }
    }
  }

  async createNewAlert() {
    const CreateAlert = await import('../create-alert/create-alert');
    
    const dialogRef = this.dialog.open(CreateAlert.CreateAlertComponent, {
      width: '500px',
      data: { 
        conditions: this.conditions
      }
    });

    const result = await dialogRef.afterClosed().toPromise();
    if (result) {
      const newAlert: Alert = {
        ...result,
        id: Date.now().toString(),
        createdAt: new Date(),
        updatedAt: new Date(),
        userId: 'user1' // En una aplicación real, usarías el ID del usuario autenticado
      };
      this.alerts.push(newAlert);
      this.filteredAlerts = [...this.alerts];
      this.snackBar.open('Alerta creada correctamente', 'Cerrar', { duration: 3000 });
    }
  }

  async editAlert(alert: Alert) {
    const CreateAlert = await import('../create-alert/create-alert');
    
    const dialogRef = this.dialog.open(CreateAlert.CreateAlertComponent, {
      width: '500px',
      data: { 
        alert: { ...alert }, // Create a new object to avoid reference issues
        conditions: this.conditions
      }
    });

    const result = await dialogRef.afterClosed().toPromise();
    if (result) {
      const index = this.alerts.findIndex(a => a.id === alert.id);
      if (index !== -1) {
        this.alerts[index] = { 
          ...result, 
          id: alert.id,
          createdAt: this.alerts[index].createdAt, // Preserve original creation date
          updatedAt: new Date()
        };
        this.filteredAlerts = [...this.alerts];
        this.snackBar.open('Alerta actualizada correctamente', 'Cerrar', { duration: 3000 });
      }
    }
  }

  async deleteAlert(alert: Alert) {
    // Dynamically import the component
    const ConfirmDialog = await import('../../shared/confirm-dialog/confirm-dialog.component');
    
    // Open the dialog with the dynamically imported component
    const dialogRef = this.dialog.open(ConfirmDialog.ConfirmDialogComponent, {
      width: '350px',
      data: {
        title: 'Eliminar Alerta',
        message: `¿Estás seguro de que deseas eliminar la alerta para ${alert.symbol}?`
      }
    });

    const result = await dialogRef.afterClosed().toPromise();
    if (result) {
      this.alerts = this.alerts.filter(a => a.id !== alert.id);
      this.filteredAlerts = this.filteredAlerts.filter(a => a.id !== alert.id);
      this.snackBar.open('Alerta eliminada correctamente', 'Cerrar', { duration: 3000 });
    }
  }

  async toggleAlert(alert: Alert, isActive: boolean) {
    const alertToUpdate = this.alerts.find(a => a.id === alert.id);
    if (alertToUpdate) {
      alertToUpdate.isActive = isActive;
      alertToUpdate.updatedAt = new Date();
      // Update the filtered alerts to reflect the change
      this.filteredAlerts = [...this.alerts];
      this.showNotification(`Alerta ${isActive ? 'activada' : 'desactivada'}`);
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

}
