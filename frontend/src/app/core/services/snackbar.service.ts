import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';

@Injectable({
  providedIn: 'root'
})
export class SnackBarService {

  private snackBar = inject(MatSnackBar);
  private translate = inject(TranslateService);

  private get closeLabel(): string {
    return this.translate.instant('COMMON.SNACKBAR_CLOSE');
  }

  success(message: string): void {
    this.snackBar.open(message, this.closeLabel, {
      duration: 4000,
      panelClass: ['app-snackbar', 'snackbar-success'],
      verticalPosition: 'top',
      horizontalPosition: 'center',
    });
  }

  error(message: string): void {
    this.snackBar.open(message, this.closeLabel, {
      duration: 5000,
      panelClass: ['app-snackbar', 'snackbar-error'],
      verticalPosition: 'top',
      horizontalPosition: 'center',
    });
  }

  info(message: string): void {
    this.snackBar.open(message, this.closeLabel, {
      duration: 4000,
      panelClass: ['app-snackbar', 'snackbar-info'],
      verticalPosition: 'top',
      horizontalPosition: 'center',
    });
  }

  successFromCode(code: string, fallback?: string): void {
    const key = `SUCCESS.${code}`;
    const translated = this.translate.instant(key);
    this.success(translated !== key ? translated : (fallback ?? code));
  }

  errorFromCode(code: string, fallback?: string): void {
    const key = `ERRORS.${code}`;
    const translated = this.translate.instant(key);
    this.error(translated !== key ? translated : (fallback ?? code));
  }

  fromResponse(success: boolean, code?: string, fallback?: string): void {
    if (success) {
      code ? this.successFromCode(code, fallback) : this.success(fallback ?? '');
    } else {
      code ? this.errorFromCode(code, fallback) : this.error(fallback ?? '');
    }
  }
}
