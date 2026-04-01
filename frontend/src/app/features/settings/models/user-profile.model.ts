// Interface para los datos del perfil de usuario
export interface UserProfileData {
  fullName: string;
  email: string;
  birthDate?: Date | null;
  phone?: string;
  currency: string;
  emailNotifications: boolean;
}

// Interface para el perfil de usuario
export interface UserPlans extends UserProfileData {
  currentPlan: string;
  cardLastFour: string;
  cardBrand: string;
  nextBillingDate: Date;
}

// Interface para el formulario de perfil (incluye campos de contraseña)
export interface UserProfileFormData extends Omit<UserProfileData, 'emailNotifications' | 'pushNotifications'> {
  currentPassword?: string;
  newPassword?: string;
  confirmPassword?: string;
}

// Datos de ejemplo para el perfil de usuario
export const mockUserProfileData: UserPlans = {
  fullName: 'Juan Pérez',
  email: 'juan.perez@ejemplo.com',
  birthDate: new Date(1990, 0, 1),
  phone: '+54 11 1234-5678',
  currency: 'USD',
  emailNotifications: true,
  currentPlan: 'premium',
  cardLastFour: '4242',
  cardBrand: 'visa',
  nextBillingDate: new Date(2026, 3, 15)
};

// Opciones para los selects
export const userProfileSelectOptions = {
  currencies: [
    { value: 'USD', viewValue: 'Dólar Estadounidense (USD)' },
    { value: 'ARS', viewValue: 'Peso Argentino (ARS)' }
  ],
  
  plans: [
    { value: 'free', viewValue: 'Plan Gratuito' },
    { value: 'premium', viewValue: 'Plan Premium ($19.99/mes)' },
  ]
};
