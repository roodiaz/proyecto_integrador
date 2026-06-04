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
            `${environment.apiUrl}/dashboard/top-cards`
        );
    }

    getPerformanceChart(period: string):
        Observable<ApiResponse<DashboardPerformanceChart>> {
        const filter: DashboardPerformanceChartFilter = { period };
        return this.http.post<ApiResponse<DashboardPerformanceChart>>(
            `${environment.apiUrl}/dashboard/performance-chart`, filter
        );
    }

    getLatestTransactions():
        Observable<ApiResponse<DashboardLatestTransaction[]>> {
        return this.http.get<ApiResponse<DashboardLatestTransaction[]>>(
            `${environment.apiUrl}/dashboard/latest-transactions`
        );
    }

    getRecentNotifications():
        Observable<ApiResponse<DashboardRecentNotification[]>> {
        return this.http.get<ApiResponse<DashboardRecentNotification[]>>(
            `${environment.apiUrl}/dashboard/recent-notifications`
        );
    }

    getPortfolioDistribution():
        Observable<ApiResponse<DashboardPortfolioDistribution[]>> {
        return this.http.get<ApiResponse<DashboardPortfolioDistribution[]>>(
            `${environment.apiUrl}/dashboard/portfolio-distribution`
        );
    }
}