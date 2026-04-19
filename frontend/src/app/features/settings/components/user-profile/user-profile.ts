import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CommonModule } from '@angular/common';
import { MatDialog } from '@angular/material/dialog';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDividerModule } from '@angular/material/divider';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { 
  mockUserProfileData, 
  userProfileSelectOptions 
} from '../../models/user-profile.model';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatButtonModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatDividerModule,
    MatSlideToggleModule
  ],
  templateUrl: './user-profile.html',
  styleUrls: ['./user-profile.css']
})
export class UserProfile implements OnInit {
  profileForm: FormGroup;
  hidePassword = true;
  
  // Opciones para los selects
  currencies = userProfileSelectOptions.currencies;

  constructor(
    private fb: FormBuilder,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) {
    this.profileForm = this.fb.group({
      // Información Personal
      fullName: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      birthDate: [null],
      phone: ['', [Validators.pattern('^[0-9+\-\s()]*$')]],
      
      // Preferencias
      currency: ['USD', [Validators.required]],
      
      // Notificaciones
      emailNotifications: [true],
      
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
    // Usamos los datos de ejemplo del modelo
    this.profileForm.patchValue({
      ...mockUserProfileData,
      currentPassword: '',
      newPassword: '',
      confirmPassword: ''
    });
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
