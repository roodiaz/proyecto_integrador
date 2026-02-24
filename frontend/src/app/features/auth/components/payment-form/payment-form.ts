import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PaymentInfo } from '../../models/register.model';

@Component({
  selector: 'app-payment-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './payment-form.html',
  styleUrl: './payment-form.css'
})
export class PaymentForm {
  @Input() isLoading = false;
  @Output() paymentSubmitted = new EventEmitter<PaymentInfo>();
  @Output() back = new EventEmitter<void>();
  
  paymentForm: FormGroup;
  currentYear = new Date().getFullYear();
  currentMonth = new Date().getMonth() + 1; // 1-12
  
  constructor(private fb: FormBuilder) {
    this.paymentForm = this.fb.group({
      cardNumber: ['', [Validators.required, Validators.pattern('^[0-9]{16}$')]],
      cardHolder: ['', [Validators.required, Validators.minLength(3)]],
      expiryDate: ['', [Validators.required, this.validateExpiryDate]],
      cvv: ['', [Validators.required, Validators.pattern('^[0-9]{3,4}$')]]
    });
  }
  
  validateExpiryDate(control: AbstractControl): ValidationErrors | null {
    if (!control.value) return null;
    
    const [month, year] = control.value.split('/');
    if (!month || !year) return { invalidFormat: true };
    
    const expiryMonth = parseInt(month, 10);
    const expiryYear = 2000 + parseInt(year, 10);
    
    if (isNaN(expiryMonth) || isNaN(expiryYear) || 
        expiryMonth < 1 || expiryMonth > 12) {
      return { invalidDate: true };
    }
    
    const expiryDate = new Date(expiryYear, expiryMonth, 1);
    const currentDate = new Date();
    currentDate.setHours(0, 0, 0, 0);
    
    // Set to first day of next month for comparison
    const firstDayNextMonth = new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 1);
    
    return expiryDate >= firstDayNextMonth ? null : { expired: true };
  }
  
  onSubmit(): void {
    if (this.paymentForm.invalid || this.isLoading) {
      this.paymentForm.markAllAsTouched();
      return;
    }
    
    const [expiryMonth, expiryYear] = this.paymentForm.value.expiryDate.split('/');
    
    const paymentInfo: PaymentInfo = {
      cardNumber: this.paymentForm.value.cardNumber,
      cardHolder: this.paymentForm.value.cardHolder,
      expiryMonth: parseInt(expiryMonth, 10),
      expiryYear: 2000 + parseInt(expiryYear, 10),
      cvv: this.paymentForm.value.cvv
    };
    
    this.paymentSubmitted.emit(paymentInfo);
  }
  
  formatCardNumber(event: Event): void {
    const input = event.target as HTMLInputElement;
    let value = input.value.replace(/\s+/g, '').replace(/[^0-9]/gi, '');
    
    if (value.length > 16) {
      value = value.substring(0, 16);
    }
    
    // Add space every 4 digits
    value = value.replace(/(\d{4})(?=\d)/g, '$1 ').trim();
    
    // Update the input value
    input.value = value;
    this.paymentForm.get('cardNumber')?.setValue(value.replace(/\s+/g, ''), { emitEvent: false });
  }
  
  formatExpiryDate(event: Event): void {
    const input = event.target as HTMLInputElement;
    let value = input.value.replace(/\s+/g, '').replace(/[^0-9]/gi, '');
    
    if (value.length > 4) {
      value = value.substring(0, 4);
    }
    
    // Add slash after MM
    if (value.length > 2) {
      value = value.replace(/(\d{2})(?=\d{2})/, '$1/');
    }
    
    // Update the input value
    input.value = value;
    this.paymentForm.get('expiryDate')?.setValue(value, { emitEvent: false });
  }
  
  onBack(): void {
    this.back.emit();
  }
}
