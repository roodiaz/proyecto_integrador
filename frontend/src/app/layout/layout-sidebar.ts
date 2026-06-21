import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SidebarService } from '../core/services/sidebar.service';
import { ViewportService } from '../core/services/viewport.service';
import { RouterOutlet } from '@angular/router';
import { Sidebar } from '../shared/components/sidebar/sidebar';
import { Header } from '../shared/components/header/header';
import { BottomNav } from '../shared/components/bottom-nav/bottom-nav';

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
  imports: [CommonModule, RouterOutlet, Sidebar, Header, BottomNav],
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
   * @param viewportService Servicio que informa si el viewport actual es mobile, para alternar entre Sidebar y BottomNav.
   */
  constructor(private sidebarService: SidebarService, public viewportService: ViewportService) {
    this.sidebarService.isCollapsed$.subscribe(isCollapsed => {
      this.isSidebarCollapsed = isCollapsed;
    });
  }
}
