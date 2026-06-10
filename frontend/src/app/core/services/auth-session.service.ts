import { Injectable } from '@angular/core';
import { AuthService } from '../../features/auth/services/auth.service';
import { Router } from '@angular/router';
import { TokenRefreshService } from './token-refresh.service';

/**
 * Servicio que centraliza el cierre de sesión del usuario: invalida el
 * `refreshToken` en el servidor (cuando es posible), limpia el almacenamiento
 * de sesión local y redirige a la landing.
 */
@Injectable({
    providedIn: 'root'
})
export class AuthSessionService {

    /**
     * @param authService Servicio de autenticación, usado para invalidar el `refreshToken` en el servidor.
     * @param router Router de Angular, usado para redirigir a la pantalla de login.
     */
    constructor(
        private authService: AuthService,
        private router: Router,
        private tokenRefreshService: TokenRefreshService
    ) { }

    /**
     * Cierra la sesión del usuario actual: si existe un `refreshToken` guardado
     * intenta invalidarlo en el servidor y, en cualquier caso (éxito, error o
     * ausencia de token), limpia la sesión local y redirige al login.
     */
    logout(): void {
        const refreshToken = sessionStorage.getItem('refreshToken');

        if (!refreshToken) {
            this.clearSession();
            return;
        }

        this.authService.logout(refreshToken)
            .subscribe({
                next: () => this.clearSession(),
                error: () => this.clearSession()
            });
    }

    /** Limpia el almacenamiento de sesión local y redirige al usuario a la landing. */
    private clearSession(): void {
        this.tokenRefreshService.cancelProactiveRefresh();
        sessionStorage.clear();
        this.router.navigate(['/landing']);
    }
}
