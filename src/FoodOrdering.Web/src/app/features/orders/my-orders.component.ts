import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { Order, OrderStatus, OrderType, orderStatusLabel, orderTypeLabel } from '../../core/models/api.models';
import { FoodOrderingApiService } from '../../core/services/food-ordering-api.service';
import { OrderLiveService, upsertOrderList } from '../../core/services/order-live.service';
import { TenantService } from '../../core/tenant/tenant.service';

@Component({
  selector: 'app-my-orders',
  imports: [CurrencyPipe, DatePipe, RouterLink],
  template: `
    <div class="space-y-6">
      <div>
        <div class="flex items-center gap-2.5">
          <h1 class="text-2xl font-bold text-slate-900">My Orders</h1>
          @if (orderLive.connectionState() === 'connected') {
            <span class="rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-medium text-emerald-700">
              Live
            </span>
          }
        </div>
        <p class="mt-1 text-sm text-slate-500">Track your orders across all stores.</p>
      </div>

      @if (loading()) {
        <p class="text-sm text-slate-500">Loading your orders...</p>
      } @else if (orders().length === 0) {
        <div class="rounded-xl border border-dashed border-slate-300 bg-white p-8 text-center">
          <p class="text-sm text-slate-500">You haven't placed any orders yet.</p>
          <a routerLink="/" class="mt-3 inline-block text-sm font-semibold text-slate-900 underline">
            Browse stores
          </a>
        </div>
      } @else {
        <div class="space-y-4">
          @for (order of orders(); track order.publicId) {
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
              <div class="flex flex-wrap items-center justify-between gap-2">
                <div>
                  <a [routerLink]="['/', order.storeSlug]" class="font-bold text-slate-900 hover:underline">
                    {{ order.storeName }}
                  </a>
                  <span class="ml-2 text-xs text-slate-400">
                    {{ order.orderNumber }} · {{ order.createdAtUtc | date: 'short' }}
                  </span>
                </div>

                <div class="flex items-center gap-3">
                  <span class="rounded-full bg-amber-50 px-2.5 py-0.5 text-xs font-medium text-amber-800">
                    {{ orderTypeText(order.orderType) }}
                  </span>
                  <span class="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-700">
                    {{ statusText(order.status) }}
                  </span>
                  <span class="font-bold text-slate-900">{{ order.totalAmount | currency }}</span>
                </div>
              </div>

              <div class="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-slate-500">
                <span>Phone: {{ order.customerPhone }}</span>
                @if (order.deliveryAddress) {
                  <span>Deliver to: {{ order.deliveryAddress }}</span>
                }
              </div>

              <ul class="mt-3 space-y-1 border-t border-slate-100 pt-3 text-sm text-slate-600">
                @for (item of order.items; track item.menuItemPublicId) {
                  <li class="flex justify-between">
                    <span>{{ item.quantity }}× {{ item.itemName }}</span>
                    <span>{{ item.lineTotal | currency }}</span>
                  </li>
                }
              </ul>

              @if (order.notes) {
                <p class="mt-2 text-xs text-slate-500">Notes: {{ order.notes }}</p>
              }

              @if (order.status === OrderStatus.Pending) {
                <div class="mt-4 flex justify-end">
                  <button
                    type="button"
                    (click)="cancelOrder(order.publicId)"
                    class="rounded-md border border-red-200 px-3 py-1 text-xs font-medium text-red-700 hover:bg-red-50"
                  >
                    Cancel Order
                  </button>
                </div>
              }
            </div>
          }
        </div>
      }
    </div>
  `
})
export class MyOrdersComponent implements OnInit, OnDestroy {
  private readonly api = inject(FoodOrderingApiService);
  private readonly tenant = inject(TenantService);
  readonly orderLive = inject(OrderLiveService);

  private readonly liveSubscriptions = new Subscription();

  readonly OrderStatus = OrderStatus;
  readonly orders = signal<Order[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.tenant.setStoreSlug(null);
    this.loadOrders();
    void this.orderLive.connectForCustomer();

    this.liveSubscriptions.add(
      this.orderLive.orderUpdated$.subscribe((order) =>
        this.orders.update((list) => upsertOrderList(list, order))
      )
    );

    this.liveSubscriptions.add(
      this.orderLive.reconnected$.subscribe(() => this.loadOrders())
    );
  }

  ngOnDestroy(): void {
    this.liveSubscriptions.unsubscribe();
  }

  statusText(status: OrderStatus): string {
    return orderStatusLabel(status);
  }

  orderTypeText(type: OrderType): string {
    return orderTypeLabel(type);
  }

  cancelOrder(publicId: string): void {
    this.api.cancelMyOrder(publicId).subscribe({
      next: () => this.loadOrders()
    });
  }

  private loadOrders(): void {
    this.api.getMyOrders().subscribe({
      next: (list) => {
        this.orders.set(list);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
