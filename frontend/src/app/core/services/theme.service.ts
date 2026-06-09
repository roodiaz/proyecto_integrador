import { Injectable, signal } from '@angular/core';

export type Theme = 'Dark' | 'Light';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly STORAGE_KEY = 'investlab-theme';

  readonly currentTheme = signal<Theme>('Dark');

  /**
   * Aplica el tema al iniciar la aplicación.
   * Prioridad: backend > localStorage > 'Dark' (default).
   */
  initialize(backendTheme?: string): void {
    const stored = localStorage.getItem(this.STORAGE_KEY) as Theme | null;
    const theme = this.resolveTheme(backendTheme) ?? stored ?? 'Dark';
    this.applyTheme(theme);
  }

  setTheme(theme: Theme): void {
    this.applyTheme(theme);
    localStorage.setItem(this.STORAGE_KEY, theme);
  }

  private resolveTheme(value?: string): Theme | null {
    if (value === 'Light') return 'Light';
    if (value === 'Dark') return 'Dark';
    return null;
  }

  private applyTheme(theme: Theme): void {
    this.currentTheme.set(theme);
    if (theme === 'Light') {
      document.documentElement.setAttribute('data-theme', 'light');
      document.body.setAttribute('data-theme', 'light');
    } else {
      document.documentElement.removeAttribute('data-theme');
      document.body.removeAttribute('data-theme');
    }
  }

  /** Devuelve la configuración de colores para instancias de Chart.js */
  getChartTheme() {
    const isLight = this.currentTheme() === 'Light';
    return {
      textColor: isLight ? 'rgba(26, 29, 46, 0.85)' : 'rgba(255, 255, 255, 0.7)',
      gridColor: isLight ? 'rgba(0, 0, 0, 0.07)' : 'rgba(255, 255, 255, 0.1)',
      gridBorderColor: isLight ? 'rgba(0, 0, 0, 0.12)' : 'rgba(255, 255, 255, 0.2)',
      tooltipBg: isLight ? 'rgba(255, 255, 255, 0.97)' : 'rgba(20, 20, 20, 0.9)',
      tooltipText: isLight ? '#1a1d2e' : '#ffffff',
      tooltipBorderColor: isLight ? '#527bd9' : '#4a90e2',
      legendColor: isLight ? '#1a1d2e' : '#ffffff',
    };
  }
}
