import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatBottomSheetRef } from '@angular/material/bottom-sheet';
import { MaterialModule } from '../../../material.module';

/**
 * Bottom sheet de la acción central "Operar" del bottom nav. Ni Comprar ni Vender
 * precargan ningún activo: ambas llevan a Mercado con el buscador enfocado, y el
 * usuario elige el ticker y usa los botones "Comprar"/"Vender" de esa pantalla
 * (que ya manejan correctamente el caso de no tener posición, etc.). Crear alerta
 * sí abre directamente el modal, porque no depende de un activo preexistente.
 */
@Component({
  selector: 'app-operate-sheet',
  standalone: true,
  imports: [MaterialModule, TranslateModule],
  templateUrl: './operate-sheet.html',
  styleUrl: './operate-sheet.css'
})
export class OperateSheet {

  private readonly sheetRef = inject(MatBottomSheetRef<OperateSheet>);
  private readonly router = inject(Router);

  /** Navega a Mercado con el buscador enfocado, para que el usuario elija el activo a comprar o vender. */
  goToMarket(): void {
    this.sheetRef.dismiss();
    this.router.navigate(['/market'], { queryParams: { focusSearch: true } });
  }

  createAlert(): void {
    this.sheetRef.dismiss();
    this.router.navigate(['/alerts'], { queryParams: { action: 'create' } });
  }
}
