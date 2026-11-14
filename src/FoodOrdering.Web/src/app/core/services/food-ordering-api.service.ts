import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AddStoreMemberRequest,
  Category,
  CreateReviewRequest,
  CreateStoreRequest,
  MenuItem,
  Order,
  OrderReview,
  OrderStatus,
  PaymentCollectionMethod,
  PlaceOrderRequest,
  PromoCode,
  CreatePromoCodeRequest,
  ValidatePromoResponse,
  SaveMenuItemRequest,
  Store,
  StoreMember,
  StoreReviewSummary,
  UpdateStoreProfileRequest
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class FoodOrderingApiService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/api`;

  // Stores
  getStores(): Observable<Store[]> {
    return this.http.get<Store[]>(`${this.api}/stores`);
  }

  getStoreBySlug(slug: string): Observable<Store> {
    return this.http.get<Store>(`${this.api}/stores/${encodeURIComponent(slug)}`);
  }

  createStore(payload: CreateStoreRequest): Observable<Store> {
    return this.http.post<Store>(`${this.api}/stores`, payload);
  }

  updateCurrentStoreSettings(isAcceptingOrders: boolean): Observable<Store> {
    return this.http.patch<Store>(`${this.api}/stores/current/settings`, { isAcceptingOrders });
  }

  updateCurrentStoreProfile(payload: UpdateStoreProfileRequest): Observable<Store> {
    return this.http.put<Store>(`${this.api}/stores/current/profile`, payload);
  }

  // Categories (uses X-Store-Slug via tenantInterceptor)
  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.api}/categories`);
  }

  createCategory(name: string, sortOrder: number): Observable<Category> {
    return this.http.post<Category>(`${this.api}/categories`, { name, sortOrder });
  }

  updateCategory(publicId: string, name: string, sortOrder: number, isActive = true): Observable<Category> {
    return this.http.put<Category>(`${this.api}/categories/${publicId}`, { name, sortOrder, isActive });
  }

  reorderCategories(orderedPublicIds: string[]): Observable<Category[]> {
    return this.http.put<Category[]>(`${this.api}/categories/reorder`, { orderedPublicIds });
  }

  deleteCategory(publicId: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/categories/${publicId}`);
  }

  // Menu Items (uses X-Store-Slug via tenantInterceptor)
  getMenuItems(availableOnly?: boolean): Observable<MenuItem[]> {
    let params = new HttpParams();
    if (availableOnly !== undefined) {
      params = params.set('availableOnly', availableOnly);
    }
    return this.http.get<MenuItem[]>(`${this.api}/menu-items`, { params });
  }

  createMenuItem(payload: SaveMenuItemRequest): Observable<MenuItem> {
    return this.http.post<MenuItem>(`${this.api}/menu-items`, payload);
  }

  updateMenuItem(publicId: string, payload: SaveMenuItemRequest): Observable<MenuItem> {
    return this.http.put<MenuItem>(`${this.api}/menu-items/${publicId}`, payload);
  }

  deleteMenuItem(publicId: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/menu-items/${publicId}`);
  }

  uploadMenuItemImage(file: File): Observable<{ imageUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ imageUrl: string }>(`${this.api}/menu-items/upload-image`, formData);
  }

  // Orders
  placeOrder(payload: PlaceOrderRequest): Observable<Order> {
    return this.http.post<Order>(`${this.api}/orders`, payload);
  }

  getOrderByPublicId(publicId: string): Observable<Order> {
    return this.http.get<Order>(`${this.api}/orders/${publicId}`);
  }

  getMyOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(`${this.api}/orders/my`);
  }

  cancelMyOrder(publicId: string): Observable<Order> {
    return this.http.post<Order>(`${this.api}/orders/${publicId}/cancel`, {});
  }

  getStoreOrders(status?: OrderStatus): Observable<Order[]> {
    let params = new HttpParams();
    if (status !== undefined) {
      params = params.set('status', status);
    }
    return this.http.get<Order[]>(`${this.api}/orders`, { params });
  }

  /**
   * Advances order status. When completing delivery orders, requires deliveryPin and paymentCollectedVia.
   */
  updateOrderStatus(
    publicId: string,
    status: OrderStatus,
    paymentCollectedVia?: PaymentCollectionMethod | null,
    deliveryPin?: string | null
  ): Observable<Order> {
    return this.http.patch<Order>(`${this.api}/orders/${publicId}/status`, {
      status,
      paymentCollectedVia,
      deliveryPin
    });
  }

  /**
   * Assigns rider name and phone to a delivery order, records dispatch time, and sets status to OutForDelivery.
   */
  assignDriver(publicId: string, driverName: string, driverPhone: string): Observable<Order> {
    return this.http.post<Order>(`${this.api}/orders/${publicId}/assign-driver`, {
      driverName,
      driverPhone
    });
  }

  // Promos
  validatePromo(code: string, subtotal: number): Observable<ValidatePromoResponse> {
    return this.http.post<ValidatePromoResponse>(`${this.api}/promos/validate`, { code, subtotal });
  }

  getStorePromos(): Observable<PromoCode[]> {
    return this.http.get<PromoCode[]>(`${this.api}/promos`);
  }

  createPromo(payload: CreatePromoCodeRequest): Observable<PromoCode> {
    return this.http.post<PromoCode>(`${this.api}/promos`, payload);
  }

  togglePromo(publicId: string): Observable<PromoCode> {
    return this.http.patch<PromoCode>(`${this.api}/promos/${publicId}/toggle`, {});
  }

  deletePromo(publicId: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/promos/${publicId}`);
  }

  // Reviews
  createOrderReview(orderPublicId: string, payload: CreateReviewRequest): Observable<OrderReview> {
    return this.http.post<OrderReview>(`${this.api}/orders/${orderPublicId}/review`, payload);
  }

  getStoreReviews(slug: string): Observable<StoreReviewSummary> {
    return this.http.get<StoreReviewSummary>(`${this.api}/stores/${encodeURIComponent(slug)}/reviews`);
  }

  // Store Members
  getStoreMembers(): Observable<StoreMember[]> {
    return this.http.get<StoreMember[]>(`${this.api}/store-members`);
  }

  addStoreMember(payload: AddStoreMemberRequest): Observable<StoreMember> {
    return this.http.post<StoreMember>(`${this.api}/store-members`, payload);
  }

  removeStoreMember(userId: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/store-members/${userId}`);
  }
}
