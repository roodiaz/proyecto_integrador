import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import { DashboardTopCards } from '../models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
    private readonly apiUrl = `${environment.apiUrl}/dashboard`;

    constructor(private http: HttpClient) { }

    getTopCards(): Observable<ApiResponse<DashboardTopCards>> {
        return this.http.get<ApiResponse<DashboardTopCards>>(
            `${environment.apiUrl}/dashboard/top-cards`
        );
    }
}