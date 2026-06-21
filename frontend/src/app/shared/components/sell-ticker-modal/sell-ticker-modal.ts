import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';

/**
 * Paso intermedio del FAB "Operar" > Vender: pide el ticker que el usuario
 * quiere vender (no hay un activo preseleccionado en este punto, a diferencia
 * de Mercado o Portfolio). Al confirmar, devuelve el símbolo para que quien
 * abrió este modal abra el modal de Vender ya existente — ese modal se
 * encarga de buscar en qué portfolio(s) el usuario tiene esa posición, dejarlo
 * elegir si hay más de uno, o avisar si no la tiene en ninguno.
 */
@Component({
  selector: 'app-sell-ticker-modal',
  standalone: true,
  imports: [FormsModule, TranslateModule],
  templateUrl: './sell-ticker-modal.html',
  styleUrl: './sell-ticker-modal.css'
})
export class SellTickerModal {

  ticker = '';

  constructor(private dialogRef: MatDialogRef<SellTickerModal, string>) {}

  /** Cierra el modal sin devolver ningún ticker. */
  onCancel(): void {
    this.dialogRef.close();
  }

  /** Confirma el ticker ingresado (si no está vacío) y cierra el modal devolviéndolo. */
  onContinue(): void {
    const value = this.ticker.trim().toUpperCase();
    if (!value) return;
    this.dialogRef.close(value);
  }
}
