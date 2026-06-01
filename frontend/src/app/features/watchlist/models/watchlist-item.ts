import { ApiResponse } from "../../../core/models/api-response.model";

export interface FavoriteItem {
  id: number;
  symbol: string;
  name: string;
  price: number;
  variationPercent: number;
}

export interface FavoriteListData {
  items: FavoriteItem[];
  total: number;
  page: number;
  pageSize: number;
  currentFavorites: number;
  maxFavorites: number;
}

export interface FavoriteFilter {
  page: number;
  pageSize: number;
  search?: string;
}

export type FavoriteListResponse = ApiResponse<FavoriteListData>;