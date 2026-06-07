import { Component } from '@angular/core';
import { SidebarService } from '../core/services/sidebar.service';
import { RouterOutlet } from '@angular/router';
import { Sidebar } from '../shared/components/sidebar/sidebar';

/**
 * Layout principal de la aplicación: combina la barra lateral de navegación
 * (`Sidebar`) con el área de contenido donde se renderizan las pantallas
 * según la ruta activa (`router-outlet`).
 *
 * Se suscribe al estado de colapso de la barra lateral para ajustar el
 * margen del área de contenido y que ambas se mantengan sincronizadas.
 */
@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, Sidebar],
  templateUrl: './layout-sidebar.html',
  styleUrls: ['./layout-sidebar.css']
})
export class LayoutComponent {

  // ── Estado de la barra lateral ──
  isSidebarCollapsed = false;

  /**
   * Se suscribe al observable de estado de la barra lateral para reflejar,
   * en tiempo real, si está colapsada o expandida.
   * @param sidebarService Servicio que expone el estado de colapso de la barra lateral.
   */
  constructor(private sidebarService: SidebarService) {
    this.sidebarService.isCollapsed$.subscribe(isCollapsed => {
      this.isSidebarCollapsed = isCollapsed;
    });
  }
}
