import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Alert, AlertHistory, ALERT_CONDITIONS, mockAlerts, mockAlertHistory } from '../../models/alert.model';
import { MaterialModule } from '../../shared/material.module';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { MatTable } from '@angular/material/table';

// Import the component class without importing the type
const ConfirmDialogComponent = () => import('../../shared/confirm-dialog/confirm-dialog.component')
  .then(m => m.ConfirmDialogComponent);

@Component({
  selector: 'app-alerts',
  standalone: true,
  imports: [
    CommonModule,
    MaterialModule,
    MatTable
    ],
  templateUrl: './alerts.html',
  styleUrls: ['./alerts.css']
})
export class Alerts implements OnInit {
  activeTabIndex = 0;
  alerts: Alert[] = [];
  filteredAlerts: Alert[] = [];
  alertHistory: AlertHistory[] = [];
  conditions = ALERT_CONDITIONS;
  displayedColumns: string[] = ['symbol', 'condition', 'target', 'status', 'actions'];
  historyColumns: string[] = ['symbol', 'message', 'price', 'timestamp', 'read'];
  searchTerm = '';
  isEditing = false;
  currentAlertId: string | null = null;

  @ViewChild(MatTable) table!: MatTable<Alert>;

  constructor(
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) { }

  ngOnInit() {
    this.loadAlerts();
    this.loadAlertHistory();
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
    this.alertHistory = [...mockAlertHistory];
  }

  onTabChange(index: number) {
    this.activeTabIndex = index;
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

  toggleAlert(alert: Alert, isActive: boolean) {
    const alertToUpdate = this.alerts.find(a => a.id === alert.id);
    if (alertToUpdate) {
      alertToUpdate.isActive = isActive;
      alertToUpdate.updatedAt = new Date();
      // Update the filtered alerts to reflect the change
      this.filteredAlerts = [...this.alerts];
      // In a real app, you would update the alert on the server here
      this.snackBar.open(`Alerta ${isActive ? 'activada' : 'desactivada'}`, 'Cerrar', {
        duration: 2000,
      });
    }
  }

  markAsRead(history: AlertHistory) {
    if (!history.isRead) {
      history.isRead = true;
      // In a real app, you would update the history on the server here
      this.snackBar.open('Mensaje marcado como leído', 'Cerrar', {
        duration: 2000,
      });
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
