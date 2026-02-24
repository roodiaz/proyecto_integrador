import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MaterialModule } from '../../../../shared/material.module';

// Credenciales quemadas
const HARDCODED_CREDENTIALS = {
  email: 'admin@gmail.com',
  password: 'admin'
};

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
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(4)]]
    });
  }

  onSubmit() {
    if (this.loginForm.valid) {
      const { email, password } = this.loginForm.value;
      
      if (email === HARDCODED_CREDENTIALS.email && password === HARDCODED_CREDENTIALS.password) {
        console.log('Inicio de sesión exitoso');
        this.loginError = null;
        
        // Guardar en localStorage (simulando autenticación)
        localStorage.setItem('isLoggedIn', 'true');
        localStorage.setItem('userEmail', email);
        
        // Redirigir al dashboard
        this.router.navigate(['/dashboard']);
      } else {
        this.loginError = 'Email o contraseña incorrectos';
      }
    }
  }

  onGoogleSignIn() {
    alert('Inicio de sesión con Google estará disponible próximamente');
  }

  get email() { return this.loginForm.get('email'); }
  get password() { return this.loginForm.get('password'); }
}
