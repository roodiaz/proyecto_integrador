import { Component, Inject, OnInit, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { MaterialModule } from '../../../../shared/material.module';
import { Alert, ALERT_CONDITIONS, CreateAlertDto, UpdateAlertDto } from '../../models/alert.model';
import { AlertService } from '../../services/alert.service';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { InfoTooltipComponent } from '../../../../shared/components/info-tooltip/info-tooltip.component';

@Component({
  selector: 'app-create-alert',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MaterialModule,
    InfoTooltipComponent,
    TranslateModule
  ],
  templateUrl: './create-alert.html',
  styleUrls: ['./create-alert.css'],
  encapsulation: ViewEncapsulation.None
})
export class CreateAlertComponent implements OnInit {

  // ── Estado del formulario ──────────────────────────────────────────────────
  /** Formulario reactivo con los campos símbolo, condición, precio/porcentaje y estado. */
  alertForm: FormGroup;
  /** Catálogo de condiciones disponibles para el selector. */
  conditions = ALERT_CONDITIONS;
  /** Indica si el modal fue abierto para editar una alerta existente. */
  isEditMode = false;

  constructor(
    private fb: FormBuilder,
    private alertService: AlertService,
    private snackBarService: SnackBarService,
    private dialogRef: MatDialogRef<CreateAlertComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { alert?: Alert }
  ) {
    this.alertForm = this.fb.group({
      symbol:        ['', [Validators.required, Validators.pattern('^[A-Z]{1,5}$')]],
      condition:     ['<', Validators.required],
      price:         [null],
      percentChange: [null],
      isActive:      [true]
    });

    if (data?.alert) {
      this.isEditMode = true;
    }
  }

  ngOnInit(): void {
    if (this.data?.alert) {
      this.alertForm.patchValue({
        symbol:        this.data.alert.symbol        ?? '',
        condition:     this.data.alert.condition     ?? '<',
        price:         this.data.alert.price         ?? null,
        percentChange: this.data.alert.percentChange ?? null,
        isActive:      this.data.alert.isActive      ?? true
      });
    }

    // Ajusta los validadores de precio/porcentaje cuando cambia el tipo de condición
    this.alertForm.get('condition')?.valueChanges.subscribe(condition => {
      this.updateValueValidators(condition);
    });
  }

  // ── Lógica del formulario ──────────────────────────────────────────────────

  /**
   * Determina si la condición seleccionada actualmente es de tipo porcentual (`%>` o `%<`).
   * Se usa en el template para mostrar el campo de precio o el de porcentaje.
   * @returns `true` si la condición es porcentual, `false` si es de precio absoluto.
   */
  isPercentageCondition(): boolean {
    const condition = this.alertForm.get('condition')?.value;
    return condition === '%>' || condition === '%<';
  }

  // ── Acciones ───────────────────────────────────────────────────────────────

  /**
   * Valida el formulario y envía los datos al servicio correspondiente.
   * En modo edición llama a `update`; en modo creación llama a `create`.
   * Cierra el diálogo con `true` si la operación es exitosa.
   */
  onSubmit(): void {
    if (!this.alertForm.valid) return;

    const formValue = this.alertForm.value;

    const dto: CreateAlertDto = {
      symbol:        formValue.symbol.toUpperCase(),
      condition:     formValue.condition,
      price:         formValue.price,
      percentChange: formValue.percentChange,
      isActive:      formValue.isActive
    };

    if (this.isEditMode) {
      const updateDto: UpdateAlertDto = { id: this.data.alert!.id, ...dto };

      this.alertService.update(updateDto).subscribe({
        next: () => {
          this.snackBarService.successFromCode('ALERT_UPDATED');
          this.dialogRef.close(true);
        },
        error: error => {
          this.snackBarService.fromResponse(false, error?.error?.code, error?.error?.message);
        }
      });

      return;
    }

    this.alertService.create(dto).subscribe({
      next: () => {
        this.snackBarService.successFromCode('ALERT_CREATED');
        this.dialogRef.close(true);
      },
      error: error => {
        this.snackBarService.fromResponse(false, error?.error?.code, error?.error?.message);
      }
    });
  }

  /**
   * Cierra el diálogo sin guardar cambios.
   */
  onCancel(): void {
    this.dialogRef.close();
  }

  // ── Métodos privados ───────────────────────────────────────────────────────

  /**
   * Actualiza los validadores de `price` y `percentChange` según el tipo de condición.
   * Las condiciones porcentuales requieren un valor entre 0.01 y 100;
   * las de precio absoluto requieren un valor mayor a 0.01.
   * Limpia el valor del campo que queda fuera de uso para evitar datos residuales.
   * @param condition Valor de condición recién seleccionado.
   */
  private updateValueValidators(condition: string): void {
    const priceControl   = this.alertForm.get('price');
    const percentControl = this.alertForm.get('percentChange');

    if (condition === '%>' || condition === '%<') {
      priceControl?.clearValidators();
      priceControl?.setValue(null);
      percentControl?.setValidators([Validators.required, Validators.min(0.01), Validators.max(100)]);
    } else {
      percentControl?.clearValidators();
      percentControl?.setValue(null);
      priceControl?.setValidators([Validators.required, Validators.min(0.01)]);
    }

    priceControl?.updateValueAndValidity();
    percentControl?.updateValueAndValidity();
  }
}
