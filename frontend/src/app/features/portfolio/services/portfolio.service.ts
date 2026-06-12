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
  PagedTransactions,
  UserPortfolio,
  SetupPortfolioRequest
} from '../models/portfolio.model';

@Injectable({
  providedIn: 'root'
})
export class PortfolioService {
  private apiUrl = `${environment.apiUrl}/portfolio`;

  constructor(private http: HttpClient) { }

  getUserPortfolios(): Observable<ApiResponse<UserPortfolio[]>> {
    return this.http.get<ApiResponse<UserPortfolio[]>>(this.apiUrl);
  }

  createPortfolio(dto: SetupPortfolioRequest): Observable<ApiResponse<UserPortfolio>> {
    return this.http.post<ApiResponse<UserPortfolio>>(this.apiUrl, dto);
  }

  setActivePortfolio(portfolioId: number): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(`${this.apiUrl}/${portfolioId}/activate`, {});
  }

  deletePortfolio(portfolioId: number): Observable<ApiResponse> {
    return this.http.delete<ApiResponse>(`${this.apiUrl}/${portfolioId}`);
  }

  resetPortfolio(portfolioId: number, dto: SetupPortfolioRequest): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(`${this.apiUrl}/${portfolioId}/reset`, dto);
  }

  getBalanceCards(portfolioId: number): Observable<ApiResponse<PortfolioBalanceCards>> {
    return this.http.get<ApiResponse<PortfolioBalanceCards>>(`${this.apiUrl}/${portfolioId}/balance-cards`);
  }

  getPieChart(portfolioId: number): Observable<ApiResponse<PortfolioPieChartItem[]>> {
    return this.http.get<ApiResponse<PortfolioPieChartItem[]>>(`${this.apiUrl}/${portfolioId}/pie-chart`);
  }

  getLineChart(portfolioId: number, period: string): Observable<ApiResponse<PortfolioLineChartItem[]>> {
    return this.http.post<ApiResponse<PortfolioLineChartItem[]>>(`${this.apiUrl}/${portfolioId}/line-chart`, { period });
  }

  getOpenPositions(portfolioId: number, filter: OpenPositionsFilter): Observable<ApiResponse<PagedOpenPositions>> {
    return this.http.post<ApiResponse<PagedOpenPositions>>(`${this.apiUrl}/${portfolioId}/open-positions`, filter);
  }

  getTransactionHistory(portfolioId: number, filter: TransactionFilter): Observable<ApiResponse<PagedTransactions>> {
    return this.http.post<ApiResponse<PagedTransactions>>(`${this.apiUrl}/${portfolioId}/history`, filter);
  }

  getAssetPrice(symbol: string): Observable<ApiResponse<{ symbol: string; currentPrice: number }>> {
    return this.http.get<ApiResponse<{ symbol: string; currentPrice: number }>>(`${this.apiUrl}/price/${symbol}`);
  }

  buyAsset(portfolioId: number, symbol: string, quantity: number): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(`${this.apiUrl}/${portfolioId}/buy`, { symbol, quantity });
  }

  getPosition(portfolioId: number, symbol: string): Observable<ApiResponse<PortfolioPosition>> {
    return this.http.get<ApiResponse<PortfolioPosition>>(`${this.apiUrl}/${portfolioId}/position/${symbol}`);
  }

  sell(portfolioId: number, data: SellData): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(`${this.apiUrl}/${portfolioId}/sell`, data);
  }

  exportHoldings(portfolioId: number, filter: OpenPositionsFilter): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/${portfolioId}/export/holdings`, filter, { responseType: 'blob' });
  }

  exportTransactions(portfolioId: number, filter: TransactionFilter): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/${portfolioId}/export/transactions`, filter, { responseType: 'blob' });
  }
}
