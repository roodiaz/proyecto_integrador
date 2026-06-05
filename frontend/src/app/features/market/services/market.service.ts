import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import { MarketOverview } from '../models/market.model';

@Injectable({
    providedIn: 'root'
})
export class MarketService {

    private http = inject(HttpClient);
    private apiUrl = `${environment.apiUrl}/market`;

    getMarketOverview():
        Observable<ApiResponse<MarketOverview>> {
        return this.http.get<ApiResponse<MarketOverview>>(
            `${this.apiUrl}/overview`
        );
    }
}