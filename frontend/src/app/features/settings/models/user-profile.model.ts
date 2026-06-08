import { ApiResponse } from '../../../core/models/api-response.model';

//REQUEST
export interface UpdateProfileRequest {
  userName: string;
  phone?: string;
  birthDate?: Date | null;
  currency: string;
  emailNotifications: boolean;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface RequestEmailChangeRequest {
  newEmail: string;
}

export interface ConfirmEmailChangeRequest {
  code: string;
}

export interface ProfileData {
  id: number;
  username: string;
  email: string;
  phone?: string;
  birthDate?: string | null;
  profileImageUrl?: string | null;

  settings: {
    currency: string;
    emailNotifications: boolean;
  };
}
export type ProfileResponse = ApiResponse<ProfileData>;

// Opciones para los selects
export const userProfileSelectOptions = {
  currencies: [
    { value: 'USD', viewValue: 'Dólar Estadounidense (USD)' },
    { value: 'ARS', viewValue: 'Peso Argentino (ARS)' }
  ],
  
  };
