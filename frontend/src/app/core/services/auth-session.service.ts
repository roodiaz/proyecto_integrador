import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { AuthService } from '../../features/auth/services/auth.service';
import { Router } from '@angular/router';

@Injectable({
    providedIn: 'root'
})
export class AuthSessionService {

    constructor(
        private authService: AuthService,
        private router: Router
    ) { }

    private clearSession(): void {
        sessionStorage.clear();
        this.router.navigate(['/login']);
    }

    logout(): void {

        const refreshToken =
            sessionStorage.getItem('refreshToken');

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
}