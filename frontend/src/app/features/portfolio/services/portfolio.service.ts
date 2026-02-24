import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, interval, map, switchMap, startWith } from 'rxjs';
import { PortfolioOperation } from '../models/portfolio-operation';
import { PortfolioSummary } from '../models/portfolio-summary';

@Injectable({
  providedIn: 'root'
})
export class PortfolioService {
  private apiUrl = 'api/portfolio'; // Reemplazar con la URL real del backend

  constructor(private http: HttpClient) {}

  getOperations(): Observable<PortfolioOperation[]> {
    return this.http.get<PortfolioOperation[]>(`${this.apiUrl}/operations`);
  }

  getPortfolioSummary(): Observable<PortfolioSummary> {
    return this.http.get<PortfolioSummary>(`${this.apiUrl}/summary`);
  }

  buyOperation(ticker: string, quantity: number, price: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/buy`, { ticker, quantity, price });
  }

  sellOperation(operationId: string, price: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/sell`, { operationId, price });
  }

  getLiveUpdates(): Observable<{ operations: PortfolioOperation[], summary: PortfolioSummary }> {
    return interval(5000).pipe(
      startWith(0),
      switchMap(() => {
        return this.getOperations().pipe(
          map(operations => {
            // Calcular summary basado en las operaciones
            const summary = this.calculateSummary(operations);
            return { operations, summary };
          })
        );
      })
    );
  }

  private calculateSummary(operations: PortfolioOperation[]): PortfolioSummary {
    const initialBalance = 10000; // Saldo inicial fijo
    const totalInvested = operations
      .filter(op => op.isOpen)
      .reduce((sum, op) => sum + (op.quantity * op.buyPrice), 0);
    
    const currentValue = operations
      .filter(op => op.isOpen)
      .reduce((sum, op) => sum + (op.quantity * op.currentPrice), 0);
    
    const currentBalance = initialBalance - totalInvested + currentValue;
    const totalProfitLoss = currentBalance - initialBalance;
    const totalProfitLossPercent = (totalProfitLoss / initialBalance) * 100;

    return {
      initialBalance,
      currentBalance,
      totalProfitLoss,
      totalProfitLossPercent,
      totalInvested
    };
  }
}
