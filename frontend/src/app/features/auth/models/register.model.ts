// REQUEST
export interface RegisterRequest {
  fullName: string;
  email: string;
  phone?: string;
  password: string;
  confirmPassword: string;
}

export interface ResendCodeRequest {
  email: string;
}

export interface VerifyRequest {
  email: string;
  code: string;
}

// RESPONSE
export interface RegisterResponse {
  requiresVerification: boolean;
  email: string;
  emailSent: boolean;
}

