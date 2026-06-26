export interface SaleItem {
  id?: string;
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  totalAmount: number;
  isCancelled: boolean;
}

export interface Sale {
  id: string;
  saleNumber: string;
  saleDate: string;
  customerId: string;
  customerName: string;
  branchId: string;
  branchName: string;
  items: SaleItem[];
  totalAmount: number;
  isCancelled: boolean;
}

export interface CreateSaleRequest {
  customerId: string;
  customerName: string;
  branchId: string;
  branchName: string;
  items: CreateSaleItemRequest[];
}

export interface CreateSaleItemRequest {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
}

export interface PaginatedResponse<T> {
  success: boolean;
  message: string;
  data: T[];
  currentPage: number;
  totalPages: number;
  totalCount: number;
}

export interface ApiResponseWithData<T> {
  success: boolean;
  message: string;
  data: T;
}
