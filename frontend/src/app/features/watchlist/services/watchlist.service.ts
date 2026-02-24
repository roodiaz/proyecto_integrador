import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, interval, map, switchMap, startWith } from 'rxjs';
import { WatchlistItem } from '../models/watchlist-item';

@Injectable({
  providedIn: 'root'
})
export class WatchlistService {
  private apiUrl = 'api/watchlist'; // Reemplazar con la URL real del backend

  constructor(private http: HttpClient) {}

  getWatchlist(): Observable<WatchlistItem[]> {
    return this.http.get<WatchlistItem[]>(`${this.apiUrl}/favorites`);
  }

  addToWatchlist(ticker: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/favorites`, { ticker });
  }

  removeFromWatchlist(ticker: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/favorites/${ticker}`);
  }

  getLivePrices(): Observable<WatchlistItem[]> {
    return interval(5000).pipe(
      startWith(0),
      switchMap(() => this.getWatchlist())
    );
  }
}
