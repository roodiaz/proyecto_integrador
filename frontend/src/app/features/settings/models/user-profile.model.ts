// Interface para los datos del perfil de usuario
export interface UserProfileData {
  fullName: string;
  email: string;
  birthDate?: Date | null;
  phone?: string;
  currency: string;
  theme: string;
  updateInterval: number;
  emailNotifications: boolean;
  pushNotifications: boolean;
}

// Interface para el formulario de perfil (incluye campos de contraseña)
export interface UserProfileFormData extends Omit<UserProfileData, 'emailNotifications' | 'pushNotifications'> {
  currentPassword?: string;
  newPassword?: string;
  confirmPassword?: string;
}

// Datos de ejemplo para el perfil de usuario
export const mockUserProfileData: UserProfileData = {
  fullName: 'Juan Pérez',
  email: 'juan.perez@ejemplo.com',
  birthDate: new Date(1990, 0, 1),
  phone: '+54 11 1234-5678',
  currency: 'USD',
  theme: 'system',
  updateInterval: 5,
  emailNotifications: true,
  pushNotifications: true
};

// Opciones para los selects
export const userProfileSelectOptions = {
  currencies: [
    { value: 'USD', viewValue: 'Dólar Estadounidense (USD)' },
    { value: 'ARS', viewValue: 'Peso Argentino (ARS)' }
  ],
  
  themes: [
    { value: 'light', viewValue: 'Claro' },
    { value: 'dark', viewValue: 'Oscuro' },
    { value: 'system', viewValue: 'Sistema' }
  ],
  
  updateIntervals: [
    { value: 1, viewValue: 'Cada minuto' },
    { value: 5, viewValue: 'Cada 5 minutos' },
    { value: 15, viewValue: 'Cada 15 minutos' }
  ]
};
