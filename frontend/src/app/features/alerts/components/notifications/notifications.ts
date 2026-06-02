import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationHistory, mockNotificationHistory } from '../../models/notifications.model';
import { MatDialog } from '@angular/material/dialog';
import { FormsModule } from '@angular/forms';

@Component({
    selector: 'app-notifications',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './notifications.html',
    styleUrl: './notifications.css'
})
export class Notifications {

    constructor(
        private dialog: MatDialog
    ) { }

    notificationHistory: NotificationHistory[] = [];

    notificationsPerPage = 8;
    currentNotificationPage = 1;
    totalNotificationPages = 1;
    paginatedNotifications: NotificationHistory[] = [];

    notificationSearch = '';
    notificationStatusFilter = '';
    notificationDateFilter = '';

    showNotificationFilters = false;

    ngOnInit(): void {
        this.loadAlertHistory();
    }

    getUnreadHistoryCount(): number {
        return this.notificationHistory.filter(history => !history.isRead).length;
    }

    loadAlertHistory() {
        this.notificationHistory = mockNotificationHistory.map(history => ({
            ...history,
            priceChange: history.priceChange || 0,
            triggered: history.triggered || false
        }));
        this.updateNotificationPagination();
    }

    updateNotificationPagination() {
        this.totalNotificationPages = Math.ceil(this.notificationHistory.length / this.notificationsPerPage);
        const startIndex = (this.currentNotificationPage - 1) * this.notificationsPerPage;
        const endIndex = startIndex + this.notificationsPerPage;
        this.paginatedNotifications = this.notificationHistory.slice(startIndex, endIndex);
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
        return this.notificationHistory.filter(history => history.triggered && !history.isRead).length;
    }

    async markAllAsRead() {
        this.notificationHistory.forEach(history => {
            if (!history.isRead) {
                history.isRead = true;
                history.readAt = new Date();
            }
        });
    }

    async markAsRead(history: NotificationHistory) {
        if (!history.isRead) {
            history.isRead = true;
            history.readAt = new Date();
        }
    }

    async deleteHistory(history: NotificationHistory) {
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
            const index = this.notificationHistory.findIndex(h => h.id === history.id);
            if (index > -1) {
                this.notificationHistory.splice(index, 1);
            }
        }
    }

    applyNotificationFilter(): void {
        this.paginatedNotifications = this.notificationHistory.filter(history => {
            const matchesSearch = !this.notificationSearch ||
                history.message.toLowerCase().includes(this.notificationSearch.toLowerCase());
        });
    }

    clearNotificationFilters(): void {
        this.notificationSearch = '';
        this.notificationStatusFilter = '';
        this.notificationDateFilter = '';
        this.applyNotificationFilter();
    }
}