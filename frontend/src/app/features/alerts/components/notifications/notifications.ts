import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationList } from '../../models/notifications.model';
import { MatDialog } from '@angular/material/dialog';
import { FormsModule } from '@angular/forms';
import { NotificationService } from '../../services/notification.service';

@Component({
    selector: 'app-notifications',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './notifications.html',
    styleUrl: './notifications.css'
})
export class Notifications {

    constructor(
        private dialog: MatDialog,
        private notificationService: NotificationService
    ) { }

    notificationList: NotificationList[] = [];

    notificationsPerPage = 8;
    currentNotificationPage = 1;
    totalNotificationPages = 1;
    paginatedNotifications: NotificationList[] = [];

    notificationSearch = '';
    notificationStatusFilter = '';
    notificationDateFilter = '';

    showNotificationFilters = false;

    ngOnInit(): void {
        this.loadNotificationList();
    }

    getUnreadHistoryCount(): number {
        return this.notificationList.filter(list => !list.isRead).length;
    }

    loadNotificationList() {

        const filter = {
            page: this.currentNotificationPage,
            pageSize: this.notificationsPerPage,
            search: this.notificationSearch,
            isRead:
                this.notificationStatusFilter === 'read'
                    ? true
                    : this.notificationStatusFilter === 'unread'
                        ? false
                        : null
        };

        this.notificationService.search(filter)
            .subscribe({
                next: (response) => {

                    this.notificationList = response.data.data;

                    this.totalNotificationPages = Math.ceil(
                        response.data.total / this.notificationsPerPage
                    );

                    this.paginatedNotifications = this.notificationList;
                }
            });
    }

    updateNotificationPagination() {
        this.totalNotificationPages = Math.ceil(this.notificationList.length / this.notificationsPerPage);
        const startIndex = (this.currentNotificationPage - 1) * this.notificationsPerPage;
        const endIndex = startIndex + this.notificationsPerPage;
        this.paginatedNotifications = this.notificationList.slice(startIndex, endIndex);
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

    getTriggeredAlertsCount(): number {
        return this.notificationList.filter(list => list.triggered && !list.isRead).length;
    }

    markAllAsRead() {

        this.notificationService.markAllAsRead()
            .subscribe({
                next: () => {
                    this.notificationList.forEach(x => x.isRead = true);
                },
                error: (error) => {
                    console.error('Error marcando todas las notificaciones', error);
                }
            });
    }

    markAsRead(history: NotificationList) {

        if (history.isRead)
            return;

        this.notificationService.markAsRead(history.id)
            .subscribe({
                next: () => {
                    history.isRead = true;
                },
                error: (error) => {
                    console.error('Error marcando notificación', error);
                }
            });
    }

    async deleteNotification(history: NotificationList) {
        console.log('ENTRO AL DELETE');
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

            this.notificationService.delete(history.id)
                .subscribe({
                    next: () => {
                        this.loadNotificationList();
                    },
                    error: (error) => {
                        console.error('Error eliminando notificación', error);
                    }
                });

        }
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