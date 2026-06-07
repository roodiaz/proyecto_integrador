import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MaterialModule } from '../../../../shared/material.module';

/** Símbolo ficticio mostrado en la barra de ticker animada del header. */
interface TickerItem {
  symbol: string;
  price: string;
  change: string;
  positive: boolean;
}

/** Tarjeta flotante con datos de mercado simulados sobre el hero. */
interface MarketCard {
  symbol: string;
  name: string;
  price: string;
  change: string;
  positive: boolean;
}

/** Beneficio de la plataforma mostrado en el carrusel automático. */
interface Benefit {
  icon: string;
  title: string;
  description: string;
}

/** Acción que el usuario puede realizar dentro de InvestLab. */
interface Capability {
  icon: string;
  title: string;
  description: string;
  accent: 'blue' | 'green' | 'purple' | 'amber' | 'pink';
}

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterModule, MaterialModule],
  templateUrl: './landing.html',
  styleUrl: './landing.css'
})
export class Landing implements OnInit, AfterViewInit, OnDestroy {
  /** Observador que dispara las animaciones de aparición al hacer scroll. */
  private revealObserver?: IntersectionObserver;

  /** Intervalo que rota automáticamente el carrusel de beneficios. */
  private benefitInterval?: ReturnType<typeof setInterval>;

  /** Símbolos ficticios que recorren la barra de ticker en loop continuo. */
  readonly tickerItems: TickerItem[] = [
    { symbol: 'AAPL', price: '$214.32', change: '+1.24%', positive: true },
    { symbol: 'TSLA', price: '$187.05', change: '-0.86%', positive: false },
    { symbol: 'NVDA', price: '$905.40', change: '+2.91%', positive: true },
    { symbol: 'MSFT', price: '$421.78', change: '+0.47%', positive: true },
    { symbol: 'AMZN', price: '$178.22', change: '-0.32%', positive: false },
    { symbol: 'GOOGL', price: '$172.61', change: '+0.95%', positive: true },
    { symbol: 'META', price: '$498.90', change: '+1.67%', positive: true },
    { symbol: 'NFLX', price: '$632.14', change: '-0.41%', positive: false },
    { symbol: 'S&P 500', price: '5,431.20', change: '+0.62%', positive: true },
    { symbol: 'NASDAQ', price: '17,862.40', change: '+0.89%', positive: true }
  ];

  /** Tarjetas flotantes con datos de mercado simulados sobre el hero. */
  readonly marketCards: MarketCard[] = [
    { symbol: 'NVDA', name: 'NVIDIA Corp.', price: '$905.40', change: '+2.91%', positive: true },
    { symbol: 'S&P 500', name: 'Índice S&P 500', price: '5,431.20', change: '+0.62%', positive: true },
    { symbol: 'TSLA', name: 'Tesla Inc.', price: '$187.05', change: '-0.86%', positive: false }
  ];

  /** Beneficios de la plataforma rotados automáticamente en el carrusel. */
  readonly benefits: Benefit[] = [
    {
      icon: 'bolt',
      title: 'Operá en tiempo real',
      description: 'Precios y gráficos actualizados al instante, igual que en una terminal profesional.'
    },
    {
      icon: 'shield',
      title: 'Cero riesgo real',
      description: 'Practicá con saldo virtual de USD 10.000 sin comprometer tu dinero.'
    },
    {
      icon: 'auto_graph',
      title: 'Alertas a tu medida',
      description: 'Definí condiciones de precio o porcentaje y enterate apenas se cumplan.'
    },
    {
      icon: 'school',
      title: 'Aprendizaje guiado',
      description: 'Entendé el mercado paso a paso mientras construís tu propio portfolio.'
    }
  ];

  /** Acciones principales que el usuario puede realizar dentro de la plataforma. */
  readonly capabilities: Capability[] = [
    {
      icon: 'candlestick_chart',
      title: 'Simular inversiones',
      description: 'Comprá y vendé activos con saldo virtual y viví la experiencia real del mercado sin riesgo.',
      accent: 'blue'
    },
    {
      icon: 'visibility',
      title: 'Seguir activos',
      description: 'Marcá tus favoritos y monitoreá su evolución con gráficos y datos actualizados.',
      accent: 'purple'
    },
    {
      icon: 'notifications_active',
      title: 'Crear alertas',
      description: 'Configurá avisos por precio o variación porcentual y enterate en el momento justo.',
      accent: 'amber'
    },
    {
      icon: 'pie_chart',
      title: 'Ver tu portfolio',
      description: 'Visualizá el rendimiento de tus inversiones simuladas con métricas claras y precisas.',
      accent: 'green'
    },
    {
      icon: 'menu_book',
      title: 'Aprender sin riesgo',
      description: 'Familiarizate con conceptos financieros reales en un entorno seguro y guiado.',
      accent: 'pink'
    }
  ];

  /** Índice del beneficio activo actualmente en el carrusel. */
  activeBenefitIndex = 0;

  constructor(private readonly hostRef: ElementRef<HTMLElement>) {}

  /**
   * Inicia la rotación automática del carrusel de beneficios cada 5 segundos.
   * @returns {void}
   */
  ngOnInit(): void {
    this.benefitInterval = setInterval(() => this.nextBenefit(), 5000);
  }

  /**
   * Activa la animación de "aparición" (`.reveal` → `.in-view`) sobre los
   * elementos marcados, a medida que entran en el viewport al scrollear.
   * @returns {void}
   */
  ngAfterViewInit(): void {
    const elements = this.hostRef.nativeElement.querySelectorAll('.reveal');

    this.revealObserver = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            entry.target.classList.add('in-view');
            this.revealObserver?.unobserve(entry.target);
          }
        }
      },
      { threshold: 0.15 }
    );

    elements.forEach((element) => this.revealObserver?.observe(element));
  }

  /**
   * Libera el observador de scroll y el intervalo del carrusel al destruirse el componente.
   * @returns {void}
   */
  ngOnDestroy(): void {
    this.revealObserver?.disconnect();
    if (this.benefitInterval) {
      clearInterval(this.benefitInterval);
    }
  }

  /**
   * Avanza el carrusel de beneficios al siguiente elemento, volviendo al inicio al llegar al final.
   * @returns {void}
   */
  nextBenefit(): void {
    this.activeBenefitIndex = (this.activeBenefitIndex + 1) % this.benefits.length;
  }

  /**
   * Selecciona manualmente un beneficio del carrusel y reinicia el temporizador automático.
   * @param {number} index Posición del beneficio a mostrar.
   * @returns {void}
   */
  selectBenefit(index: number): void {
    this.activeBenefitIndex = index;

    if (this.benefitInterval) {
      clearInterval(this.benefitInterval);
    }
    this.benefitInterval = setInterval(() => this.nextBenefit(), 5000);
  }
}
