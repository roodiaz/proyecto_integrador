import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FavoriteFilter, FavoriteListResponse } from '../models/watchlist-item';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';

@Injectable({
  providedIn: 'root'
})
export class WatchlistService {
  private apiUrl = 'api/watchlist'; // Reemplazar con la URL real del backend

  constructor(private http: HttpClient) { }

  getFavorites(filter: FavoriteFilter) {

    return this.http.post<FavoriteListResponse>(
      `${environment.apiUrl}/favorite/list`,
      filter
    );
  }

  addFavorite(symbol: string) {
    return this.http.post<ApiResponse>(
      `${environment.apiUrl}/favorite/add`,
      {
        symbol
      }
    );
  }

  removeFavorite(symbol: string) {
    return this.http.delete(
      `${environment.apiUrl}/favorite/${symbol}`
    );
  }
}
