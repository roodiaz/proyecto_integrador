import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import {
    MarketOverview,
    MarketAsset,
    MarketMover,
    MarketNews
} from '../models/market.model';

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

    getAssetDetail(symbol: string):
        Observable<ApiResponse<MarketAsset>> {
        return this.http.get<ApiResponse<MarketAsset>>(
            `${this.apiUrl}/asset/${symbol}`
        );
    }

    getTrending(): Observable<ApiResponse<MarketMover[]>> {
        return this.http.get<ApiResponse<MarketMover[]>>(
            `${this.apiUrl}/trending`
        );
    }

    getGainers(): Observable<ApiResponse<MarketMover[]>> {
        return this.http.get<ApiResponse<MarketMover[]>>(
            `${this.apiUrl}/gainers`
        );
    }

    getLosers(): Observable<ApiResponse<MarketMover[]>> {
        return this.http.get<ApiResponse<MarketMover[]>>(
            `${this.apiUrl}/losers`
        );
    }

    getMarketNews(): Observable<ApiResponse<MarketNews[]>> {
        return this.http.get<ApiResponse<MarketNews[]>>(
            `${this.apiUrl}/news`
        );
    }
}