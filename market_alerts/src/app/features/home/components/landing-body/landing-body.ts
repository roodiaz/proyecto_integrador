import { Component, ElementRef, ViewChild, OnInit, AfterViewInit } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { RouterModule, RouterLink, RouterLinkActive, NavigationEnd, Router } from '@angular/router';
import { MaterialModule } from '../../../../shared/material.module';
import { filter } from 'rxjs/operators';
import { 
  Stock, 
  MarketIndex, 
  MarketNews, 
  SAMPLE_STOCKS, 
  SAMPLE_INDICES, 
  SAMPLE_NEWS 
} from '../../models/landing.models';

@Component({
  selector: 'app-landing-body',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    RouterLink,
    RouterLinkActive,
    CurrencyPipe,
    MaterialModule
  ],
  templateUrl: './landing-body.html',
  styleUrls: ['./landing-body.css']
})
export class LandingBody implements OnInit, AfterViewInit {
  @ViewChild('imageSection') imageSection!: ElementRef;
  activeButton: string | null = null;
  activeSection: string = '';
  isSlidingSectionVisible: boolean = false;
  activeTabIndex = 0;
  
  // Datos de mercado
  topStocks: Stock[] = SAMPLE_STOCKS;
  marketNews: MarketNews[] = SAMPLE_NEWS;
  marketIndices: MarketIndex[] = SAMPLE_INDICES;

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

  ngAfterViewInit() {
    // Inicialización después de que la vista se ha cargado
    this.triggerAnimation();
  }

  toggleSlidingSection() {
    this.isSlidingSectionVisible = !this.isSlidingSectionVisible;
  }
}
