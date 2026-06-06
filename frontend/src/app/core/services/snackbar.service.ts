import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})
export class SnackBarService {

  private snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.snackBar.open(message, 'Cerrar', {
      duration: 4000,
      panelClass: ['app-snackbar', 'snackbar-success'],
      verticalPosition: 'top',
      horizontalPosition: 'right',
    });
  }

  error(message: string): void {
    this.snackBar.open(message, 'Cerrar', {
      duration: 5000,
      panelClass: ['app-snackbar', 'snackbar-error'],
      verticalPosition: 'top',
      horizontalPosition: 'right',
    });
  }

  info(message: string): void {
    this.snackBar.open(message, 'Cerrar', {
      duration: 4000,
      panelClass: ['app-snackbar', 'snackbar-info'],
      verticalPosition: 'top',
      horizontalPosition: 'right',
    });
  }

}