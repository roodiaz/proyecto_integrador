import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CommonModule } from '@angular/common';
import { MaterialModule } from '../../shared/material.module';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MaterialModule
  ],
  templateUrl: './user-profile.html',
  styleUrls: ['./user-profile.css']
})
export class UserProfile implements OnInit {
  profileForm: FormGroup;
  hidePassword = true;
  
  // Opciones para los selects
  currencies = [
    { value: 'USD', viewValue: 'Dólar Estadounidense (USD)' },
    { value: 'ARS', viewValue: 'Peso Argentino (ARS)' }
  ];
  
  themes = [
    { value: 'light', viewValue: 'Claro' },
    { value: 'dark', viewValue: 'Oscuro' },
    { value: 'system', viewValue: 'Sistema' }
  ];
  
  updateIntervals = [
    { value: 1, viewValue: 'Cada minuto' },
    { value: 5, viewValue: 'Cada 5 minutos' },
    { value: 15, viewValue: 'Cada 15 minutos' }
  ];

  constructor(
    private fb: FormBuilder,
    private snackBar: MatSnackBar
  ) {
    this.profileForm = this.fb.group({
      // Información Personal
      fullName: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      birthDate: [null],
      phone: ['', [Validators.pattern('^[0-9+\-\s()]*$')]],
      
      // Preferencias
      currency: ['USD', [Validators.required]],
      theme: ['system', [Validators.required]],
      updateInterval: [5, [Validators.required, Validators.min(1)]],
      
      // Notificaciones
      emailNotifications: [true],
      pushNotifications: [true],
      
      // Seguridad
      currentPassword: [''],
      newPassword: [''],
      confirmPassword: ['']
    });

    // Suscribirse a los cambios en los campos de contraseña para aplicar la validación dinámicamente
    this.profileForm.get('newPassword')?.valueChanges.subscribe(() => {
      this.profileForm.get('confirmPassword')?.updateValueAndValidity();
    });

    this.profileForm.get('confirmPassword')?.valueChanges.subscribe(() => {
      this.passwordMatchValidator(this.profileForm);
    });
  }

  ngOnInit(): void {
    // Cargar datos del usuario actual
    this.loadUserData();
  }

  loadUserData(): void {
    // Aquí iría la lógica para cargar los datos del usuario
    // Por ahora, datos de ejemplo
    const userData = {
      fullName: 'Juan Pérez',
      email: 'juan.perez@ejemplo.com',
      birthDate: new Date(1990, 0, 1),
      phone: '+54 11 1234-5678',
      currency: 'USD',
      theme: 'system',
      updateInterval: 5,
      emailNotifications: true,
      pushNotifications: true
    };
    
    this.profileForm.patchValue(userData);
  }

  passwordMatchValidator(form: FormGroup) {
    const newPassword = form.get('newPassword');
    const confirmPassword = form.get('confirmPassword');

    // Si ambos campos están vacíos o no han sido tocados, no hay error
    if (!newPassword?.value && !confirmPassword?.value) {
      confirmPassword?.setErrors(null);
      return;
    }

    // Si las contraseñas no coinciden, establecer error
    if (newPassword?.value !== confirmPassword?.value) {
      confirmPassword?.setErrors({ passwordMismatch: true });
    } else {
      confirmPassword?.setErrors(null);
    }
  }

  onSubmit(): void {
    if (this.profileForm.valid) {
      // Aquí iría la lógica para guardar los cambios
      console.log('Datos del formulario:', this.profileForm.value);
      
      this.snackBar.open('Cambios guardados correctamente', 'Cerrar', {
        duration: 3000,
        panelClass: ['success-snackbar']
      });
    } else {
      this.snackBar.open('Por favor, completa el formulario correctamente', 'Cerrar', {
        duration: 3000,
        panelClass: ['error-snackbar']
      });
    }
  }

  onFileSelected(event: any): void {
    const file: File = event.target.files[0];
    if (file) {
      // Aquí iría la lógica para subir la imagen
      console.log('Archivo seleccionado:', file.name);
    }
  }

  triggerFileInput(): void {
    const fileInput = document.getElementById('profileImage') as HTMLInputElement;
    fileInput.click();
  }
}
