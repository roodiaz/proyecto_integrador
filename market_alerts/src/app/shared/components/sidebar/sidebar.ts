import { Component } from '@angular/core';
import { SidebarService } from '../../../core/services/sidebar.service';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { MaterialModule } from '../../material.module';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MaterialModule
  ],
  templateUrl: './sidebar.html',
  styleUrls: ['./sidebar.css']
})
export class Sidebar {
  isCollapsed = false;

  constructor(private sidebarService: SidebarService, private router: Router) {
    this.sidebarService.isCollapsed$.subscribe(isCollapsed => {
      this.isCollapsed = isCollapsed;
    });
  }

  toggleSidebar() {
    this.sidebarService.toggle();
  }

  logout() {
    this.router.navigate(['/login']);
  }
}