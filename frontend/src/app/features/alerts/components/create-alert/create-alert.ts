import { Component, Inject, OnInit, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MaterialModule } from '../../../../shared/material.module';
import { Alert, ALERT_CONDITIONS } from '../../models/alert.model';

type AlertFormData = Omit<Alert, 'id' | 'userId' | 'createdAt' | 'updatedAt'>;

@Component({
  selector: 'app-create-alert',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MaterialModule
  ],
  templateUrl: './create-alert.html',
  styleUrls: ['./create-alert.css'],
  encapsulation: ViewEncapsulation.None
})
export class CreateAlertComponent implements OnInit {
  alertForm: FormGroup;
  conditions = ALERT_CONDITIONS;
  isEditMode = false;
  title = 'Nueva Alerta';

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<CreateAlertComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { alert?: Alert }
  ) {
    this.alertForm = this.fb.group({
      symbol: ['', [Validators.required, Validators.pattern('^[A-Z]{1,5}$')]],
      condition: ['<', Validators.required],
      price: [null],
      percentChange: [null],
      isActive: [true]
    });

    if (data?.alert) {
      this.isEditMode = true;
      this.title = 'Editar Alerta';
    }
  }

  ngOnInit(): void {
    if (this.data?.alert) {
      this.alertForm.patchValue({
        symbol: this.data.alert.symbol,
        condition: this.data.alert.condition,
        price: this.data.alert.price || null,
        percentChange: this.data.alert.percentChange || null,
        isActive: this.data.alert.isActive
      });
    }

    // Update validators when condition changes
    this.alertForm.get('condition')?.valueChanges.subscribe(condition => {
      const priceControl = this.alertForm.get('price');
      const percentControl = this.alertForm.get('percentChange');
      
      if (condition === '%>' || condition === '%<') {
        priceControl?.clearValidators();
        priceControl?.setValue(null);
        percentControl?.setValidators([
          Validators.required, 
          Validators.min(0.01), 
          Validators.max(100)
        ]);
      } else {
        percentControl?.clearValidators();
        percentControl?.setValue(null);
        priceControl?.setValidators([
          Validators.required, 
          Validators.min(0.01)
        ]);
      }
      
      priceControl?.updateValueAndValidity();
      percentControl?.updateValueAndValidity();
    });
  }

  // Check if the current condition is a percentage-based condition
  isPercentageCondition(): boolean {
    const condition = this.alertForm.get('condition')?.value;
    return condition === '%>' || condition === '%<';
  }

  onSubmit(): void {
    if (this.alertForm.valid) {
      const formValue = this.alertForm.value;
      
      const alertData: AlertFormData = {
        symbol: formValue.symbol.toUpperCase(),
        condition: formValue.condition,
        price: formValue.price,
        percentChange: formValue.percentChange,
        isActive: formValue.isActive
      };

      this.dialogRef.close(alertData);
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  getConditionDisplay(condition: string): string {
    const conditionMap: {[key: string]: string} = {
      '>': 'Mayor que',
      '<': 'Menor que',
      '%>': 'Aumento %',
      '%<': 'Disminución %'
    };
    return conditionMap[condition] || condition;
  }
}
