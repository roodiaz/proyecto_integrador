import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';
import { userProfileSelectOptions } from '../../models/user-profile.model';
import { UserService } from '../../services/user.service';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { MaterialModule } from '../../../../shared/material.module';
import { environment } from '../../../../../environments/environment';

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
  passwordForm: FormGroup;
  hidePassword = true;

  currencies = userProfileSelectOptions.currencies;
  profileImageUrl = '';

  loadingProfile = false;
  savingProfile = false;
  changingPassword = false;
  uploadingImage = false;

  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private notificationService: SnackBarService
  ) {
    this.profileForm = this.fb.group({
      userName: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      birthDate: [null],
      phone: ['', [Validators.pattern('^[0-9+\\-\\s()]*$')]],
      currency: ['USD', [Validators.required]],
      emailNotifications: [true],
    });

    this.passwordForm = this.fb.group({
      currentPassword: [''],
      newPassword: [''],
      confirmPassword: ['']
    });

    this.passwordForm.get('newPassword')?.valueChanges.subscribe(() => {
      this.passwordForm.get('confirmPassword')?.updateValueAndValidity();
    });

    this.passwordForm.get('confirmPassword')?.valueChanges.subscribe(() => {
      this.passwordMatchValidator(this.passwordForm);
    });
  }

  ngOnInit(): void {
    this.loadUserData();
  }

  loadUserData(): void {
  this.loadingProfile = true;

  this.userService.getProfile()
    .pipe(finalize(() => this.loadingProfile = false))
    .subscribe({
      next: response => {
        if (!response.success || !response.data) return;

        const profile = response.data;

        this.profileForm.patchValue({
          userName: profile.username,
          email: profile.email,
          birthDate: profile.birthDate,
          phone: profile.phone,
          currency: profile.settings.currency,
          emailNotifications: profile.settings.emailNotifications,
        });

        this.profileImageUrl = profile.profileImageUrl ? environment.serverUrl + profile.profileImageUrl : '';
      },
      error: error => {
        console.error('Error cargando perfil de usuario', error);
      }
    });
}
  onSaveProfile(): void {
    if (!this.profileForm.valid) {
      this.notificationService.error('Por favor, completa el formulario correctamente');
      return;
    }

    this.savingProfile = true;

    const request = {
      userName: this.profileForm.value.userName,
      phone: this.profileForm.value.phone,
      birthDate: this.profileForm.value.birthDate,
      currency: this.profileForm.value.currency,
      emailNotifications: this.profileForm.value.emailNotifications
    };

    this.userService.updateProfile(request)
      .pipe(finalize(() => this.savingProfile = false))
      .subscribe({
        next: response => {
          this.notificationService.success(response.message);
        },
        error: error => {
          this.notificationService.error(error.error?.message ?? 'Error al actualizar perfil');
        }
      });
  }

  passwordMatchValidator(form: FormGroup): void {
    const newPassword = form.get('newPassword');
    const confirmPassword = form.get('confirmPassword');

    if (!newPassword?.value && !confirmPassword?.value) {
      confirmPassword?.setErrors(null);
      return;
    }

    if (newPassword?.value !== confirmPassword?.value) {
      confirmPassword?.setErrors({ passwordMismatch: true });
    } else {
      confirmPassword?.setErrors(null);
    }
  }

  onChangePassword(): void {
    if (!this.passwordForm.valid) {
      this.notificationService.error('Formulario inválido');
      return;
    }

    this.changingPassword = true;

    const request = {
      currentPassword: this.passwordForm.value.currentPassword,
      newPassword: this.passwordForm.value.newPassword,
      confirmPassword: this.passwordForm.value.confirmPassword
    };

    this.userService.changePassword(request)
      .pipe(finalize(() => this.changingPassword = false))
      .subscribe({
        next: response => {
          this.notificationService.success(response.message);
          this.passwordForm.reset();
        },
        error: error => {
          this.notificationService.error(error.error?.message ?? 'Error al cambiar contraseña');
        }
      });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;

    if (!input.files?.length) return;

    const file = input.files[0];

    this.uploadingImage = true;

    this.userService.uploadProfileImage(file)
      .pipe(finalize(() => this.uploadingImage = false))
      .subscribe({
        next: (response: any) => {
          this.notificationService.success(response.message);
          this.loadUserData();
          input.value = '';
        },
        error: error => {
          this.notificationService.error(error.error?.message ?? 'Error al subir imagen');
          input.value = '';
        }
      });
  }

  triggerFileInput(): void {
    const fileInput = document.getElementById('profileImage') as HTMLInputElement;
    fileInput.click();
  }
}