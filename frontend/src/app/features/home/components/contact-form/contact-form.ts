import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { TextFieldModule } from '@angular/cdk/text-field';
import { TranslateModule } from '@ngx-translate/core';
import { MaterialModule } from '../../../../shared/material.module';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { ContactService } from '../../services/contact.service';
import { LanguageService } from '../../../../core/services/language.service';

@Component({
  selector: 'app-contact-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TextFieldModule,
    MaterialModule,
    TranslateModule
  ],
  templateUrl: './contact-form.html',
  styleUrl: './contact-form.css'
})
export class ContactForm {

  // ── Formulario ─────────────────────────────────────────────────────────────
  /** Formulario reactivo con los campos de la consulta de contacto. */
  contactForm: FormGroup;

  constructor(
    private fb: FormBuilder,
    private contactService: ContactService,
    private notificationService: SnackBarService,
    private languageService: LanguageService
  ) {
    this.contactForm = this.fb.group({
      fullName: ['', [Validators.required]],
      email:    ['', [Validators.required, Validators.email]],
      phone:    ['', [Validators.required, Validators.minLength(6)]],
      consulta: ['', [Validators.required]]
    });
  }

  // ── Getters de acceso rápido ───────────────────────────────────────────────

  /** Referencia al control `fullName` para acceder a sus errores de validación en el template. */
  get fullName() { return this.contactForm.get('fullName'); }

  /** Referencia al control `email` para acceder a sus errores de validación en el template. */
  get email()    { return this.contactForm.get('email'); }

  /** Referencia al control `phone` para acceder a sus errores de validación en el template. */
  get phone()    { return this.contactForm.get('phone'); }

  /** Referencia al control `consulta` para acceder a sus errores de validación en el template. */
  get consulta() { return this.contactForm.get('consulta'); }

  // ── Acciones ───────────────────────────────────────────────────────────────

  /**
   * Valida el formulario y envía la consulta al servicio de contacto.
   * Si el formulario es inválido no realiza ninguna acción.
   * Si el envío es exitoso muestra un snackbar de confirmación y resetea el formulario.
   * En caso de error muestra el mensaje recibido del backend.
   */
  onSubmit(): void {
    if (!this.contactForm.valid) return;

    const request = {
      name:    this.contactForm.value.fullName,
      email:   this.contactForm.value.email,
      phone:   this.contactForm.value.phone,
      message: this.contactForm.value.consulta
    };

    this.contactService.send(request).subscribe({
      next: response => {
        this.notificationService.success(response.message);
        this.contactForm.reset({ fullName: '', email: '', phone: '', consulta: '' });
      },
      error: error => {
        this.notificationService.fromResponse(false, error.error?.code, error.error?.message ?? this.languageService.instant('HOME.CONTACT.SEND_ERROR'));
      }
    });
  }
}
