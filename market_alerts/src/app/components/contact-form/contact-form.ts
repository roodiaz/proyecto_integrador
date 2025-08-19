import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { CommonModule } from '@angular/common';
import { TextFieldModule } from '@angular/cdk/text-field';

@Component({
  selector: 'app-contact-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule, 
    MatFormFieldModule, 
    MatInputModule, 
    MatIconModule, 
    MatButtonModule,
    TextFieldModule
  ],
  templateUrl: './contact-form.html',
  styleUrls: ['./contact-form.css']
})
export class ContactForm implements OnInit {
  contactForm: FormGroup;

  constructor(private fb: FormBuilder) {
    this.contactForm = this.fb.group({
      fullName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required, Validators.minLength(6)]],
      consulta: ['', [Validators.required]]
    });
  }

  ngOnInit(): void {
    // Any additional initialization can go here
  }

  onSubmit() {
    if (this.contactForm.valid) {
      console.log('Form submitted:', this.contactForm.value);
      // Here you would typically send the form data to your backend service
      // For example:
      // this.contactService.sendContactForm(this.contactForm.value).subscribe(...);
    }
  }

  // Convenience getters for easy access to form fields
  get fullName() { return this.contactForm.get('fullName'); }
  get email() { return this.contactForm.get('email'); }
  get phone() { return this.contactForm.get('phone'); }
  get consulta() { return this.contactForm.get('consulta'); }
}
