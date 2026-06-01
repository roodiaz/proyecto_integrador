import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MaterialModule } from '../../../../shared/material.module';
import { AuthService } from '../../services/auth.service';
import {
  LoginRequest,
  LoginResponse
} from '../../models/login.model';

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
  loginForm: FormGroup;
  hidePassword = true;
  loginError: string | null = null;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private authService: AuthService
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(4)]]
    });
  }

  onSubmit(): void {

    if (!this.loginForm.valid) {
      return;
    }

    this.loginError = null;

    const request: LoginRequest = {
      email: this.loginForm.value.email,
      password: this.loginForm.value.password
    };

    this.authService.login(request)
      .subscribe({
        next: (response) => {

          if (!response.success || !response.data) {
            this.loginError = response.message;
            return;
          }

          sessionStorage.setItem('accessToken', response.data.tokens.accessToken);
          sessionStorage.setItem('refreshToken', response.data.tokens.refreshToken);
          sessionStorage.setItem('userEmail', request.email);

          this.router.navigate(['/dashboard']);
        },
        error: (error) => {

          this.loginError =
            error.error?.message ??
            'Error al iniciar sesión';
        }
      });
  }

  onGoogleSignIn() {
    alert('Inicio de sesión con Google estará disponible próximamente');
  }

  get email() { return this.loginForm.get('email'); }
  get password() { return this.loginForm.get('password'); }
}
