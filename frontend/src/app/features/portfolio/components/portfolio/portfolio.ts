import { Component, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { PortfolioService } from '../../services/portfolio.service';
import { PortfolioOperation } from '../../models/portfolio-operation';
import { PortfolioSummary } from '../../models/portfolio-summary';

@Component({
  selector: 'app-portfolio',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './portfolio.html',
  styleUrl: './portfolio.css'
})
export class Portfolio implements OnInit, OnDestroy, AfterViewInit {
  operations: PortfolioOperation[] = [];
  portfolioSummary: PortfolioSummary = {
    initialBalance: 10000,
    currentBalance: 10000,
    totalProfitLoss: 0,
    totalProfitLossPercent: 0,
    totalInvested: 0
  };
  
  newTicker: string = '';
  newQuantity: number = 0;
  newPrice: number = 0;
  
  private subscription: Subscription | null = null;

  constructor() {}

  ngOnInit(): void {
    this.loadPortfolioData();
    this.startLiveUpdates();
  }

  ngOnDestroy(): void {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }

  ngAfterViewInit(): void {
    // Inicializar gráficos después de que la vista se renderice
    setTimeout(() => {
      this.initializeCharts();
    }, 100);
  }

  loadPortfolioData(): void {
    // Datos de ejemplo para mostrar mientras el backend no está disponible
    this.operations = [
      {
        id: '1',
        ticker: 'AAPL',
        quantity: 10,
        buyPrice: 175.50,
        currentPrice: 185.50,
        variationPercent: 5.7,
        profitLoss: 100.00,
        profitLossPercent: 5.7,
        operationDate: new Date('2024-01-15'),
        isOpen: true
      },
      {
        id: '2',
        ticker: 'GOOGL',
        quantity: 5,
        buyPrice: 140.00,
        currentPrice: 142.30,
        variationPercent: 1.6,
        profitLoss: 11.50,
        profitLossPercent: 1.6,
        operationDate: new Date('2024-01-20'),
        isOpen: true
      },
      {
        id: '3',
        ticker: 'MSFT',
        quantity: 8,
        buyPrice: 370.00,
        currentPrice: 380.75,
        variationPercent: 2.9,
        profitLoss: 86.00,
        profitLossPercent: 2.9,
        operationDate: new Date('2024-02-01'),
        isOpen: true
      }
    ];
    
    this.updatePortfolioSummary();
  }

  startLiveUpdates(): void {
    // Simulación de actualización en vivo
    this.subscription = new Subscription();
  }

  updatePortfolioSummary(): void {
    const totalInvested = this.operations
      .filter(op => op.isOpen)
      .reduce((sum, op) => sum + (op.quantity * op.buyPrice), 0);
    
    const currentValue = this.operations
      .filter(op => op.isOpen)
      .reduce((sum, op) => sum + (op.quantity * op.currentPrice), 0);
    
    const currentBalance = this.portfolioSummary.initialBalance - totalInvested + currentValue;
    const totalProfitLoss = currentBalance - this.portfolioSummary.initialBalance;
    const totalProfitLossPercent = (totalProfitLoss / this.portfolioSummary.initialBalance) * 100;

    this.portfolioSummary = {
      ...this.portfolioSummary,
      currentBalance,
      totalProfitLoss,
      totalProfitLossPercent,
      totalInvested
    };
  }

  canBuy(): boolean {
    return !!(this.newTicker.trim() && 
           this.newQuantity > 0 && 
           this.newPrice > 0 &&
           (this.newQuantity * this.newPrice) <= this.portfolioSummary.currentBalance);
  }

  buyAsset(): void {
    if (!this.canBuy()) return;

    const newOperation: PortfolioOperation = {
      id: Date.now().toString(),
      ticker: this.newTicker.trim().toUpperCase(),
      quantity: this.newQuantity,
      buyPrice: this.newPrice,
      currentPrice: this.newPrice,
      variationPercent: 0,
      profitLoss: 0,
      profitLossPercent: 0,
      operationDate: new Date(),
      isOpen: true
    };

    this.operations.push(newOperation);
    this.updatePortfolioSummary();
    
    // Limpiar formulario
    this.newTicker = '';
    this.newQuantity = 0;
    this.newPrice = 0;
  }

  sellOperation(operationId: string): void {
    const operation = this.operations.find(op => op.id === operationId);
    if (operation) {
      operation.isOpen = false;
      operation.profitLoss = (operation.currentPrice - operation.buyPrice) * operation.quantity;
      operation.profitLossPercent = ((operation.currentPrice - operation.buyPrice) / operation.buyPrice) * 100;
      this.updatePortfolioSummary();
    }
  }

  initializeCharts(): void {
    this.drawPieChart();
    this.drawLineChart();
  }

  drawPieChart(): void {
    const canvas = document.getElementById('pieChart') as HTMLCanvasElement;
    if (!canvas) return;
    
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const openOperations = this.operations.filter(op => op.isOpen);
    const data = openOperations.map(op => ({
      label: op.ticker,
      value: op.quantity * op.currentPrice,
      color: this.getRandomColor()
    }));

    // Dibujar gráfico de torta simple
    const centerX = canvas.width / 2;
    const centerY = canvas.height / 2;
    const radius = Math.min(centerX, centerY) - 20;
    
    let currentAngle = -Math.PI / 2;
    const total = data.reduce((sum, item) => sum + item.value, 0);

    data.forEach(item => {
      const sliceAngle = (item.value / total) * 2 * Math.PI;
      
      ctx.beginPath();
      ctx.arc(centerX, centerY, radius, currentAngle, currentAngle + sliceAngle);
      ctx.lineTo(centerX, centerY);
      ctx.fillStyle = item.color;
      ctx.fill();
      
      // Etiqueta
      const labelAngle = currentAngle + sliceAngle / 2;
      const labelX = centerX + Math.cos(labelAngle) * (radius * 0.7);
      const labelY = centerY + Math.sin(labelAngle) * (radius * 0.7);
      
      ctx.fillStyle = '#fff';
      ctx.font = '12px Arial';
      ctx.textAlign = 'center';
      ctx.fillText(item.label, labelX, labelY);
      
      currentAngle += sliceAngle;
    });
  }

  drawLineChart(): void {
    const canvas = document.getElementById('lineChart') as HTMLCanvasElement;
    if (!canvas) return;
    
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Datos de ejemplo para la evolución
    const data = [
      { day: 1, value: 10000 },
      { day: 5, value: 10200 },
      { day: 10, value: 10150 },
      { day: 15, value: 10350 },
      { day: 20, value: 10500 },
      { day: 25, value: 10400 },
      { day: 30, value: 10600 }
    ];

    const padding = 40;
    const width = canvas.width - 2 * padding;
    const height = canvas.height - 2 * padding;
    
    const maxValue = Math.max(...data.map(d => d.value));
    const minValue = Math.min(...data.map(d => d.value));
    const valueRange = maxValue - minValue;

    // Dibujar ejes
    ctx.strokeStyle = '#ccc';
    ctx.beginPath();
    ctx.moveTo(padding, padding);
    ctx.lineTo(padding, canvas.height - padding);
    ctx.lineTo(canvas.width - padding, canvas.height - padding);
    ctx.stroke();

    // Dibujar línea
    ctx.strokeStyle = '#1da1f2';
    ctx.lineWidth = 2;
    ctx.beginPath();
    
    data.forEach((point, index) => {
      const x = padding + (index / (data.length - 1)) * width;
      const y = canvas.height - padding - ((point.value - minValue) / valueRange) * height;
      
      if (index === 0) {
        ctx.moveTo(x, y);
      } else {
        ctx.lineTo(x, y);
      }
    });
    
    ctx.stroke();

    // Dibujar puntos
    ctx.fillStyle = '#1da1f2';
    data.forEach((point, index) => {
      const x = padding + (index / (data.length - 1)) * width;
      const y = canvas.height - padding - ((point.value - minValue) / valueRange) * height;
      
      ctx.beginPath();
      ctx.arc(x, y, 4, 0, 2 * Math.PI);
      ctx.fill();
    });
  }

  private getRandomColor(): string {
    const colors = ['#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF', '#FF9F40'];
    return colors[Math.floor(Math.random() * colors.length)];
  }
}
