import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { MaterialModule } from '../../../../shared/material.module';
import { SetupPortfolioRequest } from '../../models/portfolio.model';
import {
  PORTFOLIO_NAME_MAX_LENGTH,
  PORTFOLIO_NAME_MIN_LENGTH,
  PORTFOLIO_NAME_PATTERN,
  INITIAL_BALANCE_DEFAULT,
  INITIAL_BALANCE_MAX,
  INITIAL_BALANCE_MIN
} from '../../../../shared/validators/portfolio-setup.validator';

/** Modo del diálogo: configuración inicial del portfolio, reinicio de la simulación o creación de un portfolio adicional. */
export type PortfolioSetupDialogMode = 'create' | 'reset' | 'add-portfolio';

/** Datos de entrada del diálogo de configuración/reinicio del portfolio. */
export interface PortfolioSetupDialogData {
  mode: PortfolioSetupDialogMode;
  currentName?: string | null;
}

/**
 * Diálogo modal que permite definir el nombre del portfolio y el saldo inicial
 * de la simulación.
 *
 * En modo `create` se muestra como wizard obligatorio tras el primer login
 * (sin botón de cancelar, con campos vacíos y saldo inicial por defecto).
 * En modo `reset` se reutiliza para el reinicio de la simulación: el nombre
 * se precarga con el valor actual del usuario y el saldo inicial siempre
 * vuelve a proponerse en su valor por defecto.
 */
@Component({
  selector: 'app-portfolio-setup-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MaterialModule, TranslateModule],
  templateUrl: './portfolio-setup-dialog.html',
  styleUrl: './portfolio-setup-dialog.css'
})
export class PortfolioSetupDialog {

  readonly mode: PortfolioSetupDialogMode;
  readonly form: FormGroup;

  readonly nameMinLength = PORTFOLIO_NAME_MIN_LENGTH;
  readonly nameMaxLength = PORTFOLIO_NAME_MAX_LENGTH;
  readonly balanceMin = INITIAL_BALANCE_MIN;
  readonly balanceMax = INITIAL_BALANCE_MAX;

  /**
   * @param dialogRef Referencia al diálogo, usada para cerrarlo y devolver los datos confirmados.
   * @param data Modo del diálogo (`create`/`reset`) y nombre actual del portfolio (para precargar en `reset`).
   * @param fb Constructor de formularios reactivos.
   */
  constructor(
    private dialogRef: MatDialogRef<PortfolioSetupDialog, SetupPortfolioRequest>,
    @Inject(MAT_DIALOG_DATA) public data: PortfolioSetupDialogData,
    private fb: FormBuilder
  ) {
    this.mode = data.mode;

    this.form = this.fb.group({
      portfolioName: [
        this.mode === 'reset' ? (data.currentName ?? '') : '',
        [
          Validators.required,
          Validators.minLength(PORTFOLIO_NAME_MIN_LENGTH),
          Validators.maxLength(PORTFOLIO_NAME_MAX_LENGTH),
          Validators.pattern(PORTFOLIO_NAME_PATTERN)
        ]
      ],
      initialBalance: [
        INITIAL_BALANCE_DEFAULT,
        [
          Validators.required,
          Validators.min(INITIAL_BALANCE_MIN),
          Validators.max(INITIAL_BALANCE_MAX)
        ]
      ]
    });

    if (this.mode === 'create')
      this.dialogRef.disableClose = true;
  }

  /** Referencia al control `portfolioName` para acceder a sus errores de validación en el template. */
  get portfolioName() { return this.form.get('portfolioName'); }

  /** Referencia al control `initialBalance` para acceder a sus errores de validación en el template. */
  get initialBalance() { return this.form.get('initialBalance'); }

  /** Cierra el diálogo sin confirmar. Solo disponible en modo `reset`. */
  onCancel(): void {
    this.dialogRef.close();
  }

  /**
   * Valida el formulario y, si es correcto, cierra el diálogo devolviendo
   * el nombre del portfolio y el saldo inicial elegidos.
   */
  onConfirm(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.value;

    this.dialogRef.close({
      portfolioName: (value.portfolioName as string).trim(),
      initialBalance: value.initialBalance as number
    });
  }
}
