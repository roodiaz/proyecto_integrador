import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MaterialModule } from '../../../../shared/material.module';
import { AuthService } from '../../services/auth.service';
import { LoginRequest } from '../../models/login.model';
import { TranslateModule } from '@ngx-translate/core';
import { LanguageService } from '../../../../core/services/language.service';

@Component({
  selector: 'app-login-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MaterialModule,
    TranslateModule
  ],
  templateUrl: './login-form.html',
  styleUrls: ['./login-form.css']
})
export class LoginForm {

  loginForm: FormGroup;
  hidePassword = true;
  loginError: string | null = null;

  private readonly languageService = inject(LanguageService);

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
          this.loginError = response.code
            ? this.languageService.instant(`ERRORS.${response.code}`)
            : response.message;
          return;
        }

        sessionStorage.setItem('accessToken',  response.data.tokens.accessToken);
        sessionStorage.setItem('refreshToken', response.data.tokens.refreshToken);

        this.router.navigate(['/dashboard']);
      },
      error: error => {
        const code = error.error?.code;
        this.loginError = code
          ? this.languageService.instant(`ERRORS.${code}`)
          : this.languageService.instant('ERRORS.INVALID_CREDENTIALS');
      }
    });
  }

  // ── Getters de acceso rápido ───────────────────────────────────────────────

  /** Referencia al control `email` para acceder a sus errores de validación en el template. */
  get email() { return this.loginForm.get('email'); }

  /** Referencia al control `password` para acceder a sus errores de validación en el template. */
  get password() { return this.loginForm.get('password'); }
}
