import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MaterialModule } from '../../../../shared/material.module';
import { Router, RouterModule } from '@angular/router';
import { RegisterRequest } from '../../models/register.model';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MaterialModule,
    RouterModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './register-form.html',
  styleUrl: './register-form.css'
})
export class RegisterForm {

  // ── Estado del formulario ──────────────────────────────────────────────────
  /** Formulario reactivo con los campos de registro del usuario. */
  registerForm: FormGroup;
  /** Controla la visibilidad del texto en el input de contraseña. */
  hidePassword = true;
  /** Controla la visibilidad del texto en el input de confirmación de contraseña. */
  hideConfirmPassword = true;

  // ── Estado de la vista ─────────────────────────────────────────────────────
  /** Indica si se está procesando el registro para mostrar el spinner. */
  isLoading = false;
  /** Mensaje de error a mostrar cuando el registro falla. */
  errorMessage: string | null = null;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private authService: AuthService
  ) {
    this.registerForm = this.fb.group({
      fullName:        ['', [Validators.required]],
      email:           ['', [Validators.required, Validators.email]],
      phone:           ['', [Validators.pattern(/^[+]?[\d\s\-\(\)]+$/)]],
      password:        ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    }, { validator: this.passwordMatchValidator });
  }

  // ── Getters de acceso rápido ───────────────────────────────────────────────

  /** Referencia al control `fullName` para acceder a sus errores de validación en el template. */
  get fullName()        { return this.registerForm.get('fullName'); }

  /** Referencia al control `email` para acceder a sus errores de validación en el template. */
  get email()           { return this.registerForm.get('email'); }

  /** Referencia al control `phone` para acceder a sus errores de validación en el template. */
  get phone()           { return this.registerForm.get('phone'); }

  /** Referencia al control `password` para acceder a sus errores de validación en el template. */
  get password()        { return this.registerForm.get('password'); }

  /** Referencia al control `confirmPassword` para acceder a sus errores de validación en el template. */
  get confirmPassword() { return this.registerForm.get('confirmPassword'); }

  // ── Acciones ───────────────────────────────────────────────────────────────

  /**
   * Valida el formulario y envía los datos al servicio de registro.
   * Si el formulario es inválido, marca todos los campos como tocados para
   * mostrar los errores. Si el registro es exitoso, guarda el email en
   * `localStorage` y redirige a la pantalla de confirmación.
   */
  onFormSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading    = true;
    this.errorMessage = null;

    const request: RegisterRequest = { ...this.registerForm.value };

    this.authService.register(request).subscribe({
      next: response => {
        this.isLoading = false;

        localStorage.setItem('registrationEmail', response.data!.email);
        localStorage.setItem('emailSent',          response.data!.emailSent.toString());

        this.router.navigate(['/registration-success']);
      },
      error: error => {
        this.isLoading    = false;
        this.errorMessage = error.error?.message ?? 'Ocurrio un error al registrarse';
      }
    });
  }

  // ── Validadores ────────────────────────────────────────────────────────────

  /**
   * Validador a nivel de grupo que compara los campos `password` y `confirmPassword`.
   * Se aplica al `FormGroup` completo para acceder a ambos controles simultáneamente.
   * @param control El `AbstractControl` del grupo de formulario.
   * @returns `{ mismatch: true }` si las contraseñas no coinciden, o `null` si son iguales.
   */
  private passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password        = control.get('password');
    const confirmPassword = control.get('confirmPassword');

    if (!password || !confirmPassword) return null;

    return password.value === confirmPassword.value ? null : { mismatch: true };
  }
}
