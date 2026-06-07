import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

/**
 * Servicio que centraliza el estado de colapso de la barra lateral (`Sidebar`),
 * permitiendo que tanto la barra como el layout que la contiene se mantengan
 * sincronizados a través de un observable compartido.
 */
@Injectable({
  providedIn: 'root'
})
export class SidebarService {

  private isCollapsed = new BehaviorSubject<boolean>(false);

  /** Observable del estado de colapso de la barra lateral (`true` = colapsada). */
  isCollapsed$ = this.isCollapsed.asObservable();

  /** Alterna el estado de colapso de la barra lateral entre colapsada y expandida. */
  toggle() {
    this.isCollapsed.next(!this.isCollapsed.value);
  }
}
