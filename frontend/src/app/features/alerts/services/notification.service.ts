import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import { UnreadCountResponse } from '../models/notifications.model';

@Injectable({
    providedIn: 'root'
})
export class NotificationService {

    constructor(
        private http: HttpClient
    ) { }

    search(filter: any) {
        return this.http.post<ApiResponse<any>>(
            `${environment.apiUrl}/notification/search`,
            filter
        );
    }

    delete(id: number) {
        return this.http.delete(
            `${environment.apiUrl}/notification/${id}`
        );
    }

    markAsRead(id: number) {
        return this.http.patch(
            `${environment.apiUrl}/notification/${id}/read`,
            {}
        );
    }

    markAllAsRead() {
        return this.http.patch(
            `${environment.apiUrl}/notification/read-all`,
            {}
        );
    }

    getUnreadCount() {
        return this.http.get<ApiResponse<UnreadCountResponse>>(
            `${environment.apiUrl}/notification/unread-count`
        );
    }
}