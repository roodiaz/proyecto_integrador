import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { interval, map, Observable, shareReplay, startWith, switchMap } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { MarketOverview, MarketStatus } from '../../../features/market/models/market.model';
import { LanguageService } from '../../../core/services/language.service';

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

/**
 * Servicio compartido que expone el estado global del mercado (abierto/cerrado,
 * texto y horario informados por el backend) para que cualquier sección de
 * InvestLab pueda mostrarlo sin duplicar la lógica de obtención ni de formateo
 * que originalmente vivía únicamente en la pantalla Mercado.
 */
@Injectable({ providedIn: 'root' })
export class MarketStatusService {
  private readonly apiUrl = `${environment.apiUrl}/market/overview`;
  private readonly languageService = inject(LanguageService);

  /** Estado del mercado, sondeado periódicamente y compartido entre todos los suscriptores. */
  private readonly status$: Observable<MarketStatus | null> = interval(60000).pipe(
    startWith(0),
    switchMap(() => this.http.get<ApiResponse<MarketOverview>>(this.apiUrl)),
    map(response => response.data?.marketStatus ?? null),
    shareReplay({ bufferSize: 1, refCount: false })
  );

  constructor(private http: HttpClient) {}

  /** @returns Un observable con el estado actual del mercado, actualizado automáticamente. */
  watchStatus(): Observable<MarketStatus | null> {
    return this.status$;
  }

  /** @returns `true` si el mercado se encuentra abierto según el estado informado. */
  isOpen(status: MarketStatus | null): boolean {
    return status?.isOpen ?? false;
  }

  /** @returns El texto descriptivo del estado del mercado (abierto/cerrado/etc.), o un mensaje genérico si no está disponible. */
  getStatusText(status: MarketStatus | null): string {
    return status?.statusText ?? this.languageService.instant('SIDEBAR.MARKET_STATUS_UNKNOWN');
  }

  /**
   * @param status Estado del mercado informado por el backend.
   * @param fallback Valor a utilizar si el estado no incluye un horario.
   * @returns La hora de mercado informada por el backend, o el valor de respaldo indicado.
   */
  getStatusTime(status: MarketStatus | null, fallback = ''): string {
    return status?.marketTime ?? fallback;
  }

  /**
   * Calcula el texto de cuenta regresiva hasta la próxima apertura o cierre del
   * mercado, a partir de la hora actual de mercado y los horarios de apertura
   * y cierre informados por el backend (todos en el mismo huso horario).
   *
   * Si el mercado está cerrado, la próxima apertura no siempre es "hoy/mañana a
   * las openTime" — si ese próximo horario cae en sábado o domingo, hay que
   * seguir avanzando hasta el próximo día hábil (de lo contrario el contador
   * decía, por ejemplo, "abre en 6h" un sábado a la noche, como si abriera un
   * domingo).
   * @param status Estado del mercado informado por el backend.
   * @returns "Cierra en Xh Ym" si el mercado está abierto, "Abre en Xh Ym" si está cerrado, o `null` si no hay datos suficientes.
   */
  getCountdownText(status: MarketStatus | null): string | null {
    if (!status) return null;

    const current = this.parseTimeToMinutes(status.marketTime);
    if (current === null) return null;

    if (status.isOpen) {
      const target = this.parseTimeToMinutes(status.closeTime);
      if (target === null) return null;

      let diffMinutes = target - current;
      if (diffMinutes < 0) diffMinutes += 24 * 60;
      return this.formatCountdown(diffMinutes, 'SIDEBAR.MARKET_CLOSES_IN');
    }

    const target = this.parseTimeToMinutes(status.openTime);
    if (target === null) return null;

    let daysAhead = current < target ? 0 : 1;
    let weekday = (this.getMarketWeekday(status.timeZone) + daysAhead) % 7;

    while (weekday === 0 || weekday === 6) {
      daysAhead++;
      weekday = (weekday + 1) % 7;
    }

    const diffMinutes = daysAhead * 24 * 60 + (target - current);
    return this.formatCountdown(diffMinutes, 'SIDEBAR.MARKET_OPENS_IN');
  }

  /** @returns El texto traducido de la cuenta regresiva para la cantidad de minutos y la clave de traducción indicadas. */
  private formatCountdown(diffMinutes: number, key: string): string {
    const hours = Math.floor(diffMinutes / 60);
    const minutes = diffMinutes % 60;
    const durationText = hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`;
    return this.languageService.instant(key, { duration: durationText });
  }

  /** @returns El día de la semana actual en el huso horario del mercado (0 = domingo ... 6 = sábado), igual que `Date.getDay()`. */
  private getMarketWeekday(timeZone: string): number {
    const weekdayNames = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
    const formatted = new Intl.DateTimeFormat('en-US', { timeZone, weekday: 'short' }).format(new Date());
    const index = weekdayNames.indexOf(formatted);
    return index >= 0 ? index : new Date().getDay();
  }

  /** @returns La cantidad de minutos transcurridos desde la medianoche para una hora en formato "HH:mm" o "HH:mm:ss", o `null` si el formato no es válido. */
  private parseTimeToMinutes(value: string | undefined): number | null {
    if (!value) return null;
    const match = /^(\d{1,2}):(\d{2})/.exec(value);
    if (!match) return null;
    return Number(match[1]) * 60 + Number(match[2]);
  }
}
