import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Notification } from '../../models/notifications.model';
import { MatDialog } from '@angular/material/dialog';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { NotificationService } from '../../services/notification.service';
import { MaterialModule } from '../../../../shared/material.module';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, FormsModule, MaterialModule],
  templateUrl: './notifications.html',
  styleUrl: './notifications.css'
})
export class Notifications implements OnInit {

  notificationList: Notification[] = [];
  notificationsPerPage = 8;
  currentNotificationPage = 1;
  totalNotificationPages = 1;
  notificationSearch = '';
  notificationStatusFilter = '';
  notificationDateFilter = '';
  totalNotifications = 0;
  showNotificationFilters = false;

  loadingNotifications = false;

  constructor(
    private dialog: MatDialog,
    private notificationService: NotificationService
  ) { }

  ngOnInit(): void {
    this.loadNotificationList();
  }

  getUnreadHistoryCount(): number {
    return this.notificationList.filter(list => !list.isRead).length;
  }

  loadNotificationList(): void {
    this.loadingNotifications = true;

    const filter = {
      page: this.currentNotificationPage,
      pageSize: this.notificationsPerPage,
      search: this.notificationSearch,
      isRead: this.notificationStatusFilter === 'read' ? true : this.notificationStatusFilter === 'unread' ? false : null
    };

    this.notificationService.search(filter)
      .pipe(finalize(() => this.loadingNotifications = false))
      .subscribe({
        next: response => {
          this.notificationList = response.data.data;
          this.totalNotifications = response.data.total;
          this.totalNotificationPages = Math.max(1, Math.ceil(response.data.total / this.notificationsPerPage));
        },
        error: error => {
          console.error('Error cargando notificaciones', error);
          this.notificationList = [];
          this.totalNotifications = 0;
          this.totalNotificationPages = 1;
        }
      });
  }

  goToNotificationPage(page: number): void {
    if (page < 1 || page > this.totalNotificationPages) return;

    this.currentNotificationPage = page;
    this.loadNotificationList();
  }

  nextNotificationPage(): void {
    this.goToNotificationPage(this.currentNotificationPage + 1);
  }

  previousNotificationPage(): void {
    this.goToNotificationPage(this.currentNotificationPage - 1);
  }

  getNotificationPageNumbers(): number[] {
    const pages: number[] = [];
    const maxVisiblePages = 5;

    if (this.totalNotificationPages <= maxVisiblePages) {
      for (let i = 1; i <= this.totalNotificationPages; i++) pages.push(i);
      return pages;
    }

    const startPage = Math.max(1, this.currentNotificationPage - 2);
    const endPage = Math.min(this.totalNotificationPages, startPage + maxVisiblePages - 1);

    for (let i = startPage; i <= endPage; i++) pages.push(i);

    return pages;
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead()
      .subscribe({
        next: () => {
          this.notificationList.forEach(x => x.isRead = true);
        },
        error: error => {
          console.error('Error marcando todas las notificaciones', error);
        }
      });
  }

  markAsRead(history: Notification): void {
    if (history.isRead) return;

    this.notificationService.markAsRead(history.id)
      .subscribe({
        next: () => {
          history.isRead = true;
        },
        error: error => {
          console.error('Error marcando notificación', error);
        }
      });
  }

  async deleteNotification(history: Notification): Promise<void> {
    const ConfirmDialog = await import('../../../../shared/confirm-dialog/confirm-dialog.component');

    const dialogRef = this.dialog.open(ConfirmDialog.ConfirmDialogComponent, {
      width: '350px',
      backdropClass: 'blur-backdrop',
      data: {
        title: 'Eliminar notificación',
        message: '¿Estás seguro de que deseas eliminar este registro del historial?'
      }
    });

    const result = await dialogRef.afterClosed().toPromise();

    if (!result) return;

    this.notificationService.delete(history.id)
      .subscribe({
        next: () => {
          this.loadNotificationList();
        },
        error: error => {
          console.error('Error eliminando notificación', error);
        }
      });
  }

  applyNotificationFilter(): void {
    this.currentNotificationPage = 1;
    this.loadNotificationList();
  }

  clearNotificationFilters(): void {
    this.notificationSearch = '';
    this.notificationStatusFilter = '';
    this.notificationDateFilter = '';
    this.applyNotificationFilter();
  }
}