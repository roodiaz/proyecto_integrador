import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { UserPortfolio } from '../../features/portfolio/models/portfolio.model';

@Injectable({
  providedIn: 'root'
})
export class ActivePortfolioService {

  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/portfolio`;

  private readonly _portfolios$ = new BehaviorSubject<UserPortfolio[]>([]);
  private readonly _activeId$ = new BehaviorSubject<number | null>(null);

  readonly portfolios$ = this._portfolios$.asObservable();
  readonly activeId$ = this._activeId$.asObservable();

  private readonly STORAGE_KEY = 'investlab-active-portfolio';

  get portfolios(): UserPortfolio[] {
    return this._portfolios$.value;
  }

  get activeId(): number | null {
    return this._activeId$.value;
  }

  get canCreateMore(): boolean {
    return this._portfolios$.value.length < 3;
  }

  loadPortfolios() {
    return this.http.get<ApiResponse<UserPortfolio[]>>(this.apiUrl).pipe(
      tap(res => {
        const portfolios = res.data ?? [];
        this._portfolios$.next(portfolios);
        this._activeId$.next(this.resolveActiveId(portfolios));
      })
    );
  }

  setActive(portfolioId: number): void {
    this._activeId$.next(portfolioId);
    localStorage.setItem(this.STORAGE_KEY, String(portfolioId));
    this.http.post<ApiResponse>(`${this.apiUrl}/${portfolioId}/activate`, {}).subscribe();
  }

  refresh() {
    return this.loadPortfolios();
  }

  private resolveActiveId(portfolios: UserPortfolio[]): number | null {
    if (portfolios.length === 0) return null;

    const stored = Number(localStorage.getItem(this.STORAGE_KEY));
    const storedMatch = portfolios.find(p => p.id === stored);
    if (storedMatch) return storedMatch.id;

    const active = portfolios.find(p => p.isActive);
    const resolved = (active ?? portfolios[0]).id;
    localStorage.setItem(this.STORAGE_KEY, String(resolved));
    return resolved;
  }
}
