import { Injectable, signal } from '@angular/core';
import { BreakpointObserver } from '@angular/cdk/layout';

/** Breakpoint bajo el cual la app pasa a layout mobile (bottom nav, cards, bottom sheets). */
export const MOBILE_BREAKPOINT = '(max-width: 768px)';

/**
 * Centraliza la detección del viewport mobile vs. desktop para que todos los
 * componentes (layout, dashboard, mercado, portfolio, etc.) usen el mismo
 * breakpoint y reaccionen de forma consistente a los cambios de tamaño.
 */
@Injectable({
  providedIn: 'root'
})
export class ViewportService {

  /** `true` cuando el viewport actual corresponde a mobile (<= 768px). */
  readonly isMobile = signal(false);

  constructor(private breakpointObserver: BreakpointObserver) {
    this.breakpointObserver.observe(MOBILE_BREAKPOINT).subscribe(result => {
      this.isMobile.set(result.matches);
    });
  }
}
