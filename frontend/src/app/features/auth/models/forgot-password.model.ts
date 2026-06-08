// REQUEST
export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  code: string;
  newPassword: string;
  confirmPassword: string;
}

// RESPONSE
export interface ForgotPasswordResponse {
  email: string;
  emailSent: boolean;
}
