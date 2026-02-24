import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MaterialModule } from '../../../../shared/material.module';
import { Router, RouterModule } from '@angular/router';
import { PlanType, RegisterFormData, PaymentInfo } from '../../models/register.model';
import { PaymentForm } from '../payment-form/payment-form';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PlanSelection, PlanSelectionConfig } from '../plan-selection/plan-selection';

// Extend PaymentInfo interface to include nextBillingDate
declare module '../../models/register.model' {
  interface PaymentInfo {
    nextBillingDate?: string;
  }
}

@Component({
  selector: 'app-register-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MaterialModule,
    RouterModule,
    PaymentForm,
    MatProgressSpinnerModule
  ],
  templateUrl: './register-form.html',
  styleUrl: './register-form.css'
})
export class RegisterForm {
  // Make PlanType available in template
  PlanType = PlanType;
  registerForm: FormGroup;
  hidePassword = true;
  hideConfirmPassword = true;
  
  // Registration steps
  currentStep = 1;
  totalSteps = 3;
  selectedPlan: PlanType = PlanType.FREE;
  paymentInfo: PaymentInfo | null = null;
  
  // UI states
  isLoading = false;
  errorMessage: string | null = null;
  
  formData: RegisterFormData = {
    fullName: '',
    email: '',
    password: '',
    confirmPassword: '',
    planType: PlanType.FREE,
    paymentInfo: undefined
  };

  private dialog = inject(MatDialog);
  
  constructor(
    private fb: FormBuilder,
    private router: Router
  ) {
    this.registerForm = this.fb.group({
      fullName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    }, { validator: this.passwordMatchValidator });
  }

  // Getter for easy access to form fields
  get f() { return this.registerForm.controls; }
  get fullName() { return this.registerForm.get('fullName'); }
  get email() { return this.registerForm.get('email'); }
  get password() { return this.registerForm.get('password'); }
  get confirmPassword() { return this.registerForm.get('confirmPassword'); }

  // Validador personalizado para verificar que las contraseñas coincidan
  private passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');
    
    if (!password || !confirmPassword) {
      return null;
    }
    
    return password.value === confirmPassword.value ? null : { mismatch: true };
  }

  onFormSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }
    
    // Save form data and move to plan selection
    this.formData = {
      ...this.formData,
      ...this.registerForm.value,
      planType: this.selectedPlan
    };
    
    this.openPlanSelection();
  }

  openPlanSelection(): void {
    const dialogRef = this.dialog.open(PlanSelection, {
      width: 'auto',
      minWidth: '300px',
      maxWidth: '95vw',
      maxHeight: '95vh',
      panelClass: 'plan-selection-dialog',
      disableClose: true,
      autoFocus: false,
      hasBackdrop: true,
      backdropClass: 'plan-selection-backdrop',
      data: { 
        currentPlan: this.selectedPlan,
        isModal: true,
        showBackButton: false,
        showComparison: false
      } as PlanSelectionConfig
    });

    dialogRef.afterClosed().subscribe((planType: PlanType | undefined) => {
      if (planType !== undefined) {
        this.selectedPlan = planType;
        this.formData.planType = planType;
        
        // Set next billing date for premium plan
        if (planType === PlanType.PREMIUM) {
          const nextDate = new Date();
          nextDate.setMonth(nextDate.getMonth() + 1);
          
          // Initialize payment info with default values if it doesn't exist
          const paymentInfo: PaymentInfo = this.formData.paymentInfo || {
            cardNumber: '',
            cardHolder: '',
            expiryMonth: 0,
            expiryYear: 0,
            cvv: '',
            nextBillingDate: nextDate.toLocaleDateString('es-AR')
          };
          paymentInfo.nextBillingDate = nextDate.toLocaleDateString('es-AR');
          this.formData.paymentInfo = paymentInfo;
          
          // Move to payment step if not already there
          if (this.currentStep < 3) {
            this.currentStep = 3;
          }
        } else {
          // If switching to free plan, clear payment info
          this.formData.paymentInfo = undefined;
        }
      }
    });
  }
  
  onPaymentSubmitted(paymentInfo: PaymentInfo): void {
    this.paymentInfo = paymentInfo;
    this.completeRegistration();
  }
  
  onBackToPlanSelection(): void {
    this.previousStep();
  }
  
  private completeRegistration(): void {
    this.isLoading = true;
    this.errorMessage = null;
    
    const formData: RegisterFormData = {
      ...this.registerForm.value,
      planType: this.selectedPlan,
      paymentInfo: this.paymentInfo || undefined
    };
    
    console.log('Registration data:', formData);
    
    // Simulate API call
    setTimeout(() => {
      try {
        // TODO: Replace with actual API call
        // this.authService.register(formData).subscribe({
        //   next: (response) => {
        //     this.isLoading = false;
        //     this.router.navigate(['/dashboard']);
        //   },
        //   error: (error) => {
        //     this.isLoading = false;
        //     this.handleRegistrationError(error);
        //   }
        // });
        
        // For demo purposes, simulate successful registration
        this.isLoading = false;
        this.router.navigate(['/dashboard']);
      } catch (error) {
        this.isLoading = false;
        this.handleRegistrationError(error);
      }
    }, 1000);
  }
  
  private handleRegistrationError(error: any): void {
    console.error('Registration error:', error);
    
    if (error.status === 409) {
      this.errorMessage = 'El correo electrónico ya está en uso. Por favor, utiliza otro correo.';
    } else if (error.status === 400) {
      this.errorMessage = 'Datos de registro inválidos. Por favor, verifica la información.';
    } else {
      this.errorMessage = 'Ocurrió un error al registrar la cuenta. Por favor, inténtalo de nuevo más tarde.';
    }
    
    // Scroll to the top to show the error message
    window.scrollTo(0, 0);
  }
  
  nextStep(): void {
    if (this.currentStep < this.totalSteps) {
      this.currentStep++;
      window.scrollTo(0, 0);
    }
  }
  
  previousStep(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
      window.scrollTo(0, 0);
    }
  }
  
}
