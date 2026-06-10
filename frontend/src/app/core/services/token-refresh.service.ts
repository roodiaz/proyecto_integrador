import { Injectable, inject } from '@angular/core';
import { Observable, finalize, map, shareReplay, throwError } from 'rxjs';
import { AuthService } from '../../features/auth/services/auth.service';
import { getJwtExpiration } from '../utils/jwt.util';

/** Margen de seguridad antes de la expiración del access token para disparar la renovación proactiva. */
const PROACTIVE_REFRESH_BUFFER_MS = 60_000;

/**
 * Centraliza la renovación del access token para evitar condiciones de carrera:
 * si hay múltiples solicitudes 401 simultáneas, todas comparten la misma
 * petición de refresh en curso en lugar de disparar una por cada una
 * (lo que invalidaría el refresh token de un solo uso para las demás).
 *
 * También programa una renovación proactiva poco antes de que expire el
 * access token, para reducir la cantidad de respuestas 401 que el usuario
 * llega a experimentar.
 */
@Injectable({ providedIn: 'root' })
export class TokenRefreshService {

    private authService = inject(AuthService);

    private inFlight$: Observable<string> | null = null;
    private proactiveTimer: ReturnType<typeof setTimeout> | null = null;

    constructor() {
        const accessToken = sessionStorage.getItem('accessToken');

        if (accessToken) {
            this.scheduleProactiveRefresh(accessToken);
        }
    }

    /**
     * Renueva el access token utilizando el refresh token almacenado.
     * Si ya hay una renovación en curso, todos los llamados comparten el
     * mismo resultado en lugar de disparar múltiples solicitudes de refresh.
     * @returns Un observable que emite el nuevo access token.
     */
    refreshAccessToken(): Observable<string> {
        const refreshToken = sessionStorage.getItem('refreshToken');

        if (!refreshToken) {
            return throwError(() => new Error('NO_REFRESH_TOKEN'));
        }

        if (this.inFlight$) {
            return this.inFlight$;
        }

        this.inFlight$ = this.authService.refreshToken().pipe(
            map(response => {
                const tokens = response.data!.tokens;

                sessionStorage.setItem('accessToken', tokens.accessToken);
                sessionStorage.setItem('refreshToken', tokens.refreshToken);

                this.scheduleProactiveRefresh(tokens.accessToken);

                return tokens.accessToken;
            }),
            finalize(() => { this.inFlight$ = null; }),
            shareReplay(1)
        );

        return this.inFlight$;
    }

    /**
     * Programa una renovación automática del access token un minuto antes de
     * que expire, para minimizar los casos en que el usuario reciba un 401.
     * @param accessToken Access token vigente, usado para leer su fecha de expiración.
     */
    scheduleProactiveRefresh(accessToken: string): void {
        this.cancelProactiveRefresh();

        const expiresAt = getJwtExpiration(accessToken);

        if (!expiresAt) {
            return;
        }

        const delay = expiresAt - Date.now() - PROACTIVE_REFRESH_BUFFER_MS;

        if (delay <= 0) {
            return;
        }

        this.proactiveTimer = setTimeout(() => {
            this.refreshAccessToken().subscribe({ error: () => { /* el interceptor maneja el error en el próximo request */ } });
        }, delay);
    }

    /** Cancela la renovación proactiva programada, si existe (por ejemplo, al cerrar sesión). */
    cancelProactiveRefresh(): void {
        if (this.proactiveTimer) {
            clearTimeout(this.proactiveTimer);
            this.proactiveTimer = null;
        }
    }
}
