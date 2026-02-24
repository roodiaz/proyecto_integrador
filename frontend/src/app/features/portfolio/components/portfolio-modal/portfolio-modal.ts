import { Component, Input, Output, EventEmitter, OnChanges, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PortfolioOperation } from '../../models/portfolio-operation';

export interface BuyData {
  ticker: string;
  quantity: number;
  price: number;
}

@Component({
  selector: 'app-portfolio-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './portfolio-modal.html',
  styleUrl: './portfolio-modal.css'
})
export class PortfolioModal implements OnInit, OnChanges {
  @Input() isVisible: boolean = false;
  @Input() mode: 'buy' | 'sell' = 'buy';
  @Input() operation: PortfolioOperation | null = null;
  @Output() closeModal = new EventEmitter<void>();
  @Output() buyComplete = new EventEmitter<BuyData>();
  @Output() sellComplete = new EventEmitter<string>();

  // Buy form data
  buyTicker: string = '';
  buyQuantity: number = 0;
  buyPrice: number = 0;

  // Sell form data
  sellPrice: number = 0;
  sellQuantity: number = 0;

  // Market price (simulated)
  marketPrice: number = 0;

  constructor() {}

  ngOnInit() {
    // When mode changes, update market price if we have a ticker
    this.updateMarketPrice();
  }

  ngOnChanges() {
    this.updateMarketPrice();
  }

  private updateMarketPrice(): void {
    if (this.mode === 'buy' && this.buyTicker.trim()) {
      // Simulate getting market price for ticker
      this.marketPrice = this.getSimulatedPrice(this.buyTicker);
    } else if (this.mode === 'sell' && this.operation) {
      this.marketPrice = this.operation.currentPrice;
    }
  }

  onTickerChange(ticker: string): void {
    this.buyTicker = ticker;
    this.updateMarketPrice();
  }

  private getSimulatedPrice(ticker: string): number {
    // Simulated prices for common tickers
    const prices: { [key: string]: number } = {
      'AAPL': 175.50,
      'GOOGL': 142.30,
      'MSFT': 380.75,
      'TSLA': 245.80,
      'AMZN': 145.20,
      'META': 325.40,
      'NVDA': 485.60
    };
    
    const upperTicker = ticker.toUpperCase();
    return prices[upperTicker] || 100.00; // Default price if not found
  }

  get modalTitle(): string {
    return this.mode === 'buy' ? 'Nueva Operación' : 'Vender Activo';
  }

  get currentBalance(): number {
    return 10000; // Saldo actual fijo para ejemplo
  }

  get canBuy(): boolean {
    return !!(this.buyTicker.trim() && 
           this.buyQuantity > 0 && 
           this.marketPrice > 0 &&
           (this.buyQuantity * this.marketPrice) <= this.currentBalance);
  }

  get canSell(): boolean {
    return !!(this.sellQuantity > 0 && 
               this.operation !== null && 
               this.sellQuantity <= this.operation.quantity);
  }

  get totalCost(): number {
    return this.buyQuantity * this.marketPrice;
  }

  get sellProfit(): number {
    if (!this.operation || !this.sellQuantity) return 0;
    const profit = (this.operation.currentPrice - this.operation.buyPrice) * this.sellQuantity;
    return profit;
  }

  get sellProfitPercent(): number {
    if (!this.operation || !this.sellQuantity) return 0;
    const costPerShare = this.operation.buyPrice;
    const currentPricePerShare = this.operation.currentPrice;
    return ((currentPricePerShare - costPerShare) / costPerShare) * 100;
  }

  switchMode(newMode: 'buy' | 'sell'): void {
    this.mode = newMode;
    this.resetForms();
  }

  onClose(): void {
    this.closeModal.emit();
    this.resetForms();
  }

  onBuy(): void {
    if (!this.canBuy) return;

    const buyData: BuyData = {
      ticker: this.buyTicker.trim().toUpperCase(),
      quantity: this.buyQuantity,
      price: this.marketPrice
    };

    this.buyComplete.emit(buyData);
    this.resetForms();
  }

  onSell(): void {
    if (!this.canSell || !this.operation) return;

    this.sellComplete.emit(this.operation.id);
    this.resetForms();
  }

  private resetForms(): void {
    this.buyTicker = '';
    this.buyQuantity = 0;
    this.buyPrice = 0;
    this.sellPrice = 0;
    this.sellQuantity = 0;
    this.marketPrice = 0;
  }
}
