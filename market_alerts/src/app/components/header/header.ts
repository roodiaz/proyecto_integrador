import { Component, Output, EventEmitter } from '@angular/core';
import { SidebarService } from '../../services/sidebar.service';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MaterialModule } from '../../shared/material.module';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    MaterialModule
  ],
  templateUrl: './header.html',
  styleUrls: ['./header.css']
})
export class Header {
  isSidebarCollapsed = false;

  constructor(private sidebarService: SidebarService) {
    this.sidebarService.isCollapsed$.subscribe(isCollapsed => {
      this.isSidebarCollapsed = isCollapsed;
    });
  }
  @Output() toggleSidebar = new EventEmitter<void>();
  @Output() toggleTheme = new EventEmitter<void>();
  @Output() changeLanguage = new EventEmitter<string>();
  @Output() logout = new EventEmitter<void>();

  // User data
  user = {
    name: 'Juan Pérez',
    email: 'juan.perez@example.com',
    wallet: 1250.75,
    notifications: 3
  };

  // Language settings
  language: 'es' | 'en' = 'es';

  // Mostrar monto fijo sin conversión
  get walletAmount(): string {
    return `$${this.user.wallet.toFixed(2)}`;
  }

  // Cambiar solo el idioma visualmente
  setLanguage(language: 'es' | 'en') {
    this.language = language;
  }

  // Handle menu actions
  handleMenuAction(action: string) {
    switch(action) {
      case 'profile':
        // Navigate to profile
        break;
      case 'theme':
        this.toggleTheme.emit();
        break;
      case 'language':
        this.changeLanguage.emit('es'); // Toggle between languages
        break;
      case 'logout':
        this.logout.emit();
        break;
    }
  }

  // Toggle sidebar
  onToggleSidebar() {
    this.toggleSidebar.emit();
  }
}