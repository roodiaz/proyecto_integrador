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

import { AuthService } from '../../features/auth/services/auth.service';
import { ApiResponse } from '../models/api-response.model';
import { LoginData } from '../../features/auth/models/login.model';

/**
 * Interceptor HTTP de autenticación.
 *
 * Agrega el `accessToken` almacenado como encabezado `Authorization` a cada
 * petición saliente. Si el servidor responde `401 Unauthorized` (y la petición
 * no es a un endpoint de autenticación), intenta renovar la sesión con
 * `refreshToken` y reintenta la petición original con el nuevo token. Si la
 * renovación falla, limpia la sesión y redirige al login.
 * @param req Petición HTTP saliente.
 * @param next Siguiente manejador en la cadena de interceptores.
 * @returns El observable de la respuesta HTTP, ya autenticada y, de ser necesario, reintentada.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {

    const authService = inject(AuthService);
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
                req.url.includes('/auth/refresh');

            if (error.status === 401 && !isAuthEndpoint) {
                return authService.refreshToken().pipe(
                    switchMap((response: ApiResponse<LoginData>) => {

                        if (!response.success || !response.data) {
                            sessionStorage.clear();
                            router.navigate(['/login']);

                            return throwError(() => error);
                        }

                        const accessToken = response.data.tokens.accessToken;
                        const refreshToken = response.data.tokens.refreshToken;

                        sessionStorage.setItem('accessToken', accessToken);
                        sessionStorage.setItem('refreshToken', refreshToken);

                        const retryRequest = req.clone({
                            setHeaders: {
                                Authorization: `Bearer ${accessToken}`
                            }
                        });

                        return next(retryRequest);
                    }),

                    catchError(() => {
                        sessionStorage.clear();
                        router.navigate(['/login']);
                        return throwError(() => error);
                    })
                );
            }

            return throwError(() => error);
        })
    );

};
