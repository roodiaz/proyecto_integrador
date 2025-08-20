import { Component, OnInit, ViewChild, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSidenav } from '@angular/material/sidenav';
import { Sidebar } from '../sidebar/sidebar';
import { Header } from '../header/header';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    MatSidenavModule,
    MatButtonModule,
    MatIconModule,
    Sidebar,
    Header
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard implements OnInit {
  @ViewChild('sidenav') sidenav!: MatSidenav;
  showSidebar = true;
  isMobile = false;

  ngOnInit() {
    this.checkScreenSize();
    if (this.sidenav && !this.isMobile) {
      this.sidenav.open();
    }
  }

  @HostListener('window:resize')
  onResize() {
    this.checkScreenSize();
  }

  private checkScreenSize() {
    this.isMobile = window.innerWidth < 960;
    if (this.isMobile) {
      this.showSidebar = false;
    } else {
      this.showSidebar = true;
    }
  }

  toggleSidebar() {
    this.showSidebar = !this.showSidebar;
    if (this.sidenav) {
      this.sidenav.toggle();
    }
  }
}
