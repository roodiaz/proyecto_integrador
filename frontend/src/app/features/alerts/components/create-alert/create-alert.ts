import { Component, Inject, OnInit, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MaterialModule } from '../../../../shared/material.module';
import { Alert, ALERT_CONDITIONS, CreateAlertDto, UpdateAlertDto } from '../../models/alert.model';
import { AlertService } from '../../services/alert.service';
import { SnackBarService } from '../../../../core/services/snackbar.service';

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
    private alertService: AlertService,
    private snackBarService: SnackBarService,
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
        symbol: this.data.alert.symbol ?? '',
        condition: this.data.alert.condition ?? '<',
        price: this.data.alert.price ?? null,
        percentChange: this.data.alert.percentChange ?? null,
        isActive: this.data.alert.isActive ?? true
      });

    }

    // Actualiza los validadores cuando cambia el tipo de condicion
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

  // Determina si la condicion seleccionada es de tipo porcentual
  isPercentageCondition(): boolean {
    const condition = this.alertForm.get('condition')?.value;
    return condition === '%>' || condition === '%<';
  }

  onSubmit(): void {

    if (!this.alertForm.valid)
      return;

    const formValue = this.alertForm.value;

    const dto: CreateAlertDto = {
      symbol: formValue.symbol.toUpperCase(),
      condition: formValue.condition,
      price: formValue.price,
      percentChange: formValue.percentChange,
      isActive: formValue.isActive
    };

    if (this.isEditMode) {

      const updateDto: UpdateAlertDto = {
        id: this.data.alert!.id,
        ...dto
      };

      this.alertService.update(updateDto)
        .subscribe({
          next: () => {

            this.snackBarService.success(
              'Alerta actualizada correctamente'
            );

            this.dialogRef.close(true);
          },
          error: (error) => {

            this.snackBarService.error(
              error?.error?.message ??
              'Error al actualizar la alerta'
            );
          }
        });

      return;
    }
    else {
      this.alertService.create(dto)
        .subscribe({
          next: () => {

            this.snackBarService.success(
              'Alerta creada correctamente'
            );

            this.dialogRef.close(true);
          },
          error: (error) => {

            this.snackBarService.error(
              error?.error?.message ??
              'Error al crear la alerta'
            );
          }
        });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  getConditionDisplay(condition: string): string {
    const conditionMap: { [key: string]: string } = {
      '>': 'Mayor que',
      '<': 'Menor que',
      '%>': 'Aumento %',
      '%<': 'Disminución %'
    };
    return conditionMap[condition] || condition;
  }
}
