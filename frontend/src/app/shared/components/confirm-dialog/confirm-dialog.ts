import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { MaterialModule } from '../../material.module';

/** Datos de entrada del diálogo de confirmación genérico. */
export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  danger?: boolean;
}

/**
 * Diálogo modal genérico de confirmación, reutilizable para cualquier acción
 * que requiera aceptación explícita del usuario (por ejemplo, eliminar un
 * portfolio). Devuelve `true` si el usuario confirma, o `undefined` si cancela
 * o cierra el diálogo.
 */
@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule, MaterialModule, TranslateModule],
  templateUrl: './confirm-dialog.html',
  styleUrl: './confirm-dialog.css'
})
export class ConfirmDialogComponent {

  constructor(
    private dialogRef: MatDialogRef<ConfirmDialogComponent, boolean>,
    @Inject(MAT_DIALOG_DATA) public data: ConfirmDialogData
  ) { }

  /** Cierra el diálogo sin confirmar la acción. */
  onCancel(): void {
    this.dialogRef.close();
  }

  /** Cierra el diálogo confirmando la acción. */
  onConfirm(): void {
    this.dialogRef.close(true);
  }
}
