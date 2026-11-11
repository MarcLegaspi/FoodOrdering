import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AddStoreMemberRequest,
  Category,
  CreateStoreRequest,
  MenuItem,
  Order,
  OrderStatus,
  PlaceOrderRequest,
  SaveMenuItemRequest,
  Store,
  StoreMember,
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

  updateOrderStatus(publicId: string, status: OrderStatus): Observable<Order> {
    return this.http.patch<Order>(`${this.api}/orders/${publicId}/status`, { status });
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
