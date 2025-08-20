import { Component, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatBadgeModule,
    MatDividerModule,
    MatButtonToggleModule,
    FormsModule
  ],
  templateUrl: './header.html',
  styleUrls: ['./header.css']
})
export class Header {
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

  // Currency settings
  currency: 'USD' | 'ARS' = 'USD';

  // Mostrar monto fijo sin conversión
  get walletAmount(): string {
    return `$${this.user.wallet.toFixed(2)}`;
  }

  // Cambiar solo la moneda visualmente
  changeCurrency(currency: 'USD' | 'ARS') {
    this.currency = currency;
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