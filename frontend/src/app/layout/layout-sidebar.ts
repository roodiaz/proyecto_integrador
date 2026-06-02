import { Component } from '@angular/core';
import { SidebarService } from '../core/services/sidebar.service';
import { RouterOutlet } from '@angular/router';
import { Sidebar } from '../shared/components/sidebar/sidebar';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, Sidebar],
  templateUrl: './layout-sidebar.html',
  styleUrls: ['./layout-sidebar.css']
})
export class LayoutComponent {
  isSidebarCollapsed = false;

  constructor(private sidebarService: SidebarService) {
    this.sidebarService.isCollapsed$.subscribe(isCollapsed => {
      this.isSidebarCollapsed = isCollapsed;
    });
  }
}
