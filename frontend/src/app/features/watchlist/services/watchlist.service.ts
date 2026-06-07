import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FavoriteFilter, FavoriteListResponse } from '../models/watchlist-item';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';

@Injectable({
  providedIn: 'root'
})
export class WatchlistService {
  private readonly apiUrl = `${environment.apiUrl}/favorite`;

  constructor(private http: HttpClient) { }

  getFavorites(filter: FavoriteFilter): Observable<FavoriteListResponse> {
    return this.http.post<FavoriteListResponse>(`${this.apiUrl}/list`, filter);
  }

  addFavorite(symbol: string): Observable<ApiResponse> {
    const normalizedSymbol = symbol.trim().toUpperCase();
    return this.http.post<ApiResponse>(`${this.apiUrl}/add`, { symbol: normalizedSymbol });
  }

  removeFavorite(symbol: string): Observable<ApiResponse> {
    const normalizedSymbol = symbol.trim().toUpperCase();
    return this.http.delete<ApiResponse>(`${this.apiUrl}/${normalizedSymbol}`);
  }

  existsFavorite(symbol: string): Observable<ApiResponse<boolean>> {
    const normalizedSymbol = symbol.trim().toUpperCase();
    return this.http.get<ApiResponse<boolean>>(`${this.apiUrl}/exists/${normalizedSymbol}`);
  }
}
