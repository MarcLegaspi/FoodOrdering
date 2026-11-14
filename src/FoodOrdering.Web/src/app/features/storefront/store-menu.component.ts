import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { CartSelectedOption, CartService } from '../../core/cart/cart.service';
import { Category, MenuItem, OrderReview, OrderType, PaymentMethod, PromoDiscountType, Store, ValidatePromoResponse } from '../../core/models/api.models';
import { FoodOrderingApiService } from '../../core/services/food-ordering-api.service';
import { TenantService } from '../../core/tenant/tenant.service';
import { resolveImageUrl } from '../../core/utils/media-url';

@Component({
  selector: 'app-store-menu',
  imports: [CurrencyPipe, DatePipe, FormsModule, RouterLink],
  template: `
    @if (loading()) {
      <p class="text-sm text-slate-500">Loading store menu...</p>
    } @else if (error()) {
      <div class="rounded-lg bg-red-50 p-4 text-sm text-red-700">{{ error() }}</div>
    } @else if (store(); as activeStore) {
      <div class="grid gap-8 lg:grid-cols-3">
        <!-- Menu Section -->
        <div class="space-y-6 lg:col-span-2">
          <div class="rounded-xl border border-slate-200 bg-white p-6">
            <div class="flex flex-wrap items-center justify-between gap-2">
              <h1 class="text-2xl font-bold text-slate-900">{{ activeStore.name }}</h1>
              <span
                class="rounded-full px-2.5 py-0.5 text-xs font-medium"
                [class]="activeStore.isAcceptingOrders ? 'bg-emerald-50 text-emerald-700' : 'bg-red-50 text-red-700'"
              >
                {{ activeStore.isAcceptingOrders ? 'Accepting Orders' : 'Closed' }}
              </span>
            </div>
            <p class="mt-1 text-sm text-slate-500">{{ activeStore.email }} · {{ activeStore.phone }}</p>
            @if (activeStore.address || activeStore.openingHours) {
              <p class="mt-1 text-xs text-slate-500">
                {{ activeStore.address }}
                @if (activeStore.address && activeStore.openingHours) {
                  <span> · </span>
                }
                {{ activeStore.openingHours }}
              </p>
            }

            <div class="mt-3 flex flex-wrap items-center gap-2 text-xs">
              @if (activeStore.estimatedDeliveryTime) {
                <span class="inline-flex items-center gap-1 rounded-md bg-slate-100 px-2.5 py-1 font-medium text-slate-700">
                  ⏱ Est. Delivery: {{ activeStore.estimatedDeliveryTime }}
                </span>
              }
              <span class="inline-flex items-center gap-1 rounded-md bg-slate-100 px-2.5 py-1 font-medium text-slate-700">
                🚗 Delivery Fee: {{ activeStore.deliveryFee > 0 ? (activeStore.deliveryFee | currency) : 'Free' }}
              </span>
              @if (activeStore.minimumDeliveryOrder > 0) {
                <span class="inline-flex items-center gap-1 rounded-md bg-slate-100 px-2.5 py-1 font-medium text-slate-700">
                  Min. Delivery: {{ activeStore.minimumDeliveryOrder | currency }}
                </span>
              }
              @if (activeStore.freeDeliveryThreshold) {
                <span class="inline-flex items-center gap-1 rounded-md bg-emerald-50 px-2.5 py-1 font-semibold text-emerald-700 border border-emerald-200">
                  Free Delivery Over {{ activeStore.freeDeliveryThreshold | currency }}
                </span>
              }
              @if (activeStore.averageRating) {
                <button
                  type="button"
                  (click)="openReviewsModal()"
                  class="inline-flex items-center gap-1 rounded-md bg-amber-50 px-2.5 py-1 font-bold text-amber-800 border border-amber-200 hover:bg-amber-100 transition"
                >
                  ★ {{ activeStore.averageRating.toFixed(1) }}
                  <span class="text-xs font-normal text-amber-700 underline">({{ activeStore.reviewCount }} reviews)</span>
                </button>
              }
            </div>

            @if (!activeStore.isAcceptingOrders) {
              <p class="mt-3 rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-800">
                This store is currently closed and not accepting new orders.
              </p>
            }
            @if (!auth.isAuthenticated() && recentGuestOrderId(); as recentId) {
              <div class="mt-3 flex items-center justify-between rounded-lg bg-indigo-50 px-3 py-2 text-xs text-indigo-900 border border-indigo-100">
                <span>Have an active order with this store?</span>
                <a [routerLink]="['/', activeStore.slug, 'orders', recentId]" class="font-bold underline hover:text-indigo-700">
                  Track Recent Order →
                </a>
              </div>
            }
          </div>

          @if (menuItems().length > 0) {
            <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div class="flex flex-wrap gap-2">
                <button
                  type="button"
                  (click)="selectedCategoryId.set(null)"
                  class="rounded-full px-3 py-1.5 text-xs font-medium transition"
                  [class]="selectedCategoryId() === null ? 'bg-slate-900 text-white' : 'border border-slate-300 bg-white text-slate-700 hover:bg-slate-50'"
                >
                  All
                </button>
                @for (cat of categories(); track cat.publicId) {
                  <button
                    type="button"
                    (click)="selectedCategoryId.set(cat.publicId)"
                    class="rounded-full px-3 py-1.5 text-xs font-medium transition"
                    [class]="selectedCategoryId() === cat.publicId ? 'bg-slate-900 text-white' : 'border border-slate-300 bg-white text-slate-700 hover:bg-slate-50'"
                  >
                    {{ cat.name }}
                  </button>
                }
              </div>

              <input
                type="search"
                name="searchQuery"
                [ngModel]="searchQuery()"
                (ngModelChange)="searchQuery.set($event)"
                placeholder="Search menu..."
                class="w-full rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-sm sm:w-56"
              />
            </div>
          }

          @if (groupedMenu().length === 0) {
            <div class="rounded-xl border border-dashed border-slate-300 bg-white p-8 text-center text-sm text-slate-500">
              {{ menuItems().length > 0 ? 'No menu items match your filter.' : 'No menu items available yet.' }}
            </div>
          }

          @for (group of groupedMenu(); track group.categoryName) {
            <section class="space-y-3">
              <h2 class="text-lg font-semibold text-slate-900">{{ group.categoryName }}</h2>

              <div class="grid gap-3 sm:grid-cols-2">
                @for (item of group.items; track item.publicId) {
                  <div class="flex flex-col justify-between overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
                    @if (imageUrl(item.imageUrl); as src) {
                      <img
                        [src]="src"
                        [alt]="item.name"
                        class="h-44 w-full object-cover"
                      />
                    } @else {
                      <div class="flex h-44 w-full items-center justify-center bg-slate-100 text-xs font-medium text-slate-400">
                        No image
                      </div>
                    }

                    <div class="flex flex-1 flex-col justify-between p-4">
                      <div>
                        <div class="flex items-start justify-between gap-2">
                          <h3 class="font-semibold text-slate-900">{{ item.name }}</h3>
                          <span class="font-semibold text-slate-900">{{ item.price | currency }}</span>
                        </div>
                        @if (item.description) {
                          <p class="mt-1 text-sm text-slate-500">{{ item.description }}</p>
                        }
                      </div>

                      <button
                        type="button"
                        [disabled]="!activeStore.isAcceptingOrders || !item.isAvailable"
                        (click)="onAddItemClicked(item)"
                        class="mt-4 w-full rounded-lg border border-slate-300 py-1.5 text-sm font-medium text-slate-800 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        {{ !activeStore.isAcceptingOrders ? 'Store Closed' : (!item.isAvailable ? 'Sold Out' : (item.optionGroups.length > 0 ? 'Customize & Add' : 'Add to cart')) }}
                      </button>
                    </div>
                  </div>
                }
              </div>
            </section>
          }
        </div>

        <!-- Cart Sidebar -->
        <aside class="h-fit rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
          <h2 class="text-lg font-bold text-slate-900">Your Cart ({{ cart.itemCount() }})</h2>

          @if (orderError()) {
            <div class="mt-3 rounded-lg bg-red-50 p-3 text-sm text-red-700">{{ orderError() }}</div>
          }

          @if (cart.items().length === 0) {
            <p class="mt-4 text-sm text-slate-500">Your cart is empty. Add items from the menu.</p>
          } @else {
            <div class="mt-4 divide-y divide-slate-100">
              @for (item of cart.items(); track item.cartItemKey) {
                <div class="flex items-start justify-between gap-2 py-3 text-sm">
                  <div>
                    <p class="font-medium text-slate-900">{{ item.name }}</p>
                    @if (item.selectedOptions.length > 0) {
                      <p class="text-xs text-slate-500">
                        @for (opt of item.selectedOptions; track opt.publicId; let last = $last) {
                          {{ opt.name }}@if (opt.priceDelta > 0) { (+{{ opt.priceDelta | currency }}) }@if (!last) {, }
                        }
                      </p>
                    }
                    <p class="text-xs text-slate-400">{{ item.unitPrice | currency }} each</p>
                  </div>

                  <div class="flex items-center gap-2">
                    <button
                      type="button"
                      (click)="cart.updateQuantity(item.cartItemKey, item.quantity - 1)"
                      class="h-7 w-7 rounded border border-slate-300 text-slate-700 hover:bg-slate-50"
                    >
                      -
                    </button>
                    <span class="w-5 text-center font-medium">{{ item.quantity }}</span>
                    <button
                      type="button"
                      (click)="cart.updateQuantity(item.cartItemKey, item.quantity + 1)"
                      class="h-7 w-7 rounded border border-slate-300 text-slate-700 hover:bg-slate-50"
                    >
                      +
                    </button>
                  </div>
                </div>
              }
            </div>

            <div class="mt-4 space-y-3 border-t border-slate-200 pt-4">
              <div class="grid grid-cols-2 gap-2 rounded-lg bg-slate-100 p-1 text-xs font-semibold">
                <button
                  type="button"
                  (click)="setOrderType(OrderType.Pickup)"
                  [class]="orderType() === OrderType.Pickup ? 'rounded-md bg-white py-1.5 text-slate-900 shadow-xs' : 'py-1.5 text-slate-500'"
                >
                  Pickup
                </button>
                <button
                  type="button"
                  (click)="setOrderType(OrderType.Delivery)"
                  [class]="orderType() === OrderType.Delivery ? 'rounded-md bg-white py-1.5 text-slate-900 shadow-xs' : 'py-1.5 text-slate-500'"
                >
                  Delivery
                </button>
              </div>

              <input
                type="text"
                [(ngModel)]="customerName"
                placeholder="Your full name"
                class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-slate-900 focus:outline-none"
              />

              <input
                type="tel"
                [(ngModel)]="customerPhone"
                placeholder="Contact phone number"
                class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-slate-900 focus:outline-none"
              />

              @if (!auth.isAuthenticated()) {
                <input
                  type="email"
                  [(ngModel)]="customerEmail"
                  placeholder="Email (optional, for receipt)"
                  class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-slate-900 focus:outline-none"
                />
              }

              @if (orderType() === OrderType.Delivery) {
                <textarea
                  rows="2"
                  [(ngModel)]="deliveryAddress"
                  placeholder="Delivery street address"
                  class="w-full rounded-lg border border-slate-300 p-2 text-sm focus:border-slate-900 focus:outline-none"
                ></textarea>
                <input
                  type="text"
                  [(ngModel)]="deliveryLandmark"
                  placeholder="Unit, Landmark, Gate code (optional)"
                  class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-slate-900 focus:outline-none"
                />

                @if (freeDeliveryProgress(); as fdp) {
                  <div class="rounded-lg bg-emerald-50 p-2.5 text-xs border border-emerald-200">
                    @if (fdp.qualified) {
                      <div class="font-bold text-emerald-800">🎉 You unlocked FREE Delivery!</div>
                    } @else {
                      <div class="flex items-center justify-between text-emerald-900">
                        <span>Add <strong>{{ fdp.remaining | currency }}</strong> more for FREE delivery</span>
                        <span class="font-bold">{{ fdp.percent }}%</span>
                      </div>
                      <div class="mt-1.5 h-1.5 w-full overflow-hidden rounded-full bg-emerald-200">
                        <div class="h-full bg-emerald-600 transition-all duration-300" [style.width.%]="fdp.percent"></div>
                      </div>
                    }
                  </div>
                }

                @if (isBelowDeliveryMinimum()) {
                  <div class="rounded-lg bg-amber-50 p-2.5 text-xs text-amber-900 border border-amber-200">
                    ⚠️ Minimum order for delivery is <strong>{{ activeStore.minimumDeliveryOrder | currency }}</strong>.
                    Add <strong>{{ deliveryMinimumRemaining() | currency }}</strong> more to place order.
                  </div>
                }
              }

              <textarea
                rows="2"
                [(ngModel)]="notes"
                placeholder="Order notes (optional)"
                class="w-full rounded-lg border border-slate-300 p-2 text-sm focus:border-slate-900 focus:outline-none"
              ></textarea>

              <!-- Payment Option Section -->
              <div class="space-y-2 rounded-lg border border-slate-200 bg-slate-50/70 p-3 text-xs">
                <label class="block font-semibold text-slate-700">Payment Option</label>

                @if (orderType() === OrderType.Delivery) {
                  <div class="grid grid-cols-3 gap-1.5">
                    <button
                      type="button"
                      (click)="selectedPaymentMethod.set(PaymentMethod.PayOnDelivery)"
                      class="flex flex-col items-start rounded-lg border p-2 text-left transition"
                      [class]="selectedPaymentMethod() === PaymentMethod.PayOnDelivery
                        ? 'border-slate-900 bg-slate-900 text-white'
                        : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50'"
                    >
                      <span class="text-base">🛵</span>
                      <span class="font-bold text-[11px] mt-0.5 leading-tight">Pay on Delivery</span>
                      <span class="text-[9px] mt-0.5 leading-tight" [class]="selectedPaymentMethod() === PaymentMethod.PayOnDelivery ? 'text-slate-300' : 'text-slate-500'">Cash / Rider QR</span>
                    </button>

                    <button
                      type="button"
                      (click)="selectedPaymentMethod.set(PaymentMethod.GCashManual)"
                      class="flex flex-col items-start rounded-lg border p-2 text-left transition"
                      [class]="selectedPaymentMethod() === PaymentMethod.GCashManual
                        ? 'border-emerald-700 bg-emerald-700 text-white shadow-xs'
                        : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50'"
                    >
                      <span class="text-base">💸</span>
                      <span class="font-bold text-[11px] mt-0.5 leading-tight">GCash Transfer</span>
                      <span class="text-[9px] mt-0.5 leading-tight" [class]="selectedPaymentMethod() === PaymentMethod.GCashManual ? 'text-emerald-200' : 'text-emerald-700 font-semibold'">0% Fee · Last 4</span>
                    </button>

                    <button
                      type="button"
                      (click)="selectedPaymentMethod.set(PaymentMethod.GCashOnline)"
                      class="flex flex-col items-start rounded-lg border p-2 text-left transition"
                      [class]="selectedPaymentMethod() === PaymentMethod.GCashOnline
                        ? 'border-blue-600 bg-blue-50/80 text-blue-900 ring-1 ring-blue-600'
                        : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50'"
                    >
                      <span class="text-base">📱</span>
                      <span class="font-bold text-[11px] mt-0.5 leading-tight">GCash Online</span>
                      <span class="text-[9px] text-blue-700 mt-0.5 leading-tight">Auto Gateway</span>
                    </button>
                  </div>

                  @if (selectedPaymentMethod() === PaymentMethod.PayOnDelivery) {
                    <div class="pt-2 border-t border-slate-200">
                      <label class="block text-[11px] text-slate-500 mb-1">Paying with cash? (Optional)</label>
                      <input
                        type="number"
                        [(ngModel)]="cashChangeFor"
                        placeholder="Need change for e.g. 500, 1000"
                        class="w-full rounded border border-slate-300 bg-white px-2.5 py-1.5 text-xs text-slate-800 placeholder-slate-400 focus:border-slate-900 focus:outline-none"
                      />
                      @if (cashChangeFor && cashChangeFor > finalOrderTotal()) {
                        <p class="mt-1 text-[11px] text-emerald-700 font-medium">
                          Rider will bring {{ (cashChangeFor - finalOrderTotal()) | currency }} change.
                        </p>
                      }
                    </div>
                  } @else if (selectedPaymentMethod() === PaymentMethod.GCashManual) {
                    <div class="rounded-lg border border-emerald-200 bg-emerald-50/70 p-3 text-xs space-y-2 mt-2">
                      <div class="flex items-center justify-between">
                        <span class="font-bold text-emerald-900 flex items-center gap-1">
                          <span>💸</span> Direct GCash Transfer
                        </span>
                        <span class="rounded bg-emerald-100 px-1.5 py-0.5 text-[10px] font-bold text-emerald-800">0% Fee</span>
                      </div>
                      <div class="rounded-md bg-white p-2.5 border border-emerald-100 space-y-1">
                        <div class="flex items-center justify-between text-[11px]">
                          <span class="text-slate-500">Send to GCash:</span>
                          <span class="font-bold font-mono text-slate-900 select-all">{{ activeStore.phone || 'Store GCash' }}</span>
                        </div>
                        <div class="flex items-center justify-between text-[11px]">
                          <span class="text-slate-500">Account Name:</span>
                          <span class="font-semibold text-slate-800">{{ activeStore.name }}</span>
                        </div>
                        <div class="flex items-center justify-between text-[11px] border-t border-slate-100 pt-1">
                          <span class="text-slate-500">Amount:</span>
                          <span class="font-bold text-emerald-700">{{ finalOrderTotal() | currency }}</span>
                        </div>
                      </div>
                      <div class="space-y-1 pt-1">
                        <label class="block text-[11px] font-semibold text-slate-800">
                          Last 4 digits of GCash Reference No. <span class="text-red-500">*</span>
                        </label>
                        <div class="flex items-center gap-2">
                          <span class="text-slate-400 font-mono text-xs">Ref: •••••••••</span>
                          <input
                            type="text"
                            [(ngModel)]="paymentRefLast4"
                            maxlength="4"
                            inputmode="numeric"
                            placeholder="e.g. 5821"
                            class="w-24 rounded border border-slate-300 bg-white px-2 py-1.5 text-center font-mono text-sm font-bold tracking-widest text-slate-900 placeholder-slate-400 focus:border-emerald-600 focus:outline-none"
                          />
                        </div>
                        <p class="text-[10px] text-slate-500">
                          From your GCash SMS or payment receipt (e.g. Ref No. 102938472<strong>5821</strong>).
                        </p>
                      </div>
                    </div>
                  } @else {
                    <div class="rounded-md bg-blue-50 p-2 text-[11px] text-blue-800 border border-blue-200">
                      ⚡ <strong>Pre-paid Delivery:</strong> Pre-paid via GCash gateway. No payment needed at doorstep—just give your 4-digit PIN to the rider!
                    </div>
                  }
                } @else {
                  <div class="grid grid-cols-3 gap-1.5">
                    <button
                      type="button"
                      (click)="selectedPaymentMethod.set(PaymentMethod.GCashManual)"
                      class="flex flex-col items-start rounded-lg border p-2 text-left transition"
                      [class]="selectedPaymentMethod() === PaymentMethod.GCashManual
                        ? 'border-emerald-700 bg-emerald-700 text-white shadow-xs'
                        : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50'"
                    >
                      <span class="text-base">💸</span>
                      <span class="font-bold text-[11px] mt-0.5 leading-tight">GCash Transfer</span>
                      <span class="text-[9px] mt-0.5 leading-tight" [class]="selectedPaymentMethod() === PaymentMethod.GCashManual ? 'text-emerald-200' : 'text-emerald-700 font-semibold'">0% Fee · Last 4</span>
                    </button>

                    <button
                      type="button"
                      (click)="selectedPaymentMethod.set(PaymentMethod.GCashOnline)"
                      class="flex flex-col items-start rounded-lg border p-2 text-left transition"
                      [class]="selectedPaymentMethod() === PaymentMethod.GCashOnline
                        ? 'border-blue-600 bg-blue-50/80 text-blue-900 ring-1 ring-blue-600'
                        : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50'"
                    >
                      <span class="text-base">📱</span>
                      <span class="font-bold text-[11px] mt-0.5 leading-tight">GCash Online</span>
                      <span class="text-[9px] text-blue-700 mt-0.5 leading-tight">Auto Gateway</span>
                    </button>

                    <button
                      type="button"
                      (click)="selectedPaymentMethod.set(PaymentMethod.PayAtCounter)"
                      class="flex flex-col items-start rounded-lg border p-2 text-left transition"
                      [class]="selectedPaymentMethod() === PaymentMethod.PayAtCounter
                        ? 'border-slate-900 bg-slate-900 text-white'
                        : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50'"
                    >
                      <span class="text-base">🏪</span>
                      <span class="font-bold text-[11px] mt-0.5 leading-tight">Pay at Counter</span>
                      <span class="text-[9px] text-slate-400 mt-0.5 leading-tight">Cash at store</span>
                    </button>
                  </div>

                  @if (selectedPaymentMethod() === PaymentMethod.GCashManual) {
                    <div class="rounded-lg border border-emerald-200 bg-emerald-50/70 p-3 text-xs space-y-2 mt-2">
                      <div class="flex items-center justify-between">
                        <span class="font-bold text-emerald-900 flex items-center gap-1">
                          <span>💸</span> Direct GCash Transfer
                        </span>
                        <span class="rounded bg-emerald-100 px-1.5 py-0.5 text-[10px] font-bold text-emerald-800">0% Fee</span>
                      </div>
                      <div class="rounded-md bg-white p-2.5 border border-emerald-100 space-y-1">
                        <div class="flex items-center justify-between text-[11px]">
                          <span class="text-slate-500">Send to GCash:</span>
                          <span class="font-bold font-mono text-slate-900 select-all">{{ activeStore.phone || 'Store GCash' }}</span>
                        </div>
                        <div class="flex items-center justify-between text-[11px]">
                          <span class="text-slate-500">Account Name:</span>
                          <span class="font-semibold text-slate-800">{{ activeStore.name }}</span>
                        </div>
                        <div class="flex items-center justify-between text-[11px] border-t border-slate-100 pt-1">
                          <span class="text-slate-500">Amount:</span>
                          <span class="font-bold text-emerald-700">{{ finalOrderTotal() | currency }}</span>
                        </div>
                      </div>
                      <div class="space-y-1 pt-1">
                        <label class="block text-[11px] font-semibold text-slate-800">
                          Last 4 digits of GCash Reference No. <span class="text-red-500">*</span>
                        </label>
                        <div class="flex items-center gap-2">
                          <span class="text-slate-400 font-mono text-xs">Ref: •••••••••</span>
                          <input
                            type="text"
                            [(ngModel)]="paymentRefLast4"
                            maxlength="4"
                            inputmode="numeric"
                            placeholder="e.g. 5821"
                            class="w-24 rounded border border-slate-300 bg-white px-2 py-1.5 text-center font-mono text-sm font-bold tracking-widest text-slate-900 placeholder-slate-400 focus:border-emerald-600 focus:outline-none"
                          />
                        </div>
                        <p class="text-[10px] text-slate-500">
                          From your GCash SMS or payment receipt (e.g. Ref No. 102938472<strong>5821</strong>).
                        </p>
                      </div>
                    </div>
                  } @else if (selectedPaymentMethod() === PaymentMethod.GCashOnline) {
                    <div class="rounded-md bg-blue-50 p-2 text-[11px] text-blue-800 border border-blue-200 mt-2">
                      ⚡ <strong>Express Pickup:</strong> Pre-paid via GCash gateway. Just show your order number at the counter!
                    </div>
                  }
                }
              </div>

              <!-- Promo Code Section -->
              <div class="border-t border-slate-200 pt-3">
                @if (appliedPromo(); as promo) {
                  <div class="flex items-center justify-between rounded-lg bg-emerald-50 px-3 py-2 text-xs border border-emerald-200">
                    <div class="space-y-0.5">
                      <div class="font-bold text-emerald-800 flex items-center gap-1.5">
                        <span>🏷 {{ promo.code }}</span>
                        <span class="rounded bg-emerald-200/60 px-1.5 py-0.5 text-[10px] text-emerald-900">Applied</span>
                      </div>
                      <div class="text-emerald-700">
                        -{{ effectiveDiscount() | currency }} off food subtotal
                      </div>
                    </div>
                    <button
                      type="button"
                      (click)="removePromo()"
                      class="text-xs font-semibold text-emerald-800 hover:text-red-600 underline"
                    >
                      Remove
                    </button>
                  </div>
                } @else if (auth.isAuthenticated()) {
                  <div class="space-y-1.5">
                    <div class="flex gap-2">
                      <input
                        type="text"
                        [(ngModel)]="promoInput"
                        placeholder="Promo code"
                        class="w-full uppercase rounded-lg border border-slate-300 px-3 py-1.5 text-xs font-mono tracking-wider focus:border-slate-900 focus:outline-none"
                        (keydown.enter)="applyPromo()"
                      />
                      <button
                        type="button"
                        (click)="applyPromo()"
                        [disabled]="validatingPromo() || !promoInput.trim()"
                        class="shrink-0 rounded-lg bg-slate-900 px-3 py-1.5 text-xs font-semibold text-white hover:bg-slate-800 disabled:opacity-50"
                      >
                        {{ validatingPromo() ? 'Checking...' : 'Apply' }}
                      </button>
                    </div>
                    @if (promoError()) {
                      <p class="text-[11px] text-red-600">{{ promoError() }}</p>
                    }
                  </div>
                } @else {
                  <div class="rounded-lg bg-slate-50 p-2.5 text-xs text-slate-600 border border-slate-200 flex items-center justify-between">
                    <span>Have a promo code?</span>
                    <a
                      routerLink="/login"
                      [queryParams]="{ returnUrl: '/' + activeStore.slug }"
                      class="font-semibold text-slate-900 underline hover:text-slate-700"
                    >
                      Sign in to apply
                    </a>
                  </div>
                }
              </div>

              <div class="space-y-1 border-t border-slate-200 pt-3 text-sm">
                <div class="flex items-center justify-between text-slate-600">
                  <span>Food Subtotal</span>
                  <span>{{ cart.totalAmount() | currency }}</span>
                </div>
                @if (effectiveDiscount() > 0) {
                  <div class="flex items-center justify-between text-emerald-700 font-medium">
                    <span>Promo Discount ({{ appliedPromo()?.code }})</span>
                    <span>-{{ effectiveDiscount() | currency }}</span>
                  </div>
                }
                @if (orderType() === OrderType.Delivery) {
                  <div class="flex items-center justify-between text-slate-600">
                    <span>Delivery Fee</span>
                    <span [class.text-emerald-700]="currentDeliveryFee() === 0" [class.font-semibold]="currentDeliveryFee() === 0">
                      {{ currentDeliveryFee() === 0 ? 'FREE' : (currentDeliveryFee() | currency) }}
                    </span>
                  </div>
                }
                <div class="flex items-center justify-between font-bold text-base text-slate-900 pt-1">
                  <span>Total</span>
                  <span>{{ finalOrderTotal() | currency }}</span>
                </div>
              </div>

              <button
                type="button"
                [disabled]="submitting() || !activeStore.isAcceptingOrders || isBelowDeliveryMinimum()"
                (click)="checkout()"
                class="w-full rounded-lg bg-slate-900 py-2.5 text-sm font-semibold text-white hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-50"
              >
                @if (!activeStore.isAcceptingOrders) {
                  Store Closed
                } @else if (isBelowDeliveryMinimum()) {
                  Below Delivery Minimum
                } @else if (submitting()) {
                  Placing order...
                } @else if (auth.isAuthenticated()) {
                  Place Order
                } @else {
                  Place Order as Guest
                }
              </button>

              @if (!auth.isAuthenticated()) {
                <div class="text-center text-xs text-slate-500">
                  Already have an account?
                  <a
                    routerLink="/login"
                    [queryParams]="{ returnUrl: '/' + activeStore.slug }"
                    class="font-semibold text-slate-900 underline hover:text-slate-700 ml-1"
                  >
                    Sign in
                  </a>
                </div>
              }
            </div>
          }
        </aside>
      </div>

      @if (customizingItem(); as customItem) {
        <div class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
          <div class="max-h-[90vh] w-full max-w-md overflow-y-auto rounded-xl bg-white p-6 shadow-xl">
            <div class="flex items-start justify-between gap-3">
              <div>
                <h2 class="text-lg font-bold text-slate-900">{{ customItem.name }}</h2>
                <p class="text-sm text-slate-500">Base price: {{ customItem.price | currency }}</p>
              </div>
              <button
                type="button"
                (click)="closeCustomize()"
                class="rounded-md px-2 py-1 text-sm text-slate-500 hover:bg-slate-100"
              >
                ✕
              </button>
            </div>

            <div class="mt-4 space-y-4">
              @for (group of customItem.optionGroups; track group.publicId) {
                <div class="space-y-2 border-t border-slate-100 pt-3">
                  <div class="flex items-center justify-between">
                    <span class="text-sm font-semibold text-slate-900">{{ group.name }}</span>
                    <span class="text-xs font-medium text-slate-500">
                      {{ group.isRequired ? 'Required' : 'Optional' }} · {{ group.allowMultiple ? 'Choose any' : 'Choose 1' }}
                    </span>
                  </div>

                  <div class="space-y-1.5">
                    @for (opt of group.options; track opt.publicId) {
                      <label class="flex cursor-pointer items-center justify-between rounded-lg border border-slate-200 px-3 py-2 text-sm hover:bg-slate-50">
                        <span class="flex items-center gap-2">
                          <input
                            [type]="group.allowMultiple ? 'checkbox' : 'radio'"
                            [name]="'group-' + group.publicId"
                            [checked]="isOptionSelected(group.publicId, opt.publicId)"
                            (change)="onOptionToggle(group.publicId, opt.publicId, group.allowMultiple)"
                          />
                          <span class="text-slate-800">{{ opt.name }}</span>
                        </span>
                        @if (opt.priceDelta > 0) {
                          <span class="text-xs font-medium text-slate-600">+{{ opt.priceDelta | currency }}</span>
                        }
                      </label>
                    }
                  </div>
                </div>
              }
            </div>

            <div class="mt-6 flex items-center justify-between gap-3 border-t border-slate-200 pt-4">
              <span class="text-base font-bold text-slate-900">
                Total: {{ customizingUnitPrice() | currency }}
              </span>
              <button
                type="button"
                [disabled]="!canConfirmCustomization()"
                (click)="confirmCustomization()"
                class="rounded-lg bg-slate-900 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-50"
              >
                Add to Cart
              </button>
            </div>
          </div>
        </div>
      }

      @if (showingReviewsModal()) {
        <div class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
          <div class="max-h-[85vh] w-full max-w-lg overflow-y-auto rounded-xl bg-white p-6 shadow-xl space-y-4">
            <div class="flex items-center justify-between border-b border-slate-100 pb-3">
              <div>
                <h2 class="text-lg font-bold text-slate-900">{{ activeStore.name }} Reviews</h2>
                @if (activeStore.averageRating) {
                  <p class="text-xs text-amber-700 font-semibold flex items-center gap-1 mt-0.5">
                    ★ {{ activeStore.averageRating.toFixed(1) }} out of 5 stars ({{ activeStore.reviewCount }} customer reviews)
                  </p>
                }
              </div>
              <button
                type="button"
                (click)="closeReviewsModal()"
                class="rounded-md p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-600"
              >
                ✕
              </button>
            </div>

            @if (loadingReviews()) {
              <div class="py-8 text-center text-xs text-slate-400">Loading customer reviews...</div>
            } @else if (storeReviews().length === 0) {
              <div class="py-8 text-center text-xs text-slate-400">No reviews yet for this store.</div>
            } @else {
              <div class="divide-y divide-slate-100 space-y-3">
                @for (rev of storeReviews(); track rev.publicId) {
                  <div class="pt-3 space-y-1">
                    <div class="flex items-center justify-between text-xs">
                      <span class="font-bold text-slate-900">{{ rev.customerName }}</span>
                      <span class="text-[11px] text-slate-400">{{ rev.createdAtUtc | date: 'mediumDate' }}</span>
                    </div>
                    <div class="text-amber-400 text-sm">
                      @for (s of [1,2,3,4,5]; track s) {
                        <span [class.text-amber-400]="s <= rev.rating" [class.text-slate-200]="s > rev.rating">★</span>
                      }
                    </div>
                    @if (rev.comment) {
                      <p class="text-xs text-slate-600 italic">"{{ rev.comment }}"</p>
                    }
                  </div>
                }
              </div>
            }
          </div>
        </div>
      }
    }
  `
})
export class StoreMenuComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(FoodOrderingApiService);
  private readonly tenant = inject(TenantService);
  readonly auth = inject(AuthService);
  readonly cart = inject(CartService);

  readonly store = signal<Store | null>(null);
  readonly categories = signal<Category[]>([]);
  readonly menuItems = signal<MenuItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly orderError = signal<string | null>(null);
  readonly submitting = signal(false);
  readonly selectedCategoryId = signal<string | null>(null);
  readonly searchQuery = signal('');
  readonly OrderType = OrderType;
  readonly PaymentMethod = PaymentMethod;
  readonly imageUrl = resolveImageUrl;
  readonly orderType = signal<OrderType>(OrderType.Pickup);
  readonly selectedPaymentMethod = signal<PaymentMethod>(PaymentMethod.PayAtCounter);
  cashChangeFor: number | null = null;
  readonly customizingItem = signal<MenuItem | null>(null);
  readonly selectedOptionsByGroup = signal<Record<string, string[]>>({});
  readonly recentGuestOrderId = signal<string | null>(null);
  readonly appliedPromo = signal<ValidatePromoResponse | null>(null);
  readonly validatingPromo = signal(false);
  readonly promoError = signal<string | null>(null);
  readonly showingReviewsModal = signal(false);
  readonly storeReviews = signal<OrderReview[]>([]);
  readonly loadingReviews = signal(false);
  promoInput = '';
  customerName = '';
  customerPhone = '';
  customerEmail = '';
  deliveryAddress = '';
  deliveryLandmark = '';
  notes = '';
  paymentRefLast4 = '';

  readonly currentDeliveryFee = computed(() => {
    const st = this.store();
    if (!st || this.orderType() !== OrderType.Delivery) {
      return 0;
    }
    const subtotal = this.cart.totalAmount();
    if (st.freeDeliveryThreshold && subtotal >= st.freeDeliveryThreshold) {
      return 0;
    }
    return st.deliveryFee ?? 0;
  });

  readonly effectiveDiscount = computed(() => {
    const promo = this.appliedPromo();
    if (!promo) return 0;
    const subtotal = this.cart.totalAmount();
    if (subtotal <= 0) return 0;
    if (promo.discountType === PromoDiscountType.Percentage) {
      return Math.min(subtotal, Math.round(subtotal * (promo.discountValue / 100) * 100) / 100);
    }
    return Math.min(subtotal, promo.discountValue);
  });

  readonly finalOrderTotal = computed(() => {
    const subtotal = this.cart.totalAmount();
    const discount = this.effectiveDiscount();
    const foodTotal = Math.max(0, subtotal - discount);
    return foodTotal + this.currentDeliveryFee();
  });

  readonly isBelowDeliveryMinimum = computed(() => {
    const st = this.store();
    if (!st || this.orderType() !== OrderType.Delivery) {
      return false;
    }
    return (st.minimumDeliveryOrder ?? 0) > 0 && this.cart.totalAmount() < st.minimumDeliveryOrder;
  });

  readonly deliveryMinimumRemaining = computed(() => {
    const st = this.store();
    if (!st || !this.isBelowDeliveryMinimum()) {
      return 0;
    }
    return Math.max(0, st.minimumDeliveryOrder - this.cart.totalAmount());
  });

  readonly freeDeliveryProgress = computed(() => {
    const st = this.store();
    if (!st || this.orderType() !== OrderType.Delivery || !st.freeDeliveryThreshold) {
      return null;
    }
    const subtotal = this.cart.totalAmount();
    const threshold = st.freeDeliveryThreshold;
    const remaining = Math.max(0, threshold - subtotal);
    const percent = Math.min(100, Math.round((subtotal / threshold) * 100));
    return { threshold, remaining, percent, qualified: subtotal >= threshold };
  });

  readonly customizingUnitPrice = computed(() => {
    const item = this.customizingItem();
    if (!item) {
      return 0;
    }

    const extra = this.resolveSelectedCartOptions(item).reduce((sum, o) => sum + o.priceDelta, 0);
    return item.price + extra;
  });

  readonly canConfirmCustomization = computed(() => {
    const item = this.customizingItem();
    if (!item) {
      return false;
    }

    const selections = this.selectedOptionsByGroup();
    return item.optionGroups.every(
      (g) => !g.isRequired || (selections[g.publicId]?.length ?? 0) > 0
    );
  });

  readonly groupedMenu = computed(() => {
    const categoryId = this.selectedCategoryId();
    const query = this.searchQuery().trim().toLowerCase();

    const items = this.menuItems().filter((item) => {
      if (categoryId && item.categoryPublicId !== categoryId) {
        return false;
      }

      if (query) {
        const matchesName = item.name.toLowerCase().includes(query);
        const matchesDescription = item.description.toLowerCase().includes(query);
        if (!matchesName && !matchesDescription) {
          return false;
        }
      }

      return true;
    });

    const map = new Map<string, MenuItem[]>();

    for (const item of items) {
      const list = map.get(item.categoryName) ?? [];
      list.push(item);
      map.set(item.categoryName, list);
    }

    return Array.from(map.entries()).map(([categoryName, groupItems]) => ({
      categoryName,
      items: groupItems
    }));
  });

  ngOnInit(): void {
    this.readRecentGuestOrder();
    this.route.paramMap.subscribe((params) => {
      const slug = params.get('storeSlug');
      if (!slug) {
        return;
      }

      this.tenant.setStoreSlug(slug);
      this.cart.setStore(slug);
      this.loadStoreData(slug);
    });
  }

  ngOnDestroy(): void {
    this.tenant.setStoreSlug(null);
  }

  onAddItemClicked(item: MenuItem): void {
    if (item.optionGroups.length === 0) {
      this.cart.addItem(item, []);
      return;
    }

    const initial: Record<string, string[]> = {};
    for (const group of item.optionGroups) {
      if (group.isRequired && !group.allowMultiple && group.options.length > 0) {
        initial[group.publicId] = [group.options[0].publicId];
      }
    }

    this.selectedOptionsByGroup.set(initial);
    this.customizingItem.set(item);
  }

  closeCustomize(): void {
    this.customizingItem.set(null);
    this.selectedOptionsByGroup.set({});
  }

  isOptionSelected(groupPublicId: string, optionPublicId: string): boolean {
    return (this.selectedOptionsByGroup()[groupPublicId] ?? []).includes(optionPublicId);
  }

  onOptionToggle(groupPublicId: string, optionPublicId: string, allowMultiple: boolean): void {
    this.selectedOptionsByGroup.update((current) => {
      if (!allowMultiple) {
        return { ...current, [groupPublicId]: [optionPublicId] };
      }

      const existing = current[groupPublicId] ?? [];
      const next = existing.includes(optionPublicId)
        ? existing.filter((id) => id !== optionPublicId)
        : [...existing, optionPublicId];
      return { ...current, [groupPublicId]: next };
    });
  }

  confirmCustomization(): void {
    const item = this.customizingItem();
    if (!item || !this.canConfirmCustomization()) {
      return;
    }

    this.cart.addItem(item, this.resolveSelectedCartOptions(item));
    this.closeCustomize();
  }

  applyPromo(): void {
    const code = this.promoInput.trim();
    if (!code) {
      return;
    }
    this.promoError.set(null);
    this.validatingPromo.set(true);

    this.api.validatePromo(code, this.cart.totalAmount()).subscribe({
      next: (res) => {
        this.validatingPromo.set(false);
        if (res.isValid) {
          this.appliedPromo.set(res);
          this.promoInput = '';
        } else {
          this.promoError.set(res.message || res.error || 'Invalid promo code.');
        }
      },
      error: (err) => {
        this.validatingPromo.set(false);
        this.promoError.set(err?.error?.error ?? err?.error?.message ?? 'Promo code could not be applied.');
      }
    });
  }

  removePromo(): void {
    this.appliedPromo.set(null);
    this.promoError.set(null);
  }

  /**
   * Switches order between Delivery and Pickup, setting sensible default payment options:
   * - Delivery: defaults to PayOnDelivery (Cash or GCash QR to rider).
   * - Pickup: defaults to GCashOnline (express pre-paid order).
   */
  setOrderType(type: OrderType): void {
    this.orderType.set(type);
    if (type === OrderType.Delivery) {
      this.selectedPaymentMethod.set(PaymentMethod.PayOnDelivery);
    } else {
      this.selectedPaymentMethod.set(PaymentMethod.GCashOnline);
    }
  }

  openReviewsModal(): void {
    const slug = this.store()?.slug;
    if (!slug) {
      return;
    }
    this.showingReviewsModal.set(true);
    this.loadingReviews.set(true);
    this.api.getStoreReviews(slug).subscribe({
      next: (summary) => {
        this.storeReviews.set(summary.recentReviews);
        this.loadingReviews.set(false);
      },
      error: () => this.loadingReviews.set(false)
    });
  }

  closeReviewsModal(): void {
    this.showingReviewsModal.set(false);
  }

  checkout(): void {
    this.orderError.set(null);

    const isGuest = !this.auth.isAuthenticated();
    const name = this.customerName.trim() || (this.auth.user()?.fullName ?? '');

    if (isGuest && !name) {
      this.orderError.set('Please enter your full name.');
      return;
    }

    const phone = this.customerPhone.trim();
    if (!phone) {
      this.orderError.set('Please enter your contact phone number.');
      return;
    }

    const address = this.deliveryAddress.trim();
    if (this.orderType() === OrderType.Delivery) {
      if (!address) {
        this.orderError.set('Please enter your delivery address.');
        return;
      }
      if (this.isBelowDeliveryMinimum()) {
        const min = this.store()?.minimumDeliveryOrder ?? 0;
        this.orderError.set(`Minimum order for delivery is $${min.toFixed(2)}.`);
        return;
      }
    }

    if (this.selectedPaymentMethod() === PaymentMethod.GCashManual) {
      const last4 = this.paymentRefLast4.trim();
      if (!last4 || last4.length !== 4 || !/^\d{4}$/.test(last4)) {
        this.orderError.set('Please enter the exact last 4 digits of your GCash payment reference number.');
        return;
      }
    }

    this.submitting.set(true);

    this.api
      .placeOrder({
        items: this.cart.items().map((i) => ({
          menuItemPublicId: i.menuItemPublicId,
          quantity: i.quantity,
          selectedOptionPublicIds: i.selectedOptions.map((o) => o.publicId)
        })),
        orderType: this.orderType(),
        customerName: name,
        customerPhone: phone,
        customerEmail: this.customerEmail.trim() || null,
        deliveryAddress: this.orderType() === OrderType.Delivery ? address : null,
        deliveryLandmark: this.orderType() === OrderType.Delivery ? (this.deliveryLandmark.trim() || null) : null,
        promoCode: this.appliedPromo()?.code ?? null,
        notes: this.notes,
        paymentMethod: this.selectedPaymentMethod(),
        cashChangeFor: this.orderType() === OrderType.Delivery && this.selectedPaymentMethod() === PaymentMethod.PayOnDelivery && this.cashChangeFor ? Number(this.cashChangeFor) : null,
        paymentRefLast4: this.selectedPaymentMethod() === PaymentMethod.GCashManual ? this.paymentRefLast4.trim() : null
      })
      .subscribe({
        next: (createdOrder) => {
          this.submitting.set(false);
          this.cart.clear();
          this.removePromo();
          this.notes = '';
          this.paymentRefLast4 = '';

          if (createdOrder.checkoutUrl) {
            if (isGuest) {
              this.saveRecentGuestOrder(createdOrder.publicId);
            }
            window.location.href = createdOrder.checkoutUrl;
            return;
          }

          if (isGuest) {
            this.saveRecentGuestOrder(createdOrder.publicId);
            void this.router.navigate(['/', createdOrder.storeSlug, 'orders', createdOrder.publicId]);
          } else {
            void this.router.navigateByUrl('/orders');
          }
        },
        error: (err) => {
          this.submitting.set(false);
          this.orderError.set(err?.error?.error ?? err?.error?.detail ?? err?.error?.message ?? err?.message ?? 'Could not place order.');
        }
      });
  }

  private saveRecentGuestOrder(publicId: string): void {
    try {
      const key = 'food_ordering_guest_orders';
      const existing: string[] = JSON.parse(localStorage.getItem(key) ?? '[]');
      if (!existing.includes(publicId)) {
        existing.unshift(publicId);
        localStorage.setItem(key, JSON.stringify(existing.slice(0, 10)));
      }
      this.recentGuestOrderId.set(publicId);
    } catch {
      // Ignore storage errors
    }
  }

  private readRecentGuestOrder(): void {
    try {
      const key = 'food_ordering_guest_orders';
      const existing: string[] = JSON.parse(localStorage.getItem(key) ?? '[]');
      if (existing.length > 0) {
        this.recentGuestOrderId.set(existing[0]);
      }
    } catch {
      // Ignore storage errors
    }
  }

  private resolveSelectedCartOptions(item: MenuItem): CartSelectedOption[] {
    const selections = this.selectedOptionsByGroup();
    const resolved: CartSelectedOption[] = [];

    for (const group of item.optionGroups) {
      const selectedIds = selections[group.publicId] ?? [];
      for (const opt of group.options) {
        if (selectedIds.includes(opt.publicId)) {
          resolved.push({
            publicId: opt.publicId,
            groupName: group.name,
            name: opt.name,
            priceDelta: opt.priceDelta
          });
        }
      }
    }

    return resolved;
  }

  private loadStoreData(slug: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.getStoreBySlug(slug).subscribe({
      next: (store) => {
        this.store.set(store);
        this.api.getCategories().subscribe((cats) => this.categories.set(cats));
        this.api.getMenuItems(true).subscribe({
          next: (items) => {
            this.menuItems.set(items);
            this.loading.set(false);
          },
          error: () => {
            this.error.set('Could not load store menu.');
            this.loading.set(false);
          }
        });
      },
      error: () => {
        this.error.set(`Store '${slug}' was not found.`);
        this.loading.set(false);
      }
    });
  }
}
