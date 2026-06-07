import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import { PortfolioPosition, SellData } from '../models/portfolio.modal.model';
import {
  PortfolioBalanceCards,
  PortfolioPieChartItem,
  PortfolioLineChartItem,
  OpenPositionsFilter,
  PagedOpenPositions,
  TransactionFilter,
  PagedTransactions
} from '../models/portfolio.model';

@Injectable({
  providedIn: 'root'
})
export class PortfolioService {
  private apiUrl = `${environment.apiUrl}/portfolio`;

  constructor(private http: HttpClient) { }

  getBalanceCards(): Observable<ApiResponse<PortfolioBalanceCards>> {
    return this.http.get<ApiResponse<PortfolioBalanceCards>>(`${this.apiUrl}/balance-cards`);
  }

  getPieChart(): Observable<ApiResponse<PortfolioPieChartItem[]>> {
    return this.http.get<ApiResponse<PortfolioPieChartItem[]>>(`${this.apiUrl}/pie-chart`);
  }

  getLineChart(period: string): Observable<ApiResponse<PortfolioLineChartItem[]>> {
    return this.http.post<ApiResponse<PortfolioLineChartItem[]>>(`${this.apiUrl}/line-chart`, { period });
  }

  getOpenPositions(filter: OpenPositionsFilter): Observable<ApiResponse<PagedOpenPositions>> {
    return this.http.post<ApiResponse<PagedOpenPositions>>(`${this.apiUrl}/open-positions`, filter);
  }

  getTransactionHistory(filter: TransactionFilter): Observable<ApiResponse<PagedTransactions>> {
    return this.http.post<ApiResponse<PagedTransactions>>(`${this.apiUrl}/history`, filter);
  }

  getAssetPrice(symbol: string): Observable<ApiResponse<{ symbol: string; currentPrice: number }>> {
    return this.http.get<ApiResponse<{ symbol: string; currentPrice: number }>>(`${this.apiUrl}/price/${symbol}`);
  }

  buyAsset(symbol: string, quantity: number): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(`${this.apiUrl}/buy`, { symbol, quantity });
  }

  getPosition(symbol: string): Observable<ApiResponse<PortfolioPosition>> {
    return this.http.get<ApiResponse<PortfolioPosition>>(`${this.apiUrl}/${symbol}`);
  }

  sell(data: SellData): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(`${this.apiUrl}/sell`, data);
  }

  resetSimulation(): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(`${this.apiUrl}/reset-simulation`, {});
  }
}
