import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

/**
 * Servicio que centraliza la presentación de notificaciones tipo "snackbar"
 * (éxito, error e información) con un estilo y comportamiento consistentes
 * en toda la aplicación.
 */
@Injectable({
  providedIn: 'root'
})
export class SnackBarService {

  private snackBar = inject(MatSnackBar);

  /**
   * Muestra una notificación de éxito (verde) durante 4 segundos, en la
   * parte superior central de la pantalla.
   * @param message Mensaje a mostrar al usuario.
   */
  success(message: string): void {
    this.snackBar.open(message, 'Cerrar', {
      duration: 4000,
      panelClass: ['app-snackbar', 'snackbar-success'],
      verticalPosition: 'top',
      horizontalPosition: 'center',
    });
  }

  /**
   * Muestra una notificación de error (roja) durante 5 segundos, en la
   * parte superior central de la pantalla.
   * @param message Mensaje a mostrar al usuario.
   */
  error(message: string): void {
    this.snackBar.open(message, 'Cerrar', {
      duration: 5000,
      panelClass: ['app-snackbar', 'snackbar-error'],
      verticalPosition: 'top',
      horizontalPosition: 'center',
    });
  }

  /**
   * Muestra una notificación informativa (azul) durante 4 segundos, en la
   * parte superior central de la pantalla.
   * @param message Mensaje a mostrar al usuario.
   */
  info(message: string): void {
    this.snackBar.open(message, 'Cerrar', {
      duration: 4000,
      panelClass: ['app-snackbar', 'snackbar-info'],
      verticalPosition: 'top',
      horizontalPosition: 'center',
    });
  }

}
