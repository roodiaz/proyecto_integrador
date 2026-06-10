import {
    HttpErrorResponse,
    HttpInterceptorFn
} from '@angular/common/http';

import { inject } from '@angular/core';
import { Router } from '@angular/router';

import {
    catchError,
    switchMap,
    throwError
} from 'rxjs';

import { TokenRefreshService } from '../services/token-refresh.service';

/**
 * Interceptor HTTP de autenticación.
 *
 * Agrega el `accessToken` almacenado como encabezado `Authorization` a cada
 * petición saliente. Si el servidor responde `401 Unauthorized` (y la petición
 * no es a un endpoint de autenticación), intenta renovar la sesión a través de
 * `TokenRefreshService` -compartiendo la renovación entre solicitudes
 * concurrentes para evitar reutilizar un refresh token ya rotado- y reintenta
 * la petición original con el nuevo token.
 *
 * La sesión solo se limpia y redirige al login cuando el refresh falla por un
 * token inválido/expirado confirmado por el servidor (o por ausencia de
 * refresh token); los errores de red o del servidor (5xx) durante el refresh
 * no cierran la sesión, ya que pueden ser transitorios.
 * @param req Petición HTTP saliente.
 * @param next Siguiente manejador en la cadena de interceptores.
 * @returns El observable de la respuesta HTTP, ya autenticada y, de ser necesario, reintentada.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {

    const tokenRefreshService = inject(TokenRefreshService);
    const router = inject(Router);

    const token = sessionStorage.getItem('accessToken');

    if (token) {
        req = req.clone({
            setHeaders: {
                Authorization: `Bearer ${token}`
            }
        });
    }

    return next(req).pipe(
        catchError((error: HttpErrorResponse) => {

            const isAuthEndpoint =
                req.url.includes('/auth/login') ||
                req.url.includes('/auth/register') ||
                req.url.includes('/auth/refresh') ||
                req.url.includes('/auth/logout');

            if (error.status === 401 && !isAuthEndpoint) {
                return tokenRefreshService.refreshAccessToken().pipe(
                    switchMap(accessToken => {
                        const retryRequest = req.clone({
                            setHeaders: {
                                Authorization: `Bearer ${accessToken}`
                            }
                        });

                        return next(retryRequest);
                    }),

                    catchError((refreshError: unknown) => {
                        const isHttpError = refreshError instanceof HttpErrorResponse;
                        const code = isHttpError ? refreshError.error?.code : undefined;

                        const isInvalidToken = isHttpError && refreshError.status === 400 &&
                            (code === 'INVALID_TOKEN' || code === 'TOKEN_EXPIRED');

                        const noRefreshToken = !sessionStorage.getItem('refreshToken');

                        if (isInvalidToken || noRefreshToken) {
                            sessionStorage.clear();
                            router.navigate(['/login']);
                        }

                        return throwError(() => error);
                    })
                );
            }

            return throwError(() => error);
        })
    );

};
