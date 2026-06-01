import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { TextFieldModule } from '@angular/cdk/text-field';
import { MaterialModule } from '../../../../shared/material.module';
import { NotificationService } from '../../../../core/services/notification.service';
import { ContactService } from '../../services/contact.service';

@Component({
  selector: 'app-contact-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TextFieldModule,
    MaterialModule
  ],
  templateUrl: './contact-form.html',
  styleUrls: ['./contact-form.css']
})
export class ContactForm implements OnInit {
  contactForm: FormGroup;

  constructor(
    private fb: FormBuilder,
    private contactService: ContactService,
    private notificationService: NotificationService
  ) {
    this.contactForm = this.fb.group({
      fullName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required, Validators.minLength(6)]],
      consulta: ['', [Validators.required]]
    });
  }

  ngOnInit(): void {
  }

  onSubmit(): void {

    if (!this.contactForm.valid)
      return;

    const request = {
      name: this.contactForm.value.fullName,
      email: this.contactForm.value.email,
      phone: this.contactForm.value.phone,
      message: this.contactForm.value.consulta
    };

    this.contactService.send(request)
      .subscribe({

        next: (response) => {
          this.notificationService.success(response.message);
          this.contactForm.reset({
            fullName: '',
            email: '',
            phone: '',
            consulta: ''
          });
        },

        error: (error) => {
          this.notificationService.error(error.error?.message ?? 'Error al enviar mensaje');
        }
      });
  }

  // Convenience getters for easy access to form fields
  get fullName() { return this.contactForm.get('fullName'); }
  get email() { return this.contactForm.get('email'); }
  get phone() { return this.contactForm.get('phone'); }
  get consulta() { return this.contactForm.get('consulta'); }
}
