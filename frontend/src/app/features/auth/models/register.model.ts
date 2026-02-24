export enum PlanType {
  FREE = 'free',
  PREMIUM = 'premium'
}

export interface RegisterFormData {
  fullName: string;
  email: string;
  password: string;
  confirmPassword: string;
  planType: PlanType;
  paymentInfo?: PaymentInfo;
}

export interface PaymentInfo {
  cardNumber: string;
  cardHolder: string;
  expiryMonth: number; // 1-12
  expiryYear: number; // full year (e.g., 2025)
  cvv: string;
}