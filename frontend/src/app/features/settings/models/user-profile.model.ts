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
export interface UserProfile extends UserProfileData {
}

// Interface para el formulario de perfil (incluye campos de contraseña)
export interface UserProfileFormData extends Omit<UserProfileData, 'emailNotifications' | 'pushNotifications'> {
  currentPassword?: string;
  newPassword?: string;
  confirmPassword?: string;
}

// Datos de ejemplo para el perfil de usuario
export const mockUserProfileData: UserProfile = {
  fullName: 'Juan Pérez',
  email: 'juan.perez@ejemplo.com',
  birthDate: new Date(1990, 0, 1),
  phone: '+54 11 1234-5678',
  currency: 'USD',
  emailNotifications: true
};

// Opciones para los selects
export const userProfileSelectOptions = {
  currencies: [
    { value: 'USD', viewValue: 'Dólar Estadounidense (USD)' },
    { value: 'ARS', viewValue: 'Peso Argentino (ARS)' }
  ],
  
  };
