import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MaterialModule } from '../../../../shared/material.module';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-register-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MaterialModule,
    RouterModule
  ],
  templateUrl: './register-form.html',
  styleUrl: './register-form.css'
})
export class RegisterForm {
  registerForm: FormGroup;
  hidePassword = true;
  hideConfirmPassword = true;

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

  // Validador personalizado para verificar que las contraseñas coincidan
  passwordMatchValidator(form: FormGroup) {
    const password = form.get('password')?.value;
    const confirmPassword = form.get('confirmPassword')?.value;
    
    if (password !== confirmPassword) {
      form.get('confirmPassword')?.setErrors({ mismatch: true });
    } else {
      form.get('confirmPassword')?.setErrors(null);
    }
    
    return null;
  }

  // Getters para acceder a los controles del formulario
  get fullName() {
    return this.registerForm.get('fullName');
  }

  get email() {
    return this.registerForm.get('email');
  }

  get password() {
    return this.registerForm.get('password');
  }

  get confirmPassword() {
    return this.registerForm.get('confirmPassword');
  }

  onSubmit() {
    if (this.registerForm.valid) {
      // Aquí iría la lógica para registrar al usuario
      console.log('Formulario válido:', this.registerForm.value);
      // Ejemplo de redirección después del registro exitoso
      // this.router.navigate(['/dashboard']);
    } else {
      // Marcar todos los campos como tocados para mostrar los errores
      this.registerForm.markAllAsTouched();
    }
  }
}
