import { Component, EventEmitter, Input, Output, OnInit, Inject, Optional } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { PlanType } from '../../models/register.model';

interface PlanFeature {
  text: string;
  free: string;
  premium: string;
}

interface Plan {
  type: PlanType;
  title: string;
  price: string;
  priceSuffix: string;
  features: PlanFeature[];
}

export interface PlanSelectionConfig {
  isModal?: boolean;
  currentPlan?: PlanType;
  showBackButton?: boolean;
  showComparison?: boolean;
}

@Component({
  selector: 'app-plan-selection',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule
  ],
  templateUrl: './plan-selection.html',
  styleUrls: ['./plan-selection.css']
})

export class PlanSelection implements OnInit {
  @Input() isModal = false;
  @Input() showBackButton = true;
  @Input() showComparison = true;
  @Input() currentPlan: PlanType = PlanType.FREE;
  @Output() planSelected = new EventEmitter<PlanType | null>();
  @Output() continue = new EventEmitter<PlanType>();
  
  PlanType = PlanType;
  selectedPlan: PlanType = PlanType.FREE;
  currentPlanIndex = 0;
  
  currentDate = new Date();
  nextBillingDate = new Date(this.currentDate.setMonth(this.currentDate.getMonth() + 1)).toLocaleDateString('es-AR');
  
  plans: Plan[] = [
    {
      type: PlanType.FREE,
      title: 'Plan Gratis',
      price: '$0',
      priceSuffix: '/siempre',
      features: [
        { text: 'Búsqueda de Tickers', free: 'Hasta 5 por día', premium: 'Ilimitados' },
        { text: 'Datos de Activos', free: 'Precio actual, variación % y datos básicos', premium: 'Datos completos (precio, variación, mercado, divisa, historial)' },
        { text: 'Operaciones Simuladas', free: 'Saldo inicial USD 1,000 – máx. 10 operaciones/mes', premium: 'Saldo inicial USD 10,000 – operaciones ilimitadas' },
        { text: 'Gráficos', free: 'Línea simple – histórico de 1 mes', premium: 'Velas japonesas o línea – histórico completo – comparaciones – % de ganancia/pérdida' },
        { text: 'Alertas de Precio', free: '1 alerta activa – intervalo fijo (5 min) – solo en UI', premium: 'Ilimitadas – intervalo configurable (1 a 5 min) – UI + Email' },
        { text: 'Lista de Seguimiento', free: 'Máx. 3 tickers favoritos', premium: 'Ilimitados' },
        { text: 'Portafolio Virtual', free: 'Tabla simple con precios actuales y variación %', premium: 'Tabla completa + gráficos (torta y evolución del portafolio)' },
        { text: 'Configuraciones', free: 'Moneda fija (USD)', premium: 'Moneda preferida (USD, ARS, etc.)' },
        { text: 'Notificaciones', free: 'Solo dentro de la app', premium: 'App + Email' }
      ]
    },
    {
      type: PlanType.PREMIUM,
      title: 'Premium',
      price: '$9.99',
      priceSuffix: '/mes',
      features: [
        { text: 'Búsqueda de Tickers', free: 'Hasta 5 por día', premium: 'Ilimitados' },
        { text: 'Datos de Activos', free: 'Precio actual, variación % y datos básicos', premium: 'Datos completos (precio, variación, mercado, divisa, historial)' },
        { text: 'Operaciones Simuladas', free: 'Saldo inicial USD 1,000 – máx. 10 operaciones/mes', premium: 'Saldo inicial USD 10,000 – operaciones ilimitadas' },
        { text: 'Gráficos', free: 'Línea simple – histórico de 1 mes', premium: 'Velas japonesas o línea – histórico completo – comparaciones – % de ganancia/pérdida' },
        { text: 'Alertas de Precio', free: '1 alerta activa – intervalo fijo (5 min) – solo en UI', premium: 'Ilimitadas – intervalo configurable (1 a 5 min) – UI + Email' },
        { text: 'Lista de Seguimiento', free: 'Máx. 3 tickers favoritos', premium: 'Ilimitados' },
        { text: 'Portafolio Virtual', free: 'Tabla simple con precios actuales y variación %', premium: 'Tabla completa + gráficos (torta y evolución del portafolio)' },
        { text: 'Configuraciones', free: 'Moneda fija (USD)', premium: 'Moneda preferida (USD, ARS, etc.)' },
        { text: 'Notificaciones', free: 'Solo dentro de la app', premium: 'App + Email' }
      ]
    }
  ];

  constructor(
    @Optional() private dialogRef: MatDialogRef<PlanSelection>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    if (data) {
      this.isModal = data.isModal ?? this.isModal;
      this.currentPlan = data.currentPlan ?? this.currentPlan;
      this.showBackButton = data.showBackButton ?? this.showBackButton;
      this.showComparison = data.showComparison ?? this.showComparison;
    }
  }

  ngOnInit(): void {
    this.selectedPlan = this.currentPlan;
    this.currentPlanIndex = this.plans.findIndex(p => p.type === this.currentPlan);
  }

  selectPlan(planType: PlanType): void {
    this.selectedPlan = planType;
    this.currentPlanIndex = this.plans.findIndex(p => p.type === planType);
    this.planSelected.emit(planType);
    
    // Auto-continue when in modal view
    if (this.isModal) {
      this.onContinue();
    }
  }

  onContinue(): void {
    if (this.isModal && this.dialogRef) {
      this.dialogRef.close(this.selectedPlan);
    } else {
      this.continue.emit(this.selectedPlan);
    }
  }

  onClose(): void {
    if (this.dialogRef) {
      this.dialogRef.close();
    } else {
      this.planSelected.emit(null);
    }
  }

  onBack(): void {
    this.planSelected.emit(null);
  }

  nextPlan(): void {
    if (this.currentPlanIndex < this.plans.length - 1) {
      this.currentPlanIndex++;
      this.selectPlan(this.plans[this.currentPlanIndex].type);
    }
  }

  previousPlan(): void {
    if (this.currentPlanIndex > 0) {
      this.currentPlanIndex--;
      this.selectPlan(this.plans[this.currentPlanIndex].type);
    }
  }

  getFeatureValue(planType: PlanType, category: string): string | null {
    const plan = this.plans.find(p => p.type === planType);
    if (!plan) return null;
    
    const feature = plan.features.find(f => f.text === category);
    if (!feature) return null;
    
    return planType === PlanType.FREE ? feature.free : feature.premium;
  }

  getCurrentPlanName(): string {
    return this.selectedPlan === PlanType.FREE ? 'Gratis' : 'Premium';
  }
}
