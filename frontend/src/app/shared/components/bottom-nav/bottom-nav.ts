import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatBottomSheet } from '@angular/material/bottom-sheet';
import { MaterialModule } from '../../material.module';
import { OperateSheet } from './operate-sheet/operate-sheet';
import { MoreSheet } from './more-sheet/more-sheet';

/**
 * Navegación inferior para mobile: reemplaza al Sidebar cuando el viewport
 * es chico. Concentra las 3 secciones de mayor frecuencia de uso (Dashboard,
 * Mercado, Portfolio), una acción central de "Operar" (atajo a comprar/vender/
 * crear alerta) y un acceso a "Más" para el resto de las secciones.
 */
@Component({
  selector: 'app-bottom-nav',
  standalone: true,
  imports: [CommonModule, RouterModule, MaterialModule, TranslateModule],
  templateUrl: './bottom-nav.html',
  styleUrl: './bottom-nav.css'
})
export class BottomNav {

  private readonly bottomSheet = inject(MatBottomSheet);
  private readonly router = inject(Router);

  /** Abre el bottom sheet de acciones rápidas de trading (Comprar/Vender/Crear alerta). */
  openOperateSheet(): void {
    this.bottomSheet.open(OperateSheet);
  }

  /** Abre el bottom sheet con el resto de las secciones (Watchlist, Notificaciones, Configuración, Cerrar sesión). */
  openMoreSheet(): void {
    this.bottomSheet.open(MoreSheet);
  }
}
