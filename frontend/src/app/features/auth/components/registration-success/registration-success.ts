import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MaterialModule } from '../../../../shared/material.module';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { FormsModule } from '@angular/forms';
import { SnackBarService } from '../../../../core/services/snackbar.service';

@Component({
  selector: 'app-registration-success',
  standalone: true,
  imports: [
    CommonModule,
    MaterialModule,
    RouterModule,
    FormsModule
  ],
  templateUrl: './registration-success.html',
  styleUrl: './registration-success.css'
})
export class RegistrationSuccess implements OnInit, OnDestroy {

  // ── Estado de la vista ─────────────────────────────────────────────────────
  /** Email del usuario recién registrado, leído de `localStorage`. */
  userEmail = '';
  /** Indica si el correo de verificación fue enviado exitosamente. */
  emailSent = true;

  // ── Contador regresivo ─────────────────────────────────────────────────────
  /** Segundos restantes para que expire el código de verificación. */
  remainingTime = 15 * 60;
  /** Representación formateada `MM:SS` del tiempo restante para el template. */
  timeDisplay = '';
  private intervalId: any;

  // ── Verificación ───────────────────────────────────────────────────────────
  /** Código ingresado por el usuario para verificar su cuenta. */
  verificationCode = '';

  constructor(
    private router: Router,
    private authService: AuthService,
    private notificationService: SnackBarService
  ) { }

  ngOnInit(): void {
    this.userEmail = localStorage.getItem('registrationEmail') || 'tu correo electrónico';
    this.emailSent = localStorage.getItem('emailSent') === 'true';

    this.startCountdown();
  }

  ngOnDestroy(): void {
    if (this.intervalId) {
      clearInterval(this.intervalId);
    }
  }

  // ── Acciones ───────────────────────────────────────────────────────────────

  /** Navega directamente a la pantalla de inicio de sesión. */
  goToLogin(): void {
    this.router.navigate(['/login']);
  }

  /**
   * Solicita el reenvío del correo de verificación al email registrado.
   * Si el reenvío es exitoso, reinicia el contador regresivo desde 15 minutos.
   */
  resendEmail(): void {
    this.authService.resendCode(this.userEmail).subscribe({
      next: response => {
        if (!response.success) return;

        this.emailSent = response.data!.emailSent;

        if (response.data!.emailSent) {
          this.remainingTime = 15 * 60;

          if (this.intervalId) clearInterval(this.intervalId);

          this.startCountdown();
        }
      },
      error: error => {
        console.error('Error reenviando código', error);
      }
    });
  }

  /**
   * Envía el código ingresado por el usuario para verificar su cuenta.
   * Si la verificación es exitosa, muestra un mensaje y redirige al login
   * tras un breve delay para que el usuario pueda leer la confirmación.
   */
  verifyCode(): void {
    const request = { email: this.userEmail, code: this.verificationCode };

    this.authService.verifyCode(request).subscribe({
      next: response => {
        if (!response.success) return;

        this.notificationService.success(response.message);

        setTimeout(() => this.router.navigate(['/login']), 1500);
      },
      error: error => {
        this.notificationService.error(error.error?.message ?? 'Ocurrió un error');
      }
    });
  }

  // ── Contador regresivo (privado) ───────────────────────────────────────────

  /**
   * Inicia el intervalo de cuenta regresiva de 1 segundo.
   * Actualiza `timeDisplay` en cada tick y redirige al login cuando llega a cero.
   */
  private startCountdown(): void {
    this.updateTimeDisplay();

    this.intervalId = setInterval(() => {
      this.remainingTime--;
      this.updateTimeDisplay();

      if (this.remainingTime <= 0) {
        clearInterval(this.intervalId);
        this.router.navigate(['/login']);
      }
    }, 1000);
  }

  /**
   * Convierte `remainingTime` (en segundos) al formato `MM:SS`
   * y lo asigna a `timeDisplay` para renderizarlo en el template.
   */
  private updateTimeDisplay(): void {
    const minutes = Math.floor(this.remainingTime / 60);
    const seconds = this.remainingTime % 60;
    this.timeDisplay = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
  }
}
