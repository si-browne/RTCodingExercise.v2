export enum PlateStatus {
  ForSale = 0,
  Reserved = 1,
  Sold = 2
}

export interface Plate {
  id: string;
  registration?: string;
  letters?: string;
  numbers?: number;
  purchasePrice: number;
  salePrice: number;
  status: PlateStatus;
  reservedDate?: Date;
  soldDate?: Date;
  soldPrice?: number;
  promoCodeUsed?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface RevenueStatistics {
  totalRevenue: number;
  totalProfit: number;
  platesSold: number;
  averageProfitMargin: number;
}
