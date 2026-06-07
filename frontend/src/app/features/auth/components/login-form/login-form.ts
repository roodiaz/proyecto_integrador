import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MaterialModule } from '../../../../shared/material.module';
import { AuthService } from '../../services/auth.service';
import { LoginRequest } from '../../models/login.model';

@Component({
  selector: 'app-login-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MaterialModule
  ],
  templateUrl: './login-form.html',
  styleUrls: ['./login-form.css']
})
export class LoginForm {

  // ── Estado del formulario ──────────────────────────────────────────────────
  /** Formulario reactivo con los campos email y contraseña. */
  loginForm: FormGroup;
  /** Controla la visibilidad del texto en el input de contraseña. */
  hidePassword = true;
  /** Mensaje de error a mostrar cuando el inicio de sesión falla. */
  loginError: string | null = null;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private authService: AuthService
  ) {
    this.loginForm = this.fb.group({
      email:    ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(4)]]
    });
  }

  // ── Acciones ───────────────────────────────────────────────────────────────

  /**
   * Valida el formulario y envía las credenciales al servicio de autenticación.
   * Si la respuesta es exitosa, almacena los tokens en `sessionStorage` y
   * redirige al dashboard. En caso de error muestra el mensaje correspondiente.
   */
  onSubmit(): void {
    if (!this.loginForm.valid) return;

    this.loginError = null;

    const request: LoginRequest = {
      email:    this.loginForm.value.email,
      password: this.loginForm.value.password
    };

    this.authService.login(request).subscribe({
      next: response => {
        if (!response.success || !response.data) {
          this.loginError = response.message;
          return;
        }

        sessionStorage.setItem('accessToken',  response.data.tokens.accessToken);
        sessionStorage.setItem('refreshToken', response.data.tokens.refreshToken);

        this.router.navigate(['/dashboard']);
      },
      error: error => {
        this.loginError = error.error?.message ?? 'Error al iniciar sesión';
      }
    });
  }

  // ── Getters de acceso rápido ───────────────────────────────────────────────

  /** Referencia al control `email` para acceder a sus errores de validación en el template. */
  get email() { return this.loginForm.get('email'); }

  /** Referencia al control `password` para acceder a sus errores de validación en el template. */
  get password() { return this.loginForm.get('password'); }
}
