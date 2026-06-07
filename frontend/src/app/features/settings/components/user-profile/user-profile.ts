import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';
import { UserService } from '../../services/user.service';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { MaterialModule } from '../../../../shared/material.module';
import { environment } from '../../../../../environments/environment';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';

/**
 * Pantalla de perfil de usuario, dentro de la sección de configuración.
 *
 * Permite ver y editar los datos personales y preferencias del usuario, cambiar
 * la foto de perfil, cambiar la contraseña y eliminar la cuenta. Los datos se
 * cargan al iniciar la pantalla y se guardan a través de `UserService`.
 */
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

  // ── Formularios ──
  profileForm: FormGroup;
  passwordForm: FormGroup;
  hidePassword = true;

  // ── Datos del perfil ──
  profileImageUrl = '';

  // ── Estados de carga ──
  loadingProfile = false;
  savingProfile = false;
  changingPassword = false;
  uploadingImage = false;
  deletingAccount = false;

  /**
   * Crea los formularios reactivos de perfil y de cambio de contraseña, y
   * suscribe la validación cruzada entre `newPassword` y `confirmPassword`
   * para detectar que ambas contraseñas coincidan.
   * @param fb Constructor de formularios reactivos de Angular.
   * @param userService Servicio para obtener y actualizar los datos del usuario.
   * @param notificationService Servicio para mostrar notificaciones al usuario.
   * @param dialog Servicio de diálogos de Angular Material, usado para confirmar la eliminación de cuenta.
   * @param router Router de Angular, usado para redirigir al login tras eliminar la cuenta.
   */
  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private notificationService: SnackBarService,
    private dialog: MatDialog,
    private router: Router
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

  // ── Ciclo de vida ──

  /** Carga los datos del usuario al iniciar la pantalla. */
  ngOnInit(): void {
    this.loadUserData();
  }

  // ── Carga y guardado del perfil ──

  /**
   * Obtiene los datos del usuario autenticado y completa el formulario de
   * perfil y la URL de la imagen de perfil con la información recibida.
   */
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

  /**
   * Valida y envía los cambios del formulario de perfil al servidor, y notifica
   * al usuario el resultado de la operación. No hace nada si ya hay un guardado
   * en curso o si el formulario es inválido.
   */
  onSaveProfile(): void {
    if (this.savingProfile) return;

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

  // ── Cambio de contraseña ──

  /**
   * Valida que las contraseñas nueva y de confirmación coincidan, marcando un
   * error de tipo `passwordMismatch` en el control `confirmPassword` cuando no
   * coinciden. Si ambos campos están vacíos no marca ningún error.
   * @param form Formulario de cambio de contraseña sobre el que se aplica la validación.
   */
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

  /**
   * Valida y envía el formulario de cambio de contraseña al servidor, notifica
   * al usuario el resultado y reinicia el formulario si la operación fue exitosa.
   * No hace nada si ya hay un cambio en curso o si el formulario es inválido.
   */
  onChangePassword(): void {
    if (this.changingPassword) return;

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

  // ── Imagen de perfil ──

  /**
   * Maneja la selección de un archivo desde el input de tipo `file` y lo sube
   * como nueva imagen de perfil. Actualiza `profileImageUrl` con la imagen
   * recibida y limpia el input al finalizar (con éxito o con error).
   * @param event Evento `change` del input de archivo.
   */
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;

    if (!input.files?.length) return;

    const file = input.files[0];

    this.uploadingImage = true;

    this.userService.uploadProfileImage(file)
      .pipe(finalize(() => this.uploadingImage = false))
      .subscribe({
        next: response => {
          this.notificationService.success(response.message);

          const imageUrl = response.data?.profileImageUrl;

          if (imageUrl)
            this.profileImageUrl = environment.serverUrl + imageUrl;

          input.value = '';
        },
        error: error => {
          this.notificationService.error(error.error?.message ?? 'Error al subir imagen');
          input.value = '';
        }
      });
  }

  /** Dispara el selector de archivos oculto al hacer clic sobre el avatar. */
  triggerFileInput(): void {
    const fileInput = document.getElementById('profileImage') as HTMLInputElement;
    fileInput.click();
  }

  // ── Eliminación de cuenta ──

  /**
   * Pide confirmación al usuario antes de eliminar la cuenta (carga el diálogo
   * de confirmación de forma diferida) y, si confirma, ejecuta la eliminación.
   */
  async confirmDeleteAccount(): Promise<void> {
    const ConfirmDialog = await import('../../../../shared/confirm-dialog/confirm-dialog.component');

    const dialogRef = this.dialog.open(ConfirmDialog.ConfirmDialogComponent, {
      width: '430px',
      backdropClass: 'blur-backdrop',
      data: {
        title: 'Eliminar cuenta',
        message: '¿Estás segura de que querés eliminar tu cuenta? Se eliminarán tu usuario, portfolio, operaciones, favoritos, alertas, notificaciones y configuración. Esta acción no se puede deshacer.'
      }
    });

    const result = await dialogRef.afterClosed().toPromise();

    if (!result) return;

    this.deleteAccount();
  }

  /**
   * Elimina la cuenta del usuario a través del servicio correspondiente, limpia
   * el almacenamiento local y de sesión, y redirige al login. Notifica al usuario
   * el resultado de la operación. No hace nada si ya hay una eliminación en curso.
   */
  deleteAccount(): void {
    if (this.deletingAccount) return;

    this.deletingAccount = true;

    this.userService.deleteAccount()
      .pipe(finalize(() => this.deletingAccount = false))
      .subscribe({
        next: response => {
          if (!response.success) {
            this.notificationService.error(response.message || 'No se pudo eliminar la cuenta');
            return;
          }

          this.notificationService.success(response.message || 'Cuenta eliminada correctamente');
          localStorage.clear();
          sessionStorage.clear();
          this.router.navigate(['/login']);
        },
        error: error => {
          this.notificationService.error(error.error?.message ?? 'Error al eliminar la cuenta');
        }
      });
  }
}
