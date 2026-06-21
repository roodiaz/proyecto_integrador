import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatBottomSheetRef } from '@angular/material/bottom-sheet';
import { MaterialModule } from '../../../material.module';

/**
 * Bottom sheet de la acción central "Operar" del bottom nav. Comprar y vender
 * requieren primero elegir un activo, así que ambas opciones llevan a Mercado
 * (con foco en el buscador); crear alerta abre directamente el modal de alerta.
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

  goToMarket(): void {
    this.sheetRef.dismiss();
    this.router.navigate(['/market'], { queryParams: { focusSearch: true } });
  }

  createAlert(): void {
    this.sheetRef.dismiss();
    this.router.navigate(['/alerts'], { queryParams: { action: 'create' } });
  }
}
