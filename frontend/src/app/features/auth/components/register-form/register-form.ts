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

  // Formulario y campos
  registerForm: FormGroup;
  hidePassword = true;
  hideConfirmPassword = true;

  // Estado de la vista
  currentStep = 1;
  isLoading = false;
  errorMessage: string | null = null;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private authService: AuthService
  ) {
    this.registerForm = this.fb.group({
      fullName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.pattern(/^[+]?[\d\s\-\(\)]+$/)]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    }, { validator: this.passwordMatchValidator });
  }

  // Accesos rapidos a los controles
  get f() { return this.registerForm.controls; }
  get fullName() { return this.registerForm.get('fullName'); }
  get email() { return this.registerForm.get('email'); }
  get phone() { return this.registerForm.get('phone'); }
  get password() { return this.registerForm.get('password'); }
  get confirmPassword() { return this.registerForm.get('confirmPassword'); }

  onFormSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = null;

    const request: RegisterRequest = { ...this.registerForm.value };

    this.authService.register(request).subscribe({
      next: (response) => {
        this.isLoading = false;

        localStorage.setItem('registrationEmail', response.data!.email);
        localStorage.setItem('emailSent', response.data!.emailSent.toString());

        this.router.navigate(['/registration-success']);
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error.error?.message ?? 'Ocurrio un error al registrarse';
      }
    });
  }

  private passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');

    if (!password || !confirmPassword) return null;

    return password.value === confirmPassword.value ? null : { mismatch: true };
  }
}
