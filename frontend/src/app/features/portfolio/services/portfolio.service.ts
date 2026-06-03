import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
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
  private apiUrl = 'api/portfolio'; // Reemplazar con la URL real del backend

  constructor(private http: HttpClient) { }

  getBalanceCards(): Observable<ApiResponse<PortfolioBalanceCards>> {
    return this.http.get<ApiResponse<PortfolioBalanceCards>>(
      `${environment.apiUrl}/portfolio/balance-cards`
    );
  }

  getPieChart(): Observable<ApiResponse<PortfolioPieChartItem[]>> {

    return this.http.get<ApiResponse<PortfolioPieChartItem[]>>(
      `${environment.apiUrl}/portfolio/pie-chart`
    );

  }

  getLineChart(period: string):
    Observable<ApiResponse<PortfolioLineChartItem[]>> {

    return this.http.post<
      ApiResponse<PortfolioLineChartItem[]>
    >(
      `${environment.apiUrl}/portfolio/line-chart`,
      { period }
    );

  }

  getOpenPositions(filter: OpenPositionsFilter):
    Observable<ApiResponse<PagedOpenPositions>> {

    return this.http.post<
      ApiResponse<PagedOpenPositions>
    >(
      `${environment.apiUrl}/portfolio/open-positions`,
      filter
    );
  }

  getTransactionHistory(filter: TransactionFilter):
    Observable<ApiResponse<PagedTransactions>> {

    return this.http.post<
      ApiResponse<PagedTransactions>
    >(
      `${environment.apiUrl}/portfolio/history`,
      filter
    );

  }
}
