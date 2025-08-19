import { Component, ElementRef, ViewChild, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterLink, RouterLinkActive, NavigationEnd, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatToolbarModule } from '@angular/material/toolbar';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-landing-body',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    RouterLink,
    RouterLinkActive,
    MatCardModule,
    MatButtonModule,
    MatDividerModule,
    MatToolbarModule
  ],
  templateUrl: './landing-body.html',
  styleUrl: './landing-body.css'
})
export class LandingBody implements OnInit {
  @ViewChild('imageSection') imageSection!: ElementRef;
  activeButton: string | null = null;

  constructor(private router: Router) {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.triggerAnimation();
    });
  }

  ngOnInit() {
    // Activar la animación al cargar el componente
    setTimeout(() => {
      this.triggerAnimation();
    }, 100);
  }

  setActive(buttonName: string) {
    this.activeButton = buttonName;
    this.triggerAnimation();
  }

  triggerAnimation() {
    const element = this.imageSection?.nativeElement;
    if (element) {
      // Agregar clase para reiniciar la animación
      element.classList.remove('animate-zoom');
      // Forzar un reflow
      void element.offsetWidth;
      // Volver a agregar la clase para reiniciar la animación
      element.classList.add('animate-zoom');
    }
  }

  isActive(buttonName: string): boolean {
    return this.activeButton === buttonName;
  }
}
