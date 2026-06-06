import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { interval, map, startWith, switchMap } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface MarketPriceCacheStatusDto {
  updatedAt: string | null;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

@Injectable({ providedIn: 'root' })
export class MarketPriceStatusService {
  private readonly apiUrl = `${environment.apiUrl}/market/prices-status`;

  constructor(private http: HttpClient) {}

  getStatus() {
    return this.http.get<ApiResponse<MarketPriceCacheStatusDto>>(this.apiUrl);
  }

  watchStatus() {
    return interval(30000).pipe(startWith(0), switchMap(() => this.getStatus()), map(response => response.data.updatedAt));
  }
}