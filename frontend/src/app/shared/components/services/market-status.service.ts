import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { interval, map, Observable, shareReplay, startWith, switchMap } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { MarketOverview, MarketStatus } from '../../../features/market/models/market.model';

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
    return status?.statusText ?? 'Estado no disponible';
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
   * @param status Estado del mercado informado por el backend.
   * @returns "Cierra en Xh Ym" si el mercado está abierto, "Abre en Xh Ym" si está cerrado, o `null` si no hay datos suficientes.
   */
  getCountdownText(status: MarketStatus | null): string | null {
    if (!status) return null;

    const current = this.parseTimeToMinutes(status.marketTime);
    const target = this.parseTimeToMinutes(status.isOpen ? status.closeTime : status.openTime);
    if (current === null || target === null) return null;

    let diffMinutes = target - current;
    if (diffMinutes < 0) diffMinutes += 24 * 60;

    const hours = Math.floor(diffMinutes / 60);
    const minutes = diffMinutes % 60;
    const durationText = hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`;

    return status.isOpen ? `Cierra en ${durationText}` : `Abre en ${durationText}`;
  }

  /** @returns La cantidad de minutos transcurridos desde la medianoche para una hora en formato "HH:mm" o "HH:mm:ss", o `null` si el formato no es válido. */
  private parseTimeToMinutes(value: string | undefined): number | null {
    if (!value) return null;
    const match = /^(\d{1,2}):(\d{2})/.exec(value);
    if (!match) return null;
    return Number(match[1]) * 60 + Number(match[2]);
  }
}
