export interface ApiResponse<T = any> {
  success: boolean;
  message: string;
  code?: string;
  data: T | null;
}