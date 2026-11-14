import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { CustomerType, Order, OrderStatus, OrderType, PaymentMethod, PaymentStatus } from '../../core/models/api.models';
import { FoodOrderingApiService } from '../../core/services/food-ordering-api.service';
import { OrderLiveService } from '../../core/services/order-live.service';

@Component({
  selector: 'app-order-tracking',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
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
                      [ngClass]="stepCircleClass(o.status, 'received')"
                    >
                      1
                    </div>
                    <span [ngClass]="stepTextClass(o.status, 'received')">Received</span>
                  </div>

                  <!-- Preparing -->
                  <div class="space-y-2">
                    <div
                      class="mx-auto flex h-8 w-8 items-center justify-center rounded-full text-xs font-bold transition-all"
                      [ngClass]="stepCircleClass(o.status, 'preparing')"
                    >
                      2
                    </div>
                    <span [ngClass]="stepTextClass(o.status, 'preparing')">Preparing</span>
                  </div>

                  <!-- Ready / Out For Delivery -->
                  <div class="space-y-2">
                    <div
                      class="mx-auto flex h-8 w-8 items-center justify-center rounded-full text-xs font-bold transition-all"
                      [ngClass]="stepCircleClass(o.status, 'transit')"
                    >
                      3
                    </div>
                    <span [ngClass]="stepTextClass(o.status, 'transit')">
                      {{ o.orderType === OrderType.Delivery ? (o.status === OrderStatus.OutForDelivery ? 'Out for Delivery 🛵' : (isStepDone(o.status, 'transit') ? 'Dispatched' : 'Ready for Dispatch')) : 'Ready for Pickup 🛍️' }}
                    </span>
                  </div>

                  <!-- Completed -->
                  <div class="space-y-2">
                    <div
                      class="mx-auto flex h-8 w-8 items-center justify-center rounded-full text-xs font-bold transition-all"
                      [ngClass]="stepCircleClass(o.status, 'completed')"
                    >
                      ✓
                    </div>
                    <span [ngClass]="stepTextClass(o.status, 'completed')">
                      {{ o.orderType === OrderType.Delivery ? 'Delivered 🎉' : 'Completed 🎉' }}
                    </span>
                  </div>
                </div>
              </div>
            } @else {
              <div class="border-b border-red-100 bg-red-50 px-6 py-4 text-center text-sm font-medium text-red-700">
                This order was cancelled.
              </div>
            }

            <!-- Delivery PIN Banner (Customer security proof of delivery) -->
            @if (o.orderType === OrderType.Delivery && o.deliveryPin && o.status !== OrderStatus.Completed && o.status !== OrderStatus.Cancelled) {
              <div class="border-b border-amber-200 bg-amber-50/80 px-6 py-4">
                <div class="flex flex-col items-center sm:flex-row sm:justify-between gap-3 text-center sm:text-left">
                  <div>
                    <div class="flex items-center justify-center sm:justify-start gap-1.5 font-bold text-amber-900 text-sm">
                      <span>🔐</span> Your Delivery PIN
                    </div>
                    <p class="text-xs text-amber-800 mt-0.5">
                      Give this 4-digit PIN to your rider upon arrival to safely verify and receive your food.
                    </p>
                  </div>
                  <div class="rounded-xl border border-amber-300 bg-white px-5 py-2 text-2xl font-black tracking-widest text-slate-900 shadow-xs">
                    {{ o.deliveryPin }}
                  </div>
                </div>
              </div>
            }

            <!-- Driver Card (When driver is assigned or order is out for delivery) -->
            @if (o.driverName) {
              <div class="border-b border-purple-200 bg-purple-50/70 px-6 py-4">
                <div class="flex flex-wrap items-center justify-between gap-3">
                  <div class="flex items-center gap-3">
                    <div class="flex h-10 w-10 items-center justify-center rounded-full bg-purple-600 text-lg text-white shadow-xs">
                      🛵
                    </div>
                    <div>
                      <div class="text-[10px] font-bold uppercase tracking-wider text-purple-700">Delivery Rider</div>
                      <div class="text-sm font-bold text-slate-900">{{ o.driverName }}</div>
                      @if (o.dispatchedAtUtc) {
                        <div class="text-[11px] text-slate-500">
                          Dispatched at {{ o.dispatchedAtUtc | date:'shortTime' }}
                        </div>
                      }
                    </div>
                  </div>
                  @if (o.driverPhone) {
                    <a
                      [href]="'tel:' + o.driverPhone"
                      class="inline-flex items-center gap-1.5 rounded-lg bg-purple-600 px-3.5 py-1.5 text-xs font-semibold text-white shadow-xs hover:bg-purple-700 transition"
                    >
                      📞 Call Rider ({{ o.driverPhone }})
                    </a>
                  }
                </div>
              </div>
            }

            <!-- Customer, Fulfillment & Payment Information -->
            <div class="grid grid-cols-1 divide-y divide-slate-100 sm:grid-cols-3 sm:divide-y-0 sm:divide-x border-b border-slate-100 p-6 text-sm">
              <div class="space-y-1 sm:pr-4">
                <span class="text-xs font-bold tracking-wider text-slate-400 uppercase">Customer Details</span>
                <p class="font-semibold text-slate-900">{{ o.customerName || 'Customer' }}</p>
                <p class="text-slate-600">📞 {{ o.customerPhone }}</p>
                @if (o.customerEmail) {
                  <p class="text-slate-500 text-xs">✉ {{ o.customerEmail }}</p>
                }
              </div>
              <div class="space-y-1 pt-4 sm:pt-0 sm:px-4">
                <span class="text-xs font-bold tracking-wider text-slate-400 uppercase">Fulfillment</span>
                <p class="font-semibold text-slate-900">
                  {{ o.orderType === OrderType.Delivery ? 'Home Delivery' : 'Store Pickup' }}
                </p>
                @if (o.estimatedDeliveryTime) {
                  <p class="text-xs font-medium text-slate-700">⏱ Est. Time: {{ o.estimatedDeliveryTime }}</p>
                }
                @if (o.orderType === OrderType.Delivery && o.deliveryAddress) {
                  <p class="text-slate-600">{{ o.deliveryAddress }}</p>
                }
                @if (o.orderType === OrderType.Delivery && o.deliveryLandmark) {
                  <p class="text-xs text-amber-800 bg-amber-50 rounded px-1.5 py-0.5 inline-block">
                    🏢 Landmark: {{ o.deliveryLandmark }}
                  </p>
                }
                @if (o.notes) {
                  <p class="text-xs text-slate-500 italic mt-1">"{{ o.notes }}"</p>
                }
              </div>
              <div class="space-y-1.5 pt-4 sm:pt-0 sm:pl-4">
                <div class="flex items-center justify-between">
                  <span class="text-xs font-bold tracking-wider text-slate-400 uppercase">Payment</span>
                  <span
                    class="rounded-full px-2 py-0.5 text-[10px] font-bold"
                    [class]="o.paymentStatus === PaymentStatus.Paid ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-800'"
                  >
                    {{ o.paymentStatus === PaymentStatus.Paid ? 'Paid' : 'Payment Due' }}
                  </span>
                </div>
                @if (o.paymentMethod === PaymentMethod.PayOnDelivery) {
                  <p class="font-semibold text-slate-800">💵 Pay on Delivery</p>
                  <p class="text-xs text-slate-600">Cash or scan Rider's GCash QR</p>
                  @if (o.cashChangeFor) {
                    <p class="text-xs text-emerald-700 font-medium">
                      Need change for: {{ o.cashChangeFor | currency }}
                    </p>
                  }
                } @else if (o.paymentMethod === PaymentMethod.GCashOnline) {
                  <p class="font-semibold text-blue-900">📱 GCash Online</p>
                  <p class="text-xs text-slate-600">Ref: {{ o.paymentReference || 'GCASH' }}</p>
                  <p class="text-[11px] text-blue-700 font-medium">⚡ Priority express pickup</p>
                } @else {
                  <p class="font-semibold text-slate-800">🏪 Pay at Counter</p>
                  <p class="text-xs text-slate-600">Pay when picking up order</p>
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
              <div class="mt-6 border-t border-slate-200 pt-4 space-y-1.5 text-sm">
                <div class="flex items-center justify-between text-slate-600">
                  <span>Food Subtotal</span>
                  <span>{{ o.subtotal | currency }}</span>
                </div>
                @if (o.discountAmount > 0) {
                  <div class="flex items-center justify-between text-emerald-700 font-medium">
                    <span>Promo Discount ({{ o.promoCode }})</span>
                    <span>-{{ o.discountAmount | currency }}</span>
                  </div>
                }
                @if (o.orderType === OrderType.Delivery) {
                  <div class="flex items-center justify-between text-slate-600">
                    <span>Delivery Fee</span>
                    <span [class.text-emerald-700]="o.deliveryFee === 0" [class.font-semibold]="o.deliveryFee === 0">
                      {{ o.deliveryFee > 0 ? (o.deliveryFee | currency) : 'FREE' }}
                    </span>
                  </div>
                }
                <div class="flex items-center justify-between font-bold text-base text-slate-900 pt-1 border-t border-slate-100">
                  <span>Total Amount</span>
                  <span>{{ o.totalAmount | currency }}</span>
                </div>
              </div>

              <!-- Customer Review Section -->
              @if (o.status === OrderStatus.Completed) {
                @if (o.review; as rev) {
                  <div class="mt-6 border-t border-slate-100 pt-5">
                    <div class="rounded-xl border border-amber-200 bg-amber-50/60 p-4">
                      <div class="flex items-center justify-between">
                        <div class="flex items-center gap-2">
                          <span class="text-amber-500 font-bold text-base tracking-wider">
                            @for (s of [1,2,3,4,5]; track s) {
                              <span [class.text-amber-400]="s <= rev.rating" [class.text-slate-300]="s > rev.rating">★</span>
                            }
                          </span>
                          <span class="text-xs font-bold text-slate-800">Your Review ({{ rev.rating }}/5)</span>
                        </div>
                        <span class="text-[11px] text-slate-400">{{ rev.createdAtUtc | date: 'mediumDate' }}</span>
                      </div>
                      @if (rev.comment) {
                        <p class="mt-2 text-xs text-slate-700 italic">"{{ rev.comment }}"</p>
                      }
                    </div>
                  </div>
                } @else if (auth.isAuthenticated()) {
                  <div class="mt-6 border-t border-slate-100 pt-5">
                    <div class="rounded-xl border border-slate-200 bg-slate-50/70 p-4 space-y-3">
                      <div>
                        <h3 class="text-xs font-bold uppercase tracking-wider text-slate-700">How was your order? Rate your experience</h3>
                        <p class="text-[11px] text-slate-500">Your rating helps this store improve and helps other customers!</p>
                      </div>

                      <div class="flex items-center gap-1.5">
                        @for (star of [1, 2, 3, 4, 5]; track star) {
                          <button
                            type="button"
                            (click)="selectedRating.set(star)"
                            (mouseenter)="hoverRating.set(star)"
                            (mouseleave)="hoverRating.set(0)"
                            class="text-2xl transition transform hover:scale-125 focus:outline-none"
                            [class.text-amber-400]="star <= (hoverRating() || selectedRating())"
                            [class.text-slate-300]="star > (hoverRating() || selectedRating())"
                          >
                            ★
                          </button>
                        }
                        <span class="ml-2 text-xs font-medium text-slate-600">
                          {{ ratingLabel(hoverRating() || selectedRating()) }}
                        </span>
                      </div>

                      @if (selectedRating() > 0) {
                        <div class="space-y-2 pt-1">
                          <textarea
                            rows="2"
                            [(ngModel)]="reviewComment"
                            placeholder="Add a comment about the food, taste, or packaging (optional)..."
                            class="w-full rounded-lg border border-slate-300 p-2.5 text-xs bg-white focus:border-slate-900 focus:outline-none"
                            maxlength="500"
                          ></textarea>
                          @if (reviewError()) {
                            <p class="text-[11px] text-red-600 font-medium">{{ reviewError() }}</p>
                          }
                          <div class="flex justify-end">
                            <button
                              type="button"
                              (click)="submitReview(o.publicId)"
                              [disabled]="submittingReview()"
                              class="rounded-lg bg-slate-900 px-4 py-1.5 text-xs font-semibold text-white hover:bg-slate-800 disabled:opacity-50"
                            >
                              {{ submittingReview() ? 'Submitting...' : 'Submit Review' }}
                            </button>
                          </div>
                        </div>
                      }
                    </div>
                  </div>
                }
              }
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
  readonly auth = inject(AuthService);

  readonly order = signal<Order | null>(null);
  readonly loading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);
  readonly storeSlug = signal<string | null>(null);

  readonly selectedRating = signal<number>(0);
  readonly hoverRating = signal<number>(0);
  readonly submittingReview = signal<boolean>(false);
  readonly reviewError = signal<string | null>(null);
  reviewComment = '';

  readonly OrderStatus = OrderStatus;
  readonly OrderType = OrderType;
  readonly CustomerType = CustomerType;
  readonly PaymentMethod = PaymentMethod;
  readonly PaymentStatus = PaymentStatus;

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
      case OrderStatus.OutForDelivery:
        return 'Out for Delivery 🛵';
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
      case OrderStatus.OutForDelivery:
        return 'bg-purple-100 text-purple-900 border border-purple-200';
      case OrderStatus.Completed:
        return 'bg-slate-200 text-slate-800';
      case OrderStatus.Cancelled:
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-slate-100 text-slate-700';
    }
  }

  /**
   * Evaluates order stepper progress chronologically:
   * Received -> Preparing -> Transit (Ready / OutForDelivery) -> Completed.
   */
  isStepDone(currentStatus: OrderStatus, step: 'received' | 'preparing' | 'transit' | 'completed'): boolean {
    if (currentStatus === OrderStatus.Cancelled) return false;
    switch (step) {
      case 'received':
        return true;
      case 'preparing':
        return currentStatus === OrderStatus.Confirmed || currentStatus === OrderStatus.Preparing || currentStatus === OrderStatus.Ready || currentStatus === OrderStatus.OutForDelivery || currentStatus === OrderStatus.Completed;
      case 'transit':
        return currentStatus === OrderStatus.Ready || currentStatus === OrderStatus.OutForDelivery || currentStatus === OrderStatus.Completed;
      case 'completed':
        return currentStatus === OrderStatus.Completed;
    }
  }

  stepCircleClass(currentStatus: OrderStatus, step: 'received' | 'preparing' | 'transit' | 'completed'): string {
    if (this.isStepDone(currentStatus, step)) {
      if (step === 'transit' && currentStatus === OrderStatus.OutForDelivery) {
        return 'bg-purple-600 text-white animate-pulse';
      }
      if (step === 'completed' && currentStatus === OrderStatus.Completed) {
        return 'bg-emerald-600 text-white';
      }
      return 'bg-slate-900 text-white';
    }
    return 'bg-slate-200 text-slate-400';
  }

  stepTextClass(currentStatus: OrderStatus, step: 'received' | 'preparing' | 'transit' | 'completed'): string {
    return this.isStepDone(currentStatus, step) ? 'text-slate-900 font-semibold' : 'text-slate-400';
  }

  ratingLabel(stars: number): string {
    switch (stars) {
      case 5: return '⭐⭐⭐⭐⭐ Excellent!';
      case 4: return '⭐⭐⭐⭐ Very Good';
      case 3: return '⭐⭐⭐ Average';
      case 2: return '⭐⭐ Below Average';
      case 1: return '⭐ Poor';
      default: return 'Select your rating';
    }
  }

  submitReview(orderPublicId: string): void {
    const rating = this.selectedRating();
    if (rating < 1 || rating > 5) {
      this.reviewError.set('Please select a star rating from 1 to 5.');
      return;
    }

    this.reviewError.set(null);
    this.submittingReview.set(true);

    this.api.createOrderReview(orderPublicId, {
      rating,
      comment: this.reviewComment.trim() || null
    }).subscribe({
      next: (review) => {
        this.submittingReview.set(false);
        this.reviewComment = '';
        this.order.update((cur) => cur ? { ...cur, review } : cur);
      },
      error: (err) => {
        this.submittingReview.set(false);
        this.reviewError.set(err?.error?.error ?? 'Could not submit review.');
      }
    });
  }
}
