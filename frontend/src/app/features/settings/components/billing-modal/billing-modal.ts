import { Component, Inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';

export interface BillingData {
  cardNumber: string;
  cardName: string;
  expiryDate: string;
  cvv: string;
  address: string;
  city: string;
  country: string;
  postalCode: string;
}

@Component({
  selector: 'app-billing-modal',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatButtonModule,
    MatSelectModule
  ],
  templateUrl: './billing-modal.html',
  styleUrls: ['./billing-modal.css']
})
export class BillingModalComponent implements OnInit {
  billingForm: FormGroup;

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<BillingModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: Partial<BillingData>
  ) {
    this.billingForm = this.fb.group({
      cardNumber: ['', [Validators.required, Validators.pattern('^[0-9]{16}$')]],
      cardName: ['', [Validators.required, Validators.minLength(3)]],
      expiryDate: ['', [Validators.required, Validators.pattern('^(0[1-9]|1[0-2])\/[0-9]{2}$')]],
      cvv: ['', [Validators.required, Validators.pattern('^[0-9]{3,4}$')]],
      address: ['', [Validators.required]],
      city: ['', [Validators.required]],
      country: ['', [Validators.required]],
      postalCode: ['', [Validators.required]]
    });
  }

  ngOnInit(): void {
    if (this.data) {
      this.billingForm.patchValue(this.data);
    }
  }

  onSubmit(): void {
    if (this.billingForm.valid) {
      this.dialogRef.close(this.billingForm.value);
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  formatCardNumber(event: any): void {
    let value = event.target.value.replace(/\s/g, '');
    let formattedValue = value.match(/.{1,4}/g)?.join(' ') || value;
    this.billingForm.get('cardNumber')?.setValue(formattedValue, { emitEvent: false });
  }

  formatExpiryDate(event: any): void {
    let value = event.target.value.replace(/\D/g, '');
    if (value.length >= 2) {
      value = value.substring(0, 2) + '/' + value.substring(2, 4);
    }
    this.billingForm.get('expiryDate')?.setValue(value, { emitEvent: false });
  }
}
