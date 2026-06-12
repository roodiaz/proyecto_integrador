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
    DashboardPortfolioDistribution,
    DashboardPortfolioComposition
} from '../models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
    private readonly apiUrl = `${environment.apiUrl}/dashboard`;

    constructor(private http: HttpClient) { }

    getTopCards(portfolioId: number):
        Observable<ApiResponse<DashboardTopCards>> {
        return this.http.get<ApiResponse<DashboardTopCards>>(
            `${this.apiUrl}/top-cards?portfolioId=${portfolioId}`
        );
    }

    getPerformanceChart(portfolioId: number, period: string):
        Observable<ApiResponse<DashboardPerformanceChart>> {
        const filter: DashboardPerformanceChartFilter = { period };
        return this.http.post<ApiResponse<DashboardPerformanceChart>>(
            `${this.apiUrl}/performance-chart?portfolioId=${portfolioId}`, filter
        );
    }

    getLatestTransactions(portfolioId: number):
        Observable<ApiResponse<DashboardLatestTransaction[]>> {
        return this.http.get<ApiResponse<DashboardLatestTransaction[]>>(
            `${this.apiUrl}/latest-transactions?portfolioId=${portfolioId}`
        );
    }

    getRecentNotifications():
        Observable<ApiResponse<DashboardRecentNotification[]>> {
        return this.http.get<ApiResponse<DashboardRecentNotification[]>>(
            `${this.apiUrl}/recent-notifications`
        );
    }

    getPortfolioDistribution(portfolioId: number):
        Observable<ApiResponse<DashboardPortfolioDistribution[]>> {
        return this.http.get<ApiResponse<DashboardPortfolioDistribution[]>>(
            `${this.apiUrl}/portfolio-distribution?portfolioId=${portfolioId}`
        );
    }

    getPortfolioComposition(portfolioId: number, date: string):
        Observable<ApiResponse<DashboardPortfolioComposition>> {
        return this.http.get<ApiResponse<DashboardPortfolioComposition>>(
            `${this.apiUrl}/portfolio-composition?portfolioId=${portfolioId}&date=${date}`
        );
    }
}