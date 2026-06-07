import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import {
    DashboardPerformanceChart,
    DashboardPerformanceChartFilter,
    DashboardTopCards,
    DashboardLatestTransaction,
    DashboardRecentNotification,
    DashboardPortfolioDistribution
} from '../models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
    private readonly apiUrl = `${environment.apiUrl}/dashboard`;

    constructor(private http: HttpClient) { }

    getTopCards():
        Observable<ApiResponse<DashboardTopCards>> {
        return this.http.get<ApiResponse<DashboardTopCards>>(
            `${this.apiUrl}/top-cards`
        );
    }

    getPerformanceChart(period: string):
        Observable<ApiResponse<DashboardPerformanceChart>> {
        const filter: DashboardPerformanceChartFilter = { period };
        return this.http.post<ApiResponse<DashboardPerformanceChart>>(
            `${this.apiUrl}/performance-chart`, filter
        );
    }

    getLatestTransactions():
        Observable<ApiResponse<DashboardLatestTransaction[]>> {
        return this.http.get<ApiResponse<DashboardLatestTransaction[]>>(
            `${this.apiUrl}/latest-transactions`
        );
    }

    getRecentNotifications():
        Observable<ApiResponse<DashboardRecentNotification[]>> {
        return this.http.get<ApiResponse<DashboardRecentNotification[]>>(
            `${this.apiUrl}/recent-notifications`
        );
    }

    getPortfolioDistribution():
        Observable<ApiResponse<DashboardPortfolioDistribution[]>> {
        return this.http.get<ApiResponse<DashboardPortfolioDistribution[]>>(
            `${this.apiUrl}/portfolio-distribution`
        );
    }
}