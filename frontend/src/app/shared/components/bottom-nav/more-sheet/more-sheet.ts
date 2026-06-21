import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatBottomSheetRef } from '@angular/material/bottom-sheet';
import { MaterialModule } from '../../../material.module';
import { AuthSessionService } from '../../../../core/services/auth-session.service';

/**
 * Bottom sheet del ítem "Más" del bottom nav: agrupa las secciones que no
 * tienen lugar propio en la barra inferior (Watchlist, Notificaciones,
 * Configuración) más el cierre de sesión.
 */
@Component({
  selector: 'app-more-sheet',
  standalone: true,
  imports: [MaterialModule, TranslateModule],
  templateUrl: './more-sheet.html',
  styleUrl: './more-sheet.css'
})
export class MoreSheet {

  private readonly sheetRef = inject(MatBottomSheetRef<MoreSheet>);
  private readonly router = inject(Router);
  private readonly authSessionService = inject(AuthSessionService);

  navigateTo(path: string, queryParams?: Record<string, string>): void {
    this.sheetRef.dismiss();
    this.router.navigate([path], queryParams ? { queryParams } : undefined);
  }

  logout(): void {
    this.sheetRef.dismiss();
    this.authSessionService.logout();
  }
}
