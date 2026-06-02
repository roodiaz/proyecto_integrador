import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import { AlertFilterDto, AlertSearchResponseDto, AlertStatsDto, CreateAlertDto, UpdateAlertDto } from '../models/alert.model';

@Injectable({
    providedIn: 'root'
})
export class AlertService {

    constructor(
        private http: HttpClient
    ) { }

    create(alert: CreateAlertDto) {
        return this.http.post<ApiResponse<any>>(
            `${environment.apiUrl}/alert`,
            alert
        );
    }

    search(filter: AlertFilterDto) {
        return this.http.post<ApiResponse<AlertSearchResponseDto>>(
            `${environment.apiUrl}/alert/search`,
            filter
        );
    }

    delete(id: number) {
        return this.http.delete<ApiResponse<any>>(
            `${environment.apiUrl}/alert/${id}`
        );
    }

    toggle(id: number) {
        return this.http.patch<ApiResponse<any>>(
            `${environment.apiUrl}/alert/${id}/toggle`,
            {}
        );
    }

    getStats() {
        return this.http.get<ApiResponse<AlertStatsDto>>(
            `${environment.apiUrl}/alert/stats`
        );
    }

    update(dto: UpdateAlertDto) {
        return this.http.put<ApiResponse<any>>(
            `${environment.apiUrl}/alert`,
            dto
        );
    }
}