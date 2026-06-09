import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MaterialModule } from '../../../../shared/material.module';
import { AuthService } from '../../services/auth.service';
import { LoginRequest } from '../../models/login.model';
import { VerifyRequest } from '../../models/register.model';
import { TranslateModule } from '@ngx-translate/core';
import { LanguageService } from '../../../../core/services/language.service';

@Component({
  selector: 'app-login-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
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

  verificationStatus: 'idle' | 'pendingCode' | 'verifying' | 'resending' = 'idle';
  verificationCode = '';
  verificationError: string | null = null;
  verificationSuccess: string | null = null;
  pendingEmail = '';

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
    this.verificationStatus = 'idle';
    this.verificationError = null;

    const request: LoginRequest = {
      email:    this.loginForm.value.email,
      password: this.loginForm.value.password
    };

    this.authService.login(request).subscribe({
      next: response => {
        if (!response.success || !response.data) {
          if (response.code === 'ACCOUNT_NOT_VERIFIED') {
            this.pendingEmail = this.loginForm.value.email;
            this.verificationStatus = 'pendingCode';
            this.verificationCode = '';
            return;
          }
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
        if (code === 'ACCOUNT_NOT_VERIFIED') {
          this.pendingEmail = this.loginForm.value.email;
          this.verificationStatus = 'pendingCode';
          this.verificationCode = '';
          return;
        }
        this.loginError = code
          ? this.languageService.instant(`ERRORS.${code}`)
          : this.languageService.instant('ERRORS.INVALID_CREDENTIALS');
      }
    });
  }

  onVerifyCode(): void {
    if (this.verificationStatus === 'verifying' || !this.verificationCode.trim()) return;

    this.verificationStatus = 'verifying';
    this.verificationError = null;

    const request: VerifyRequest = {
      email: this.pendingEmail,
      code: this.verificationCode.trim()
    };

    this.authService.verifyCode(request).subscribe({
      next: response => {
        if (!response.success) {
          this.verificationStatus = 'pendingCode';
          this.verificationError = response.message || this.languageService.instant('AUTH.REGISTRATION_SUCCESS.VALIDATION.CODE_REQUIRED');
          return;
        }
        this.verificationStatus = 'idle';
        this.loginError = null;
        this.verificationError = null;
        this.authService.login({
          email: this.loginForm.value.email,
          password: this.loginForm.value.password
        }).subscribe({
          next: resp => {
            if (!resp.success || !resp.data) {
              this.loginError = resp.message || this.languageService.instant('ERRORS.INVALID_CREDENTIALS');
              return;
            }
            sessionStorage.setItem('accessToken', resp.data.tokens.accessToken);
            sessionStorage.setItem('refreshToken', resp.data.tokens.refreshToken);
            this.router.navigate(['/dashboard']);
          },
          error: () => {
            this.loginError = this.languageService.instant('AUTH.LOGIN.VERIFIED_REDIRECT');
          }
        });
      },
      error: error => {
        this.verificationStatus = 'pendingCode';
        this.verificationError = error.error?.message || this.languageService.instant('AUTH.REGISTRATION_SUCCESS.VALIDATION.CODE_REQUIRED');
      }
    });
  }

  onResendCode(): void {
    if (this.verificationStatus === 'resending') return;

    this.verificationStatus = 'resending';
    this.verificationError = null;
    this.verificationSuccess = null;

    this.authService.resendCode(this.pendingEmail).subscribe({
      next: () => {
        this.verificationStatus = 'pendingCode';
        this.verificationSuccess = this.languageService.instant('SUCCESS.CODE_RESENT');
      },
      error: () => {
        this.verificationStatus = 'pendingCode';
        this.verificationError = this.languageService.instant('SUCCESS.CODE_RESEND_FAILED');
      }
    });
  }

  cancelVerification(): void {
    this.verificationStatus = 'idle';
    this.verificationCode = '';
    this.verificationError = null;
    this.verificationSuccess = null;
    this.pendingEmail = '';
  }

  // ── Getters de acceso rápido ───────────────────────────────────────────────

  /** Referencia al control `email` para acceder a sus errores de validación en el template. */
  get email() { return this.loginForm.get('email'); }

  /** Referencia al control `password` para acceder a sus errores de validación en el template. */
  get password() { return this.loginForm.get('password'); }
}
