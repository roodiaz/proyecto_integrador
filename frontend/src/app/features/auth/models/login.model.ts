import { ApiResponse } from '../../../core/models/api-response.model';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginData {
  tokens: {
    accessToken: string;
    refreshToken: string;
  };
}

export type LoginResponse = ApiResponse<LoginData>;