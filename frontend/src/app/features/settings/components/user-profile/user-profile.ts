import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';
import { UserService } from '../../services/user.service';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { MaterialModule } from '../../../../shared/material.module';
import { environment } from '../../../../../environments/environment';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { ThemeService, Theme } from '../../../../core/services/theme.service';
import { LanguageService, Language } from '../../../../core/services/language.service';
import { strongPasswordValidator } from '../../../../shared/validators/password-policy.validator';
import { PasswordRequirementsComponent } from '../../../../shared/components/password-requirements/password-requirements.component';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    MaterialModule,
    TranslateModule,
    PasswordRequirementsComponent
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
  currentEmail = '';

  // ── Estados de carga ──
  loadingProfile = false;
  savingProfile = false;
  changingPassword = false;
  uploadingImage = false;
  deletingAccount = false;

  // ── Tema visual ──
  selectedTheme: Theme = 'Dark';

  // ── Idioma ──
  selectedLanguage: Language = 'es';

  // ── Verificación de cambio de email ──
  /** Estados posibles del flujo independiente de verificación de email. */
  emailVerificationStatus: 'idle' | 'sending' | 'codeSent' | 'verifying' | 'verified' | 'error' | 'sendError' = 'idle';
  /** Mensaje de error a mostrar cuando `emailVerificationStatus` es 'error'. */
  emailVerificationError = '';
  /** Código de verificación ingresado por el usuario. */
  emailVerificationCode = '';
  /** Nuevo email pendiente de verificación (al que se envió el código). */
  pendingEmail = '';

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
    private router: Router,
    private themeService: ThemeService,
    private languageService: LanguageService
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
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, strongPasswordValidator()]],
      confirmPassword: ['', [Validators.required]]
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

          this.selectedTheme = (profile.settings.theme as Theme) || 'Dark';
          this.themeService.initialize(profile.settings.theme);
          this.selectedLanguage = (profile.settings.language as Language) || 'es';
          this.languageService.initialize(profile.settings.language);

          this.profileImageUrl = profile.profileImageUrl ? environment.serverUrl + profile.profileImageUrl : '';
          this.currentEmail = profile.email;
          this.resetEmailVerification();
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
      this.notificationService.error(this.languageService.instant('SETTINGS.FORM_ERRORS.FORM_INVALID'));
      return;
    }

    this.savingProfile = true;

    const request = {
      userName: this.profileForm.value.userName,
      phone: this.profileForm.value.phone,
      birthDate: this.profileForm.value.birthDate,
      currency: this.profileForm.value.currency,
      emailNotifications: this.profileForm.value.emailNotifications,
      theme: this.selectedTheme,
      language: this.selectedLanguage
    };

    this.userService.updateProfile(request)
      .pipe(finalize(() => this.savingProfile = false))
      .subscribe({
        next: response => {
          this.notificationService.fromResponse(response.success, response.code, response.message);
        },
        error: error => {
          this.notificationService.fromResponse(false, error.error?.code, error.error?.message);
        }
      });
  }

  // ── Tema visual ──

  /** Cambia el tema, lo aplica de inmediato y lo persiste en el backend. */
  onThemeChange(theme: Theme): void {
    this.selectedTheme = theme;
    this.themeService.setTheme(theme);
    this.userService.updateProfile({
      userName: this.profileForm.value.userName,
      currency: this.profileForm.value.currency,
      emailNotifications: this.profileForm.value.emailNotifications,
      phone: this.profileForm.value.phone,
      birthDate: this.profileForm.value.birthDate,
      theme,
      language: this.selectedLanguage
    }).subscribe({
      error: (error) => this.notificationService.fromResponse(false, error.error?.code, error.error?.message)
    });
  }

  onLanguageChange(lang: Language): void {
    this.selectedLanguage = lang;
    this.languageService.setLanguage(lang);
    this.userService.updateProfile({
      userName: this.profileForm.value.userName,
      currency: this.profileForm.value.currency,
      emailNotifications: this.profileForm.value.emailNotifications,
      phone: this.profileForm.value.phone,
      birthDate: this.profileForm.value.birthDate,
      theme: this.selectedTheme,
      language: lang
    }).subscribe({
      error: (error) => this.notificationService.fromResponse(false, error.error?.code, error.error?.message)
    });
  }

  // ── Verificación de cambio de email ──

  /**
   * Indica si el email ingresado en el formulario difiere del actual y tiene formato válido,
   * condición necesaria para habilitar el botón "Verificar Email".
   */
  get canRequestEmailChange(): boolean {
    const control = this.profileForm.get('email');
    const newEmail = (control?.value ?? '').trim();

    return !!newEmail
      && !control?.hasError('email')
      && newEmail.toLowerCase() !== this.currentEmail.toLowerCase();
  }

  /** Reinicia el flujo de verificación de email a su estado inicial. */
  private resetEmailVerification(): void {
    this.emailVerificationStatus = 'idle';
    this.emailVerificationError = '';
    this.emailVerificationCode = '';
    this.pendingEmail = '';
  }

  /**
   * Inicia la verificación del nuevo email ingresado: el backend valida el formato, que no
   * pertenezca a otro usuario y envía un código de verificación a esa dirección. El email
   * actual del usuario no se modifica en este paso.
   */
  onVerifyEmail(): void {
    if (!this.canRequestEmailChange || this.emailVerificationStatus === 'sending') return;

    const newEmail = (this.profileForm.value.email ?? '').trim();

    this.emailVerificationStatus = 'sending';
    this.emailVerificationError = '';

    this.userService.requestEmailChange({ newEmail })
      .subscribe({
        next: response => {
          if (!response.success) {
            this.emailVerificationStatus = 'sendError';
            this.emailVerificationError = response.message || this.languageService.instant('SETTINGS.EMAIL_VERIFY.SEND_ERROR');
            return;
          }

          this.pendingEmail = newEmail;
          this.emailVerificationCode = '';
          this.emailVerificationStatus = 'codeSent';
          this.notificationService.success(response.message);
        },
        error: error => {
          this.emailVerificationStatus = 'sendError';
          this.emailVerificationError = error.error?.message ?? this.languageService.instant('SETTINGS.EMAIL_VERIFY.SEND_ERROR');
        }
      });
  }

  /**
   * Valida el código de verificación ingresado por el usuario. Si es correcto, el backend
   * actualiza el email del usuario; se refresca la información del perfil y se ocultan los
   * controles de verificación. Si es incorrecto, muestra el error y permite reintentar.
   */
  onValidateEmailCode(): void {
    if (this.emailVerificationStatus === 'verifying' || !this.emailVerificationCode.trim()) return;

    this.emailVerificationStatus = 'verifying';
    this.emailVerificationError = '';

    this.userService.confirmEmailChange({ code: this.emailVerificationCode.trim() })
      .subscribe({
        next: response => {
          if (!response.success) {
            this.emailVerificationStatus = 'error';
            this.emailVerificationError = response.message || this.languageService.instant('SETTINGS.EMAIL_VERIFY.CODE_INVALID');
            return;
          }

          this.notificationService.fromResponse(response.success, response.code, response.message);
          this.emailVerificationStatus = 'verified';
          this.loadUserData();
        },
        error: error => {
          this.emailVerificationStatus = 'error';
          this.emailVerificationError = error.error?.message ?? this.languageService.instant('SETTINGS.EMAIL_VERIFY.CODE_INVALID');
        }
      });
  }

  /** Permite reintentar el ingreso del código tras un error, sin perder el email pendiente. */
  retryEmailCode(): void {
    this.emailVerificationCode = '';
    this.emailVerificationStatus = 'codeSent';
    this.emailVerificationError = '';
  }

  /** Cancela el flujo de verificación en curso y restaura el email actual en el formulario. */
  cancelEmailVerification(): void {
    this.profileForm.get('email')?.setValue(this.currentEmail);
    this.resetEmailVerification();
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

    if (!confirmPassword) return;

    if (newPassword?.value !== confirmPassword.value) {
      confirmPassword.setErrors({ ...confirmPassword.errors, passwordMismatch: true });
      return;
    }

    if (!confirmPassword.errors) return;

    const { passwordMismatch, ...rest } = confirmPassword.errors;
    confirmPassword.setErrors(Object.keys(rest).length ? rest : null);
  }

  /**
   * Valida y envía el formulario de cambio de contraseña al servidor, notifica
   * al usuario el resultado y reinicia el formulario si la operación fue exitosa.
   * No hace nada si ya hay un cambio en curso o si el formulario es inválido.
   */
  onChangePassword(): void {
    if (this.changingPassword) return;

    if (!this.passwordForm.valid) {
      this.passwordForm.markAllAsTouched();
      this.notificationService.error(this.languageService.instant('SETTINGS.FORM_ERRORS.FORM_INVALID_PASSWORD'));
      return;
    }

    const current = this.passwordForm.value.currentPassword;
    const newPwd = this.passwordForm.value.newPassword;
    if (current && newPwd && current === newPwd) {
      this.notificationService.error(this.languageService.instant('SETTINGS.FORM_ERRORS.PASSWORD_SAME_AS_CURRENT'));
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
          this.notificationService.fromResponse(response.success, response.code, response.message);
          this.passwordForm.reset();
        },
        error: error => {
          this.notificationService.fromResponse(false, error.error?.code, error.error?.message);
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
          this.notificationService.fromResponse(response.success, response.code, response.message);

          const imageUrl = response.data?.profileImageUrl;

          if (imageUrl)
            this.profileImageUrl = environment.serverUrl + imageUrl;

          input.value = '';
        },
        error: error => {
          this.notificationService.fromResponse(false, error.error?.code, error.error?.message);
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
        title: this.languageService.instant('SETTINGS.DANGER_ZONE.CONFIRM_TITLE'),
        message: this.languageService.instant('SETTINGS.DANGER_ZONE.CONFIRM_MSG')
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
          this.notificationService.fromResponse(response.success, response.code, response.message);
          if (!response.success) return;
          localStorage.clear();
          sessionStorage.clear();
          this.router.navigate(['/landing']);
        },
        error: error => {
          this.notificationService.fromResponse(false, error.error?.code, error.error?.message);
        }
      });
  }
}
