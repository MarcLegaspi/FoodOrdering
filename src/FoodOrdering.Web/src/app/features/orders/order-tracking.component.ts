import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { CustomerType, Order, OrderStatus, OrderType } from '../../core/models/api.models';
import { FoodOrderingApiService } from '../../core/services/food-ordering-api.service';
import { OrderLiveService } from '../../core/services/order-live.service';

@Component({
  selector: 'app-order-tracking',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="min-h-screen bg-slate-50 py-10 px-4 sm:px-6 lg:px-8">
      <div class="mx-auto max-w-2xl space-y-6">
        @if (loading()) {
          <div class="rounded-xl border border-slate-200 bg-white p-12 text-center text-slate-500 shadow-sm">
            <div class="inline-block h-8 w-8 animate-spin rounded-full border-4 border-solid border-slate-900 border-r-transparent"></div>
            <p class="mt-4 text-sm font-medium">Loading your order details...</p>
          </div>
        } @else if (errorMessage()) {
          <div class="rounded-xl border border-red-200 bg-red-50 p-6 text-center text-red-700 shadow-sm">
            <h2 class="text-base font-semibold">Unable to Load Order</h2>
            <p class="mt-1 text-sm">{{ errorMessage() }}</p>
            <div class="mt-4">
              <a
                [routerLink]="storeSlug() ? '/' + storeSlug() : '/'"
                class="inline-flex items-center rounded-lg bg-slate-900 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-800"
              >
                Return to Store
              </a>
            </div>
          </div>
        } @else if (order(); as o) {
          <!-- Header Banner -->
          <div class="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
            <div class="bg-slate-900 px-6 py-6 text-white sm:flex sm:items-center sm:justify-between">
              <div>
                <span class="inline-flex items-center rounded-md bg-slate-800 px-2.5 py-0.5 text-xs font-semibold text-slate-300">
                  {{ o.orderType === OrderType.Delivery ? 'Delivery' : 'Pickup' }}
                </span>
                @if (o.customerType === CustomerType.Guest) {
                  <span class="ml-2 inline-flex items-center rounded-md bg-amber-500/20 px-2.5 py-0.5 text-xs font-semibold text-amber-300 border border-amber-400/30">
                    Guest Order
                  </span>
                }
                <h1 class="mt-2 text-2xl font-bold tracking-tight">Order #{{ o.orderNumber }}</h1>
                <p class="mt-1 text-sm text-slate-400">
                  Placed at {{ o.storeName }} · {{ o.createdAtUtc | date:'short' }}
                </p>
              </div>
              <div class="mt-4 sm:mt-0 sm:text-right">
                <span
                  class="inline-flex items-center rounded-full px-3 py-1 text-xs font-bold"
                  [ngClass]="statusPillClass(o.status)"
                >
                  {{ statusLabel(o.status) }}
                </span>
              </div>
            </div>

            <!-- Stepper Progress Bar (unless cancelled) -->
            @if (o.status !== OrderStatus.Cancelled) {
              <div class="border-b border-slate-100 bg-slate-50/50 px-6 py-6">
                <div class="grid grid-cols-4 gap-2 text-center text-xs font-medium">
                  <!-- Pending -->
                  <div class="space-y-2">
                    <div
                      class="mx-auto flex h-8 w-8 items-center justify-center rounded-full text-xs font-bold transition-all"
                      [ngClass]="stepCircleClass(o.status, OrderStatus.Pending)"
                    >
                      1
                    </div>
                    <span [ngClass]="stepTextClass(o.status, OrderStatus.Pending)">Received</span>
                  </div>

                  <!-- Preparing -->
                  <div class="space-y-2">
                    <div
                      class="mx-auto flex h-8 w-8 items-center justify-center rounded-full text-xs font-bold transition-all"
                      [ngClass]="stepCircleClass(o.status, OrderStatus.Preparing)"
                    >
                      2
                    </div>
                    <span [ngClass]="stepTextClass(o.status, OrderStatus.Preparing)">Preparing</span>
                  </div>

                  <!-- Ready -->
                  <div class="space-y-2">
                    <div
                      class="mx-auto flex h-8 w-8 items-center justify-center rounded-full text-xs font-bold transition-all"
                      [ngClass]="stepCircleClass(o.status, OrderStatus.Ready)"
                    >
                      3
                    </div>
                    <span [ngClass]="stepTextClass(o.status, OrderStatus.Ready)">
                      {{ o.orderType === OrderType.Delivery ? 'Out for Delivery' : 'Ready for Pickup' }}
                    </span>
                  </div>

                  <!-- Completed -->
                  <div class="space-y-2">
                    <div
                      class="mx-auto flex h-8 w-8 items-center justify-center rounded-full text-xs font-bold transition-all"
                      [ngClass]="stepCircleClass(o.status, OrderStatus.Completed)"
                    >
                      ✓
                    </div>
                    <span [ngClass]="stepTextClass(o.status, OrderStatus.Completed)">Completed</span>
                  </div>
                </div>
              </div>
            } @else {
              <div class="border-b border-red-100 bg-red-50 px-6 py-4 text-center text-sm font-medium text-red-700">
                This order was cancelled.
              </div>
            }

            <!-- Customer & Fulfillment Information -->
            <div class="grid grid-cols-1 divide-y divide-slate-100 sm:grid-cols-2 sm:divide-y-0 sm:divide-x border-b border-slate-100 p-6 text-sm">
              <div class="space-y-1 pr-4">
                <span class="text-xs font-bold tracking-wider text-slate-400 uppercase">Customer Details</span>
                <p class="font-semibold text-slate-900">{{ o.customerName || 'Customer' }}</p>
                <p class="text-slate-600">📞 {{ o.customerPhone }}</p>
                @if (o.customerEmail) {
                  <p class="text-slate-500 text-xs">✉ {{ o.customerEmail }}</p>
                }
              </div>
              <div class="space-y-1 pt-4 sm:pt-0 sm:pl-4">
                <span class="text-xs font-bold tracking-wider text-slate-400 uppercase">Fulfillment</span>
                <p class="font-semibold text-slate-900">
                  {{ o.orderType === OrderType.Delivery ? 'Home Delivery' : 'Store Pickup' }}
                </p>
                @if (o.orderType === OrderType.Delivery && o.deliveryAddress) {
                  <p class="text-slate-600">{{ o.deliveryAddress }}</p>
                }
                @if (o.notes) {
                  <p class="text-xs text-slate-500 italic mt-1">"{{ o.notes }}"</p>
                }
              </div>
            </div>

            <!-- Items list -->
            <div class="p-6">
              <h2 class="text-sm font-bold tracking-wider text-slate-400 uppercase mb-4">Order Items</h2>
              <div class="divide-y divide-slate-100">
                @for (item of o.items; track $index) {
                  <div class="py-3 flex items-start justify-between">
                    <div>
                      <div class="flex items-center gap-2">
                        <span class="font-semibold text-slate-900">{{ item.quantity }}× {{ item.itemName }}</span>
                        <span class="text-xs text-slate-500">({{ item.unitPrice | currency }} each)</span>
                      </div>
                      @if (item.options && item.options.length > 0) {
                        <ul class="mt-1 space-y-0.5 pl-3 border-l-2 border-slate-200 text-xs text-slate-600">
                          @for (opt of item.options; track $index) {
                            <li>
                              {{ opt.groupName }}: <span class="font-medium text-slate-800">{{ opt.optionName }}</span>
                              @if (opt.priceDelta > 0) {
                                <span class="text-slate-500">(+{{ opt.priceDelta | currency }})</span>
                              }
                            </li>
                          }
                        </ul>
                      }
                    </div>
                    <span class="font-semibold text-slate-900">{{ item.lineTotal | currency }}</span>
                  </div>
                }
              </div>

              <!-- Total summary -->
              <div class="mt-6 border-t border-slate-200 pt-4 flex items-center justify-between text-base font-bold text-slate-900">
                <span>Total Amount</span>
                <span>{{ o.totalAmount | currency }}</span>
              </div>
            </div>
          </div>

          <!-- Bottom Actions -->
          <div class="flex items-center justify-between gap-4">
            <a
              [routerLink]="'/' + o.storeSlug"
              class="inline-flex items-center rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-semibold text-slate-700 shadow-sm hover:bg-slate-50"
            >
              ← Order More from {{ o.storeName }}
            </a>
            <span class="text-xs text-slate-400">
              Live updates enabled
            </span>
          </div>
        }
      </div>
    </div>
  `
})
export class OrderTrackingComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(FoodOrderingApiService);
  private readonly orderLive = inject(OrderLiveService);

  readonly order = signal<Order | null>(null);
  readonly loading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);
  readonly storeSlug = signal<string | null>(null);

  readonly OrderStatus = OrderStatus;
  readonly OrderType = OrderType;
  readonly CustomerType = CustomerType;

  private orderSub: Subscription | null = null;
  private currentPublicId: string | null = null;

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const publicId = params.get('publicId');
      const slug = params.get('storeSlug');
      this.storeSlug.set(slug);

      if (!publicId) {
        this.loading.set(false);
        this.errorMessage.set('Missing order ID.');
        return;
      }

      this.currentPublicId = publicId;
      this.loadOrder(publicId);
      void this.orderLive.joinOrder(publicId);
    });

    this.orderSub = this.orderLive.orderUpdated$.subscribe((updated) => {
      if (this.currentPublicId && updated.publicId.toLowerCase() === this.currentPublicId.toLowerCase()) {
        this.order.set(updated);
      }
    });
  }

  ngOnDestroy(): void {
    if (this.currentPublicId) {
      void this.orderLive.leaveOrder(this.currentPublicId);
    }
    this.orderSub?.unsubscribe();
  }

  loadOrder(publicId: string): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api.getOrderByPublicId(publicId).subscribe({
      next: (order) => {
        this.order.set(order);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err?.error?.error ?? 'Unable to find this order.');
      }
    });
  }

  statusLabel(status: OrderStatus): string {
    switch (status) {
      case OrderStatus.Pending:
        return 'Pending Confirmation';
      case OrderStatus.Confirmed:
        return 'Confirmed';
      case OrderStatus.Preparing:
        return 'Preparing in Kitchen';
      case OrderStatus.Ready:
        return 'Ready';
      case OrderStatus.Completed:
        return 'Completed';
      case OrderStatus.Cancelled:
        return 'Cancelled';
      default:
        return 'Unknown';
    }
  }

  statusPillClass(status: OrderStatus): string {
    switch (status) {
      case OrderStatus.Pending:
        return 'bg-amber-100 text-amber-900';
      case OrderStatus.Confirmed:
        return 'bg-sky-100 text-sky-900';
      case OrderStatus.Preparing:
        return 'bg-indigo-100 text-indigo-900';
      case OrderStatus.Ready:
        return 'bg-emerald-100 text-emerald-900';
      case OrderStatus.Completed:
        return 'bg-slate-200 text-slate-800';
      case OrderStatus.Cancelled:
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-slate-100 text-slate-700';
    }
  }

  stepCircleClass(currentStatus: OrderStatus, step: OrderStatus): string {
    const isPassedOrCurrent = currentStatus >= step;
    if (isPassedOrCurrent) {
      return 'bg-slate-900 text-white';
    }
    return 'bg-slate-200 text-slate-400';
  }

  stepTextClass(currentStatus: OrderStatus, step: OrderStatus): string {
    const isPassedOrCurrent = currentStatus >= step;
    return isPassedOrCurrent ? 'text-slate-900 font-semibold' : 'text-slate-400';
  }
}
