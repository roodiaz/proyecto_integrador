import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MaterialModule } from '../../../../shared/material.module';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../services/auth.service';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { ForgotPasswordRequest, ResetPasswordRequest } from '../../models/forgot-password.model';

/**
 * Pantalla de recuperación de contraseña.
 *
 * Funciona en dos pasos: primero solicita el email del usuario para enviarle
 * un código de recuperación (reutilizando la infraestructura de códigos de
 * verificación de `AuthService`), y luego solicita el código junto con la
 * nueva contraseña para restablecerla.
 */
@Component({
  selector: 'app-forgot-password-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MaterialModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './forgot-password-form.html',
  styleUrl: './forgot-password-form.css'
})
export class ForgotPasswordForm {

  // ── Estado de la vista ─────────────────────────────────────────────────────
  /** Indica si el código ya fue enviado, para mostrar el paso de restablecimiento. */
  codeSent = false;
  /** Indica si se está procesando una solicitud, para mostrar el spinner. */
  isLoading = false;
  /** Email al que se envió el código de recuperación, mostrado como referencia en el paso 2. */
  sentToEmail = '';

  // ── Formularios ────────────────────────────────────────────────────────────
  /** Formulario del paso 1: solicitud del código por email. */
  emailForm: FormGroup;
  /** Formulario del paso 2: código recibido y nueva contraseña. */
  resetForm: FormGroup;

  /** Controla la visibilidad del texto en el input de nueva contraseña. */
  hidePassword = true;
  /** Controla la visibilidad del texto en el input de confirmación de contraseña. */
  hideConfirmPassword = true;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private authService: AuthService,
    private snackBarService: SnackBarService
  ) {
    this.emailForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });

    this.resetForm = this.fb.group({
      code:            ['', [Validators.required]],
      newPassword:     ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    }, { validator: this.passwordMatchValidator });
  }

  // ── Getters de acceso rápido ───────────────────────────────────────────────

  /** Referencia al control `email` del paso 1 para acceder a sus errores de validación en el template. */
  get email() { return this.emailForm.get('email'); }

  /** Referencia al control `code` del paso 2 para acceder a sus errores de validación en el template. */
  get code() { return this.resetForm.get('code'); }

  /** Referencia al control `newPassword` del paso 2 para acceder a sus errores de validación en el template. */
  get newPassword() { return this.resetForm.get('newPassword'); }

  /** Referencia al control `confirmPassword` del paso 2 para acceder a sus errores de validación en el template. */
  get confirmPassword() { return this.resetForm.get('confirmPassword'); }

  // ── Acciones ───────────────────────────────────────────────────────────────

  /**
   * Solicita el envío del código de recuperación al email indicado.
   * Si el envío es exitoso, avanza al paso de restablecimiento de contraseña.
   */
  onSendCode(): void {
    if (this.emailForm.invalid) {
      this.emailForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;

    const request: ForgotPasswordRequest = { email: this.emailForm.value.email };

    this.authService.forgotPassword(request).subscribe({
      next: response => {
        this.isLoading = false;

        if (!response.success) {
          this.snackBarService.error(response.message ?? 'No pudimos enviar el código de recuperación');
          return;
        }

        this.sentToEmail = request.email;
        this.codeSent = true;
        this.snackBarService.success('Código enviado. Revisa tu correo electrónico.');
      },
      error: error => {
        this.isLoading = false;
        this.snackBarService.error(error.error?.message ?? 'No existe una cuenta asociada a ese email');
      }
    });
  }

  /**
   * Valida el código recibido y la nueva contraseña, y solicita el restablecimiento.
   * Si la operación es exitosa, muestra un mensaje de confirmación y redirige al login.
   */
  onResetPassword(): void {
    if (this.resetForm.invalid) {
      this.resetForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;

    const request: ResetPasswordRequest = {
      email: this.sentToEmail,
      code: this.resetForm.value.code,
      newPassword: this.resetForm.value.newPassword,
      confirmPassword: this.resetForm.value.confirmPassword
    };

    this.authService.resetPassword(request).subscribe({
      next: response => {
        this.isLoading = false;

        if (!response.success) {
          this.snackBarService.error(response.message ?? 'No pudimos restablecer la contraseña');
          return;
        }

        this.snackBarService.success('Contraseña actualizada correctamente');
        this.router.navigate(['/login']);
      },
      error: error => {
        this.isLoading = false;
        this.snackBarService.error(error.error?.message ?? 'Código inválido o expirado');
      }
    });
  }

  // ── Validadores ────────────────────────────────────────────────────────────

  /**
   * Validador a nivel de grupo que compara los campos `newPassword` y `confirmPassword`.
   * @param control El `AbstractControl` del grupo de formulario.
   * @returns `{ mismatch: true }` si las contraseñas no coinciden, o `null` si son iguales.
   */
  private passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const newPassword     = control.get('newPassword');
    const confirmPassword = control.get('confirmPassword');

    if (!newPassword || !confirmPassword) return null;

    return newPassword.value === confirmPassword.value ? null : { mismatch: true };
  }
}
