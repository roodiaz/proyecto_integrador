import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MaterialModule } from '../../../../shared/material.module';
import { Router, RouterModule } from '@angular/router';
import { RegisterFormData } from '../../models/register.model';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

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
  registerForm: FormGroup;
  hidePassword = true;
  hideConfirmPassword = true;
  
  // Registration steps
  currentStep = 1;
  totalSteps = 1;
  
  // UI states
  isLoading = false;
  errorMessage: string | null = null;
  
  formData: RegisterFormData = {
    fullName: '',
    email: '',
    password: '',
    confirmPassword: ''
  };

  
  constructor(
    private fb: FormBuilder,
    private router: Router
  ) {
    this.registerForm = this.fb.group({
      fullName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    }, { validator: this.passwordMatchValidator });
  }

  // Getter for easy access to form fields
  get f() { return this.registerForm.controls; }
  get fullName() { return this.registerForm.get('fullName'); }
  get email() { return this.registerForm.get('email'); }
  get password() { return this.registerForm.get('password'); }
  get confirmPassword() { return this.registerForm.get('confirmPassword'); }

  // Validador personalizado para verificar que las contraseñas coincidan
  private passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');
    
    if (!password || !confirmPassword) {
      return null;
    }
    
    return password.value === confirmPassword.value ? null : { mismatch: true };
  }

  onFormSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }
    
    // Save form data and complete registration
    this.formData = {
      ...this.formData,
      ...this.registerForm.value
    };
    
    this.completeRegistration();
  }

  
  private completeRegistration(): void {
    this.isLoading = true;
    this.errorMessage = null;
    
    const formData: RegisterFormData = {
      ...this.registerForm.value
    };
    
    console.log('Registration data:', formData);
    
    // Simulate API call
    setTimeout(() => {
      try {
        // TODO: Replace with actual API call
        // this.authService.register(formData).subscribe({
        //   next: (response) => {
        //     this.isLoading = false;
        //     this.router.navigate(['/dashboard']);
        //   },
        //   error: (error) => {
        //     this.isLoading = false;
        //     this.handleRegistrationError(error);
        //   }
        // });
        
        // For demo purposes, simulate successful registration
        this.isLoading = false;
        
        // Store email in localStorage for success screen
        localStorage.setItem('registrationEmail', formData.email);
        
        // Navigate to registration success screen
        this.router.navigate(['/registration-success']);
      } catch (error) {
        this.isLoading = false;
        this.handleRegistrationError(error);
      }
    }, 1000);
  }
  
  private handleRegistrationError(error: any): void {
    console.error('Registration error:', error);
    
    if (error.status === 409) {
      this.errorMessage = 'El correo electrónico ya está en uso. Por favor, utiliza otro correo.';
    } else if (error.status === 400) {
      this.errorMessage = 'Datos de registro inválidos. Por favor, verifica la información.';
    } else {
      this.errorMessage = 'Ocurrió un error al registrar la cuenta. Por favor, inténtalo de nuevo más tarde.';
    }
    
    // Scroll to the top to show the error message
    window.scrollTo(0, 0);
  }
  
  
}
