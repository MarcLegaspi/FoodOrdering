import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import {
  Category,
  CustomerType,
  MenuItem,
  Order,
  OrderStatus,
  OrderType,
  PaymentCollectionMethod,
  PaymentMethod,
  PaymentStatus,
  PromoCode,
  PromoDiscountType,
  SaveMenuItemOptionGroupRequest,
  SaveMenuItemRequest,
  Store,
  StoreMember,
  StoreRole,
  orderStatusLabel,
  orderTypeLabel,
  storeRoleLabel
} from '../../core/models/api.models';
import { FoodOrderingApiService } from '../../core/services/food-ordering-api.service';
import { OrderLiveService, upsertOrderList } from '../../core/services/order-live.service';
import { TenantService } from '../../core/tenant/tenant.service';
import { resolveImageUrl } from '../../core/utils/media-url';

@Component({
  selector: 'app-store-manage',
  imports: [CurrencyPipe, DatePipe, FormsModule],
  template: `
    <div class="space-y-6">
      <div class="flex flex-wrap items-center justify-between gap-4">
        <div>
          <div class="flex items-center gap-2.5">
            <h1 class="text-2xl font-bold text-slate-900">Manage: {{ storeSlug() }}</h1>
            @if (orderLive.connectionState() === 'connected') {
              <span class="rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-medium text-emerald-700">
                Live
              </span>
            }
            @if (store()?.averageRating; as rating) {
              <span class="inline-flex items-center gap-1 rounded-full bg-amber-50 px-2.5 py-0.5 text-xs font-bold text-amber-800 border border-amber-200">
                ★ {{ rating.toFixed(1) }} ({{ store()?.reviewCount }} {{ store()?.reviewCount === 1 ? 'review' : 'reviews' }})
              </span>
            }
          </div>
          <p class="mt-1 text-sm text-slate-500">Store dashboard for orders, menu, and team members.</p>
        </div>

        <div class="flex flex-wrap items-center gap-3">
          @if (store(); as activeStore) {
            @if (isManager()) {
              <button
                type="button"
                (click)="toggleAcceptingOrders()"
                class="rounded-lg border px-3 py-1.5 text-sm font-medium transition"
                [class]="activeStore.isAcceptingOrders
                  ? 'border-emerald-200 bg-emerald-50 text-emerald-700 hover:bg-emerald-100'
                  : 'border-red-200 bg-red-50 text-red-700 hover:bg-red-100'"
              >
                {{ activeStore.isAcceptingOrders ? 'Accepting Orders: Open' : 'Accepting Orders: Closed' }}
              </button>
            } @else {
              <span
                class="rounded-full px-2.5 py-1 text-xs font-medium"
                [class]="activeStore.isAcceptingOrders ? 'bg-emerald-50 text-emerald-700' : 'bg-red-50 text-red-700'"
              >
                {{ activeStore.isAcceptingOrders ? 'Open' : 'Closed' }}
              </span>
            }
          }

          <div class="flex gap-2 rounded-lg border border-slate-200 bg-white p-1 text-sm font-medium">
          <button
            type="button"
            (click)="tab.set('orders')"
            [class]="tab() === 'orders' ? 'rounded-md bg-slate-900 px-3 py-1.5 text-white' : 'px-3 py-1.5 text-slate-600'"
          >
            Orders ({{ orders().length }})
          </button>
          <button
            type="button"
            (click)="tab.set('menu')"
            [class]="tab() === 'menu' ? 'rounded-md bg-slate-900 px-3 py-1.5 text-white' : 'px-3 py-1.5 text-slate-600'"
          >
            Menu ({{ menuItems().length }})
          </button>
          <button
            type="button"
            (click)="switchToPromos()"
            [class]="tab() === 'promos' ? 'rounded-md bg-slate-900 px-3 py-1.5 text-white' : 'px-3 py-1.5 text-slate-600'"
          >
            Promos ({{ promos().length }})
          </button>
          @if (isManager()) {
            <button
              type="button"
              (click)="switchToTeam()"
              [class]="tab() === 'team' ? 'rounded-md bg-slate-900 px-3 py-1.5 text-white' : 'px-3 py-1.5 text-slate-600'"
            >
              Team
            </button>
            <button
              type="button"
              (click)="tab.set('settings')"
              [class]="tab() === 'settings' ? 'rounded-md bg-slate-900 px-3 py-1.5 text-white' : 'px-3 py-1.5 text-slate-600'"
            >
              Settings
            </button>
          }
        </div>
      </div>

      @if (liveAlert(); as alertText) {
        <div class="flex items-center justify-between gap-3 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm font-medium text-amber-900">
          <span>{{ alertText }}</span>
          <button
            type="button"
            (click)="liveAlert.set(null)"
            class="text-xs font-semibold text-amber-800 underline"
          >
            Dismiss
          </button>
        </div>
      }
      @if (message()) {
        <div class="rounded-lg bg-emerald-50 p-3 text-sm text-emerald-700">{{ message() }}</div>
      }
      @if (error()) {
        <div class="rounded-lg bg-red-50 p-3 text-sm text-red-700">{{ error() }}</div>
      }

      <!-- ORDERS TAB -->
      @if (tab() === 'orders') {
        <div class="space-y-4">
          <div class="flex flex-wrap items-center justify-between gap-3">
            <div class="flex flex-wrap gap-2">
              @for (opt of orderStatusOptions; track opt.label) {
                <button
                  type="button"
                  (click)="orderStatusFilter.set(opt.value)"
                  class="rounded-full px-3 py-1.5 text-xs font-medium transition"
                  [class]="orderStatusFilter() === opt.value
                    ? 'bg-slate-900 text-white'
                    : 'border border-slate-300 bg-white text-slate-700 hover:bg-slate-50'"
                >
                  {{ opt.label }} ({{ orderCount(opt.value) }})
                </button>
              }
            </div>

            <button
              type="button"
              (click)="toggleSound()"
              class="rounded-lg border px-3 py-1.5 text-xs font-medium transition"
              [class]="soundEnabled()
                ? 'border-emerald-200 bg-emerald-50 text-emerald-700 hover:bg-emerald-100'
                : 'border-slate-300 bg-white text-slate-600 hover:bg-slate-50'"
            >
              {{ soundEnabled() ? 'Sound Alerts: On' : 'Sound Alerts: Off' }}
            </button>
          </div>

          @if (filteredOrders().length === 0) {
            <p class="rounded-xl border border-dashed border-slate-300 bg-white p-8 text-center text-sm text-slate-500">
              {{ orders().length > 0 ? 'No orders with this status.' : 'No orders for this store yet.' }}
            </p>
          }

          @for (order of filteredOrders(); track order.publicId) {
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
              <div class="flex flex-wrap items-center justify-between gap-2">
                <div>
                  <span class="font-bold text-slate-900">{{ order.orderNumber }}</span>
                  <span class="ml-2 text-xs text-slate-400">{{ order.createdAtUtc | date: 'short' }}</span>
                </div>
                <div class="flex items-center gap-2">
                  <span class="rounded-full bg-amber-50 px-2.5 py-0.5 text-xs font-medium text-amber-800">
                    {{ orderTypeText(order.orderType) }}
                  </span>
                  @if (order.paymentMethod === PaymentMethod.PayOnDelivery) {
                    <span class="rounded-full bg-amber-50 border border-amber-200 px-2 py-0.5 text-xs font-medium text-amber-800">
                      💵 Pay on Delivery
                    </span>
                  } @else if (order.paymentMethod === PaymentMethod.GCashOnline) {
                    <span class="rounded-full bg-blue-50 border border-blue-200 px-2 py-0.5 text-xs font-bold text-blue-800">
                      📱 Pre-paid GCash
                    </span>
                  } @else if (order.paymentMethod === PaymentMethod.GCashManual) {
                    <span class="rounded-full bg-emerald-50 border border-emerald-300 px-2 py-0.5 text-xs font-bold text-emerald-800">
                      💸 GCash ({{ order.paymentReference || 'Manual' }})
                    </span>
                  } @else if (order.paymentMethod === PaymentMethod.PayAtCounter) {
                    <span class="rounded-full bg-slate-100 border border-slate-200 px-2 py-0.5 text-xs font-medium text-slate-700">
                      🏪 Pay at Counter
                    </span>
                  }
                  <span
                    class="rounded-full px-3 py-1 text-xs font-semibold"
                    [class]="order.status === OrderStatus.OutForDelivery ? 'bg-purple-100 text-purple-900 border border-purple-200' : 'bg-slate-100 text-slate-700'"
                  >
                    {{ statusText(order.status) }}
                  </span>
                  <div class="text-right">
                    <span class="font-bold text-slate-900">{{ order.totalAmount | currency }}</span>
                    @if (order.discountAmount > 0) {
                      <div class="text-[11px] font-semibold text-emerald-600">
                        Promo ({{ order.promoCode }}): -{{ order.discountAmount | currency }}
                      </div>
                    }
                    @if (order.orderType === OrderType.Delivery) {
                      <div class="text-[11px] text-slate-500">
                        Food: {{ order.subtotal | currency }} · Delivery: {{ order.deliveryFee > 0 ? (order.deliveryFee | currency) : 'FREE' }}
                      </div>
                    }
                  </div>
                </div>
              </div>

              <div class="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-slate-600">
                <span class="font-medium text-slate-800">
                  👤 {{ order.customerName || 'Customer' }}
                  @if (order.customerType === CustomerType.Guest) {
                    <span class="ml-1 inline-flex items-center rounded bg-amber-100 px-1.5 py-0.5 text-[10px] font-bold text-amber-800">
                      GUEST
                    </span>
                  }
                </span>
                <span>📞 {{ order.customerPhone }}</span>
                @if (order.customerEmail) {
                  <span>✉ {{ order.customerEmail }}</span>
                }
                @if (order.deliveryAddress) {
                  <span>📍 Deliver to: {{ order.deliveryAddress }}</span>
                }
                @if (order.deliveryLandmark) {
                  <span class="rounded bg-amber-50 px-1.5 py-0.5 text-amber-800">🏢 Landmark: {{ order.deliveryLandmark }}</span>
                }
                @if (order.cashChangeFor) {
                  <span class="rounded bg-emerald-50 px-1.5 py-0.5 text-emerald-800 font-semibold border border-emerald-200">
                    💵 Change for {{ order.cashChangeFor | currency }}
                  </span>
                }
                @if (order.notes) {
                  <span class="italic">Notes: {{ order.notes }}</span>
                }
              </div>

              <ul class="mt-3 space-y-1 text-sm text-slate-600">
                @for (item of order.items; track $index) {
                  <li>
                    <div>{{ item.quantity }}× {{ item.itemName }} ({{ item.lineTotal | currency }})</div>
                    @if (item.options.length > 0) {
                      <p class="text-xs text-slate-400">
                        @for (opt of item.options; track $index; let last = $last) {
                          {{ opt.groupName }}: {{ opt.optionName }}@if (opt.priceDelta > 0) { (+{{ opt.priceDelta | currency }}) }@if (!last) {, }
                        }
                      </p>
                    }
                  </li>
                }
              </ul>

              @if (order.review; as rev) {
                <div class="mt-3 rounded-lg bg-amber-50/70 border border-amber-200/80 p-2.5 text-xs">
                  <div class="flex items-center justify-between font-semibold text-amber-900">
                    <span class="flex items-center gap-1">
                      <span class="text-amber-500">★</span> {{ rev.rating }}/5 Review ({{ rev.customerName }})
                    </span>
                    <span class="text-slate-400 font-normal">{{ rev.createdAtUtc | date:'shortDate' }}</span>
                  </div>
                  @if (rev.comment) {
                    <p class="mt-1 text-slate-700 italic">"{{ rev.comment }}"</p>
                  }
                </div>
              }

              @if (order.driverName) {
                <div class="mt-2.5 flex flex-wrap items-center justify-between gap-2 rounded-lg bg-purple-50/80 p-2.5 text-xs border border-purple-200">
                  <div class="flex items-center gap-2">
                    <span class="text-base">🛵</span>
                    <span class="font-bold text-purple-900">Rider: {{ order.driverName }}</span>
                    <a [href]="'tel:' + order.driverPhone" class="text-purple-700 underline font-medium">📞 {{ order.driverPhone }}</a>
                    @if (order.dispatchedAtUtc) {
                      <span class="text-slate-500">· Dispatched {{ order.dispatchedAtUtc | date:'shortTime' }}</span>
                    }
                  </div>
                  @if (order.status === OrderStatus.OutForDelivery) {
                    <span class="rounded-full bg-purple-600 px-2 py-0.5 text-[10px] font-bold text-white uppercase tracking-wider animate-pulse">
                      Out for Delivery
                    </span>
                  }
                </div>
              }

              @if (order.status === OrderStatus.Completed && (order.deliveredAtUtc || order.paymentCollectedVia)) {
                <div class="mt-2.5 flex flex-wrap items-center gap-3 text-xs text-slate-500 border-t border-slate-100 pt-2">
                  @if (order.deliveredAtUtc) {
                    <span>Delivered at {{ order.deliveredAtUtc | date:'short' }}</span>
                  }
                  @if (order.paymentCollectedVia) {
                    <span class="font-medium text-emerald-700">
                      Payment collected via {{ order.paymentCollectedVia === PaymentCollectionMethod.Cash ? 'Cash' : 'GCash QR' }}
                    </span>
                  }
                </div>
              }

              @if (order.status !== OrderStatus.Completed && order.status !== OrderStatus.Cancelled) {
                <div class="mt-4 flex flex-wrap gap-2">
                  @if (order.paymentMethod === PaymentMethod.GCashManual && order.paymentStatus !== PaymentStatus.Paid) {
                    <button
                      type="button"
                      (click)="verifyGcashPayment(order.publicId)"
                      class="rounded-md bg-emerald-600 px-3 py-1 text-xs font-semibold text-white hover:bg-emerald-700 flex items-center gap-1 shadow-xs"
                      title="Verify GCash Reference and mark as Paid"
                    >
                      <span>✓</span> Verify GCash ({{ order.paymentReference || 'Last 4' }})
                    </button>
                  }
                  @if (order.status === OrderStatus.Pending) {
                    <button
                      type="button"
                      (click)="advanceOrder(order.publicId, OrderStatus.Confirmed)"
                      class="rounded-md bg-slate-900 px-3 py-1 text-xs font-medium text-white hover:bg-slate-800"
                    >
                      Confirm
                    </button>
                  }
                  @if (order.status === OrderStatus.Confirmed) {
                    <button
                      type="button"
                      (click)="advanceOrder(order.publicId, OrderStatus.Preparing)"
                      class="rounded-md bg-slate-900 px-3 py-1 text-xs font-medium text-white hover:bg-slate-800"
                    >
                      Start Preparing
                    </button>
                  }
                  @if (order.status === OrderStatus.Preparing) {
                    <button
                      type="button"
                      (click)="advanceOrder(order.publicId, OrderStatus.Ready)"
                      class="rounded-md bg-slate-900 px-3 py-1 text-xs font-medium text-white hover:bg-slate-800"
                    >
                      Mark Ready
                    </button>
                    @if (order.orderType === OrderType.Delivery) {
                      <button
                        type="button"
                        (click)="openAssignDriver(order)"
                        class="rounded-md bg-purple-600 px-3 py-1 text-xs font-medium text-white hover:bg-purple-700"
                      >
                        🛵 Assign Rider & Dispatch
                      </button>
                    }
                  }
                  @if (order.status === OrderStatus.Ready) {
                    @if (order.orderType === OrderType.Delivery) {
                      <button
                        type="button"
                        (click)="openAssignDriver(order)"
                        class="rounded-md bg-purple-600 px-3 py-1 text-xs font-medium text-white hover:bg-purple-700"
                      >
                        🛵 Assign Rider & Dispatch
                      </button>
                    } @else {
                      <button
                        type="button"
                        (click)="advanceOrder(order.publicId, OrderStatus.Completed)"
                        class="rounded-md bg-emerald-700 px-3 py-1 text-xs font-medium text-white hover:bg-emerald-800"
                      >
                        Complete Pickup
                      </button>
                    }
                  }
                  @if (order.status === OrderStatus.OutForDelivery) {
                    <button
                      type="button"
                      (click)="openCompleteDelivery(order)"
                      class="rounded-md bg-emerald-700 px-3 py-1 text-xs font-semibold text-white hover:bg-emerald-800"
                    >
                      ✅ Complete Delivery (Enter PIN)
                    </button>
                  }
                  <button
                    type="button"
                    (click)="advanceOrder(order.publicId, OrderStatus.Cancelled)"
                    class="rounded-md border border-red-200 px-3 py-1 text-xs font-medium text-red-700 hover:bg-red-50"
                  >
                    Cancel
                  </button>
                </div>
              }
            </div>
          }
        </div>
      }

      <!-- MENU TAB -->
      @if (tab() === 'menu') {
        <div class="grid gap-6 lg:grid-cols-2">
          <div class="rounded-xl border border-slate-200 bg-white p-5">
            <h2 class="font-bold text-slate-900">{{ editingCategoryPublicId() ? 'Edit Category' : 'Add Category' }}</h2>
            <form (ngSubmit)="saveCategory()" class="mt-3 flex flex-wrap gap-2">
              <input
                type="text"
                name="categoryName"
                [(ngModel)]="categoryName"
                placeholder="e.g. Burgers"
                required
                class="min-w-0 flex-1 rounded-lg border border-slate-300 px-3 py-2 text-sm"
              />
              <input
                type="number"
                name="categorySortOrder"
                [(ngModel)]="categorySortOrder"
                class="w-20 rounded-lg border border-slate-300 px-3 py-2 text-sm"
              />
              <button
                type="submit"
                class="rounded-lg bg-slate-900 px-4 py-2 text-sm font-semibold text-white"
              >
                {{ editingCategoryPublicId() ? 'Save' : 'Add' }}
              </button>
              @if (editingCategoryPublicId()) {
                <button
                  type="button"
                  (click)="cancelCategoryEdit()"
                  class="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
                >
                  Cancel
                </button>
              }
            </form>

            <ul class="mt-4 divide-y divide-slate-100 text-sm">
              @for (cat of categories(); track cat.publicId; let i = $index) {
                <li class="flex items-center justify-between gap-3 py-2">
                  <div>
                    <span class="font-medium text-slate-800">{{ cat.name }}</span>
                    <span class="ml-2 text-xs text-slate-400">Sort: {{ cat.sortOrder }}</span>
                  </div>
                  <div class="flex items-center gap-1.5">
                    <button
                      type="button"
                      [disabled]="i === 0"
                      (click)="moveCategory(i, -1)"
                      title="Move up"
                      class="rounded border border-slate-300 px-2 py-0.5 text-xs disabled:opacity-40"
                    >
                      ↑
                    </button>
                    <button
                      type="button"
                      [disabled]="i === categories().length - 1"
                      (click)="moveCategory(i, 1)"
                      title="Move down"
                      class="rounded border border-slate-300 px-2 py-0.5 text-xs disabled:opacity-40"
                    >
                      ↓
                    </button>
                    <button
                      type="button"
                      (click)="startEditCategory(cat)"
                      class="rounded border border-slate-300 px-2.5 py-0.5 text-xs"
                    >
                      Edit
                    </button>
                    <button
                      type="button"
                      (click)="deleteCategory(cat.publicId)"
                      class="rounded border border-red-200 px-2.5 py-0.5 text-xs text-red-700"
                    >
                      Delete
                    </button>
                  </div>
                </li>
              }
            </ul>
          </div>

          <div class="rounded-xl border border-slate-200 bg-white p-5">
            <h2 class="font-bold text-slate-900">{{ editingItemPublicId() ? 'Edit Menu Item' : 'Add Menu Item' }}</h2>
            <form (ngSubmit)="saveMenuItem()" class="mt-3 space-y-3">
              <select
                name="selectedCategoryId"
                [(ngModel)]="selectedCategoryId"
                required
                class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
              >
                <option value="">Select category...</option>
                @for (cat of categories(); track cat.publicId) {
                  <option [value]="cat.publicId">{{ cat.name }}</option>
                }
              </select>

              <div class="grid grid-cols-3 gap-2">
                <input
                  type="text"
                  name="itemName"
                  [(ngModel)]="itemName"
                  placeholder="Item name"
                  required
                  class="col-span-2 rounded-lg border border-slate-300 px-3 py-2 text-sm"
                />
                <input
                  type="number"
                  step="0.01"
                  name="itemPrice"
                  [(ngModel)]="itemPrice"
                  placeholder="Price"
                  required
                  class="rounded-lg border border-slate-300 px-3 py-2 text-sm"
                />
              </div>

              <input
                type="text"
                name="itemDescription"
                [(ngModel)]="itemDescription"
                placeholder="Description"
                class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
              />

              <div class="space-y-2">
                <div class="flex flex-wrap items-center gap-2">
                  <label class="cursor-pointer rounded-lg border border-slate-300 bg-slate-50 px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-100">
                    {{ uploadingImage() ? 'Uploading...' : 'Upload Image' }}
                    <input
                      type="file"
                      accept=".jpg,.jpeg,.png,.webp,.gif"
                      [disabled]="uploadingImage()"
                      (change)="onMenuImageSelected($event)"
                      class="hidden"
                    />
                  </label>
                  @if (imageUrl(itemImageUrl); as previewSrc) {
                    <img
                      [src]="previewSrc"
                      alt="Menu item preview"
                      class="h-10 w-10 rounded-md border border-slate-200 object-cover"
                    />
                    <button
                      type="button"
                      (click)="itemImageUrl = ''"
                      class="text-xs text-slate-500 hover:text-slate-800"
                    >
                      Clear
                    </button>
                  }
                </div>
                <input
                  type="text"
                  name="itemImageUrl"
                  [(ngModel)]="itemImageUrl"
                  placeholder="Or paste image URL (e.g. /images/menu/cheeseburger.jpg)"
                  class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                />
              </div>

              <div class="space-y-2 rounded-lg border border-slate-200 bg-slate-50/60 p-3">
                <div class="flex items-center justify-between">
                  <span class="text-xs font-semibold text-slate-700">Options & Add-ons (Sizes, Toppings)</span>
                  <button
                    type="button"
                    (click)="addOptionGroup()"
                    class="rounded border border-slate-300 bg-white px-2.5 py-1 text-xs font-medium text-slate-700 hover:bg-slate-50"
                  >
                    + Add Group
                  </button>
                </div>

                @for (group of itemOptionGroups; track $index; let gIndex = $index) {
                  <div class="space-y-2 rounded-lg border border-slate-200 bg-white p-3">
                    <div class="flex flex-wrap items-center gap-2">
                      <input
                        type="text"
                        [name]="'groupName-' + gIndex"
                        [(ngModel)]="group.name"
                        placeholder="Group name (e.g. Size, Toppings)"
                        required
                        class="min-w-0 flex-1 rounded border border-slate-300 px-2.5 py-1 text-xs"
                      />
                      <label class="flex items-center gap-1 text-xs text-slate-600">
                        <input
                          type="checkbox"
                          [name]="'groupReq-' + gIndex"
                          [(ngModel)]="group.isRequired"
                        />
                        Required
                      </label>
                      <label class="flex items-center gap-1 text-xs text-slate-600">
                        <input
                          type="checkbox"
                          [name]="'groupMulti-' + gIndex"
                          [(ngModel)]="group.allowMultiple"
                        />
                        Multi
                      </label>
                      <button
                        type="button"
                        (click)="removeOptionGroup(gIndex)"
                        class="text-xs text-red-600 hover:underline"
                      >
                        Remove
                      </button>
                    </div>

                    <div class="space-y-1.5 pl-2">
                      @for (opt of group.options; track $index; let oIndex = $index) {
                        <div class="flex items-center gap-2">
                          <input
                            type="text"
                            [name]="'optName-' + gIndex + '-' + oIndex"
                            [(ngModel)]="opt.name"
                            placeholder="Option (e.g. Large)"
                            required
                            class="min-w-0 flex-1 rounded border border-slate-300 px-2 py-1 text-xs"
                          />
                          <input
                            type="number"
                            step="0.01"
                            min="0"
                            [name]="'optPrice-' + gIndex + '-' + oIndex"
                            [(ngModel)]="opt.priceDelta"
                            placeholder="+Price"
                            class="w-20 rounded border border-slate-300 px-2 py-1 text-xs"
                          />
                          <button
                            type="button"
                            (click)="removeOption(gIndex, oIndex)"
                            class="text-xs text-slate-400 hover:text-red-600"
                          >
                            ✕
                          </button>
                        </div>
                      }
                      <button
                        type="button"
                        (click)="addOption(gIndex)"
                        class="text-xs font-medium text-slate-600 hover:text-slate-900"
                      >
                        + Add Option
                      </button>
                    </div>
                  </div>
                }
              </div>

              <div class="flex gap-2">
                <button
                  type="submit"
                  [disabled]="uploadingImage()"
                  class="flex-1 rounded-lg bg-slate-900 py-2 text-sm font-semibold text-white disabled:opacity-50"
                >
                  {{ editingItemPublicId() ? 'Save Changes' : 'Add Menu Item' }}
                </button>
                @if (editingItemPublicId()) {
                  <button
                    type="button"
                    (click)="cancelEdit()"
                    class="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
                  >
                    Cancel
                  </button>
                }
              </div>
            </form>
          </div>
        </div>

        <div class="rounded-xl border border-slate-200 bg-white p-5">
          <h2 class="font-bold text-slate-900">Menu Items</h2>
          <div class="mt-3 divide-y divide-slate-100">
            @for (item of menuItems(); track item.publicId) {
              <div class="flex flex-wrap items-center justify-between gap-2 py-3 text-sm">
                <div class="flex items-center gap-3">
                  @if (imageUrl(item.imageUrl); as src) {
                    <img
                      [src]="src"
                      [alt]="item.name"
                      class="h-10 w-10 rounded-md object-cover"
                    />
                  }
                  <div>
                    <span class="font-semibold text-slate-900">{{ item.name }}</span>
                    <span class="ml-2 text-xs text-slate-400">{{ item.categoryName }}</span>
                    @if (item.optionGroups.length > 0) {
                      <span class="ml-2 text-xs text-slate-500">· {{ item.optionGroups.length }} option group(s)</span>
                    }
                  </div>
                </div>
                <div class="flex items-center gap-3">
                  <span class="font-medium text-slate-900">{{ item.price | currency }}</span>
                  <button
                    type="button"
                    (click)="startEdit(item)"
                    class="rounded border border-slate-300 px-2.5 py-1 text-xs"
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    (click)="toggleAvailability(item)"
                    class="rounded border border-slate-300 px-2.5 py-1 text-xs"
                  >
                    {{ item.isAvailable ? 'Available' : 'Sold out' }}
                  </button>
                  <button
                    type="button"
                    (click)="deleteItem(item.publicId)"
                    class="rounded border border-red-200 px-2.5 py-1 text-xs text-red-700"
                  >
                    Delete
                  </button>
                </div>
              </div>
            }
          </div>
        </div>
      }

      <!-- PROMOS TAB -->
      @if (tab() === 'promos') {
        <div class="grid gap-6 lg:grid-cols-3">
          @if (isManager()) {
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
              <h2 class="text-lg font-bold text-slate-900">Create Promo Code</h2>
              <p class="mt-1 text-xs text-slate-500">Add discounts for registered customer orders.</p>

              <form (ngSubmit)="createPromo()" class="mt-4 space-y-4">
                <div>
                  <label class="mb-1 block text-xs font-semibold text-slate-700">Promo Code</label>
                  <input
                    type="text"
                    name="promoCode"
                    [(ngModel)]="promoCode"
                    placeholder="e.g. SAVE10 or WELCOME5"
                    class="w-full uppercase rounded-lg border border-slate-300 px-3 py-2 text-sm font-mono tracking-wider"
                    required
                  />
                </div>

                <div class="grid grid-cols-2 gap-3">
                  <div>
                    <label class="mb-1 block text-xs font-semibold text-slate-700">Type</label>
                    <select
                      name="promoDiscountType"
                      [(ngModel)]="promoDiscountType"
                      class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm bg-white"
                    >
                      <option [ngValue]="PromoDiscountType.Percentage">Percentage (%)</option>
                      <option [ngValue]="PromoDiscountType.FixedAmount">Cash Off ($)</option>
                    </select>
                  </div>
                  <div>
                    <label class="mb-1 block text-xs font-semibold text-slate-700">
                      {{ promoDiscountType === PromoDiscountType.Percentage ? 'Discount (%)' : 'Discount ($)' }}
                    </label>
                    <input
                      type="number"
                      step="any"
                      min="0.01"
                      name="promoDiscountValue"
                      [(ngModel)]="promoDiscountValue"
                      class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                      required
                    />
                  </div>
                </div>

                <div>
                  <label class="mb-1 block text-xs font-semibold text-slate-700">
                    Max Claims / Redemptions <span class="text-slate-400 font-normal">(Optional)</span>
                  </label>
                  <input
                    type="number"
                    min="1"
                    name="promoMaxUses"
                    [(ngModel)]="promoMaxUses"
                    placeholder="e.g. 10 (leave empty for unlimited)"
                    class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                  />
                  <p class="mt-1 text-[11px] text-slate-400">Total claims across all customers before auto-disabling.</p>
                </div>

                <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div>
                    <label class="mb-1 block text-xs font-semibold text-slate-700">
                      Starts At <span class="text-slate-400 font-normal">(Optional)</span>
                    </label>
                    <input
                      type="datetime-local"
                      name="promoStartsAt"
                      [(ngModel)]="promoStartsAt"
                      class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                    />
                  </div>
                  <div>
                    <label class="mb-1 block text-xs font-semibold text-slate-700">
                      Expires At <span class="text-slate-400 font-normal">(Optional)</span>
                    </label>
                    <input
                      type="datetime-local"
                      name="promoExpiresAt"
                      [(ngModel)]="promoExpiresAt"
                      class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                    />
                  </div>
                </div>

                <button
                  type="submit"
                  class="w-full rounded-lg bg-slate-900 py-2.5 text-sm font-semibold text-white hover:bg-slate-800 transition"
                >
                  Create Promo Code
                </button>
              </form>
            </div>
          }

          <div [class]="isManager() ? 'lg:col-span-2' : 'lg:col-span-3'">
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
              <div class="flex items-center justify-between">
                <div>
                  <h2 class="text-lg font-bold text-slate-900">Promo Codes ({{ promos().length }})</h2>
                  <p class="text-xs text-slate-500">Applicable only for registered customers during checkout.</p>
                </div>
                <button
                  type="button"
                  (click)="loadPromos()"
                  class="rounded border border-slate-200 px-2.5 py-1 text-xs text-slate-600 hover:bg-slate-50"
                >
                  Refresh
                </button>
              </div>

              @if (promos().length === 0) {
                <p class="mt-6 rounded-xl border border-dashed border-slate-200 p-8 text-center text-sm text-slate-500">
                  No promo codes created yet. Create your first discount code!
                </p>
              } @else {
                <div class="mt-4 divide-y divide-slate-100">
                  @for (promo of promos(); track promo.publicId) {
                    <div class="flex flex-wrap items-center justify-between gap-4 py-3.5">
                      <div class="space-y-1">
                        <div class="flex items-center gap-2">
                          <span class="rounded bg-slate-100 px-2.5 py-1 font-mono text-sm font-bold text-slate-900">
                            {{ promo.code }}
                          </span>
                          <span
                            class="rounded-full px-2 py-0.5 text-xs font-semibold"
                            [class]="promo.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'"
                          >
                            {{ promo.isActive ? 'Active' : 'Inactive' }}
                          </span>
                          @if (isPromoUpcoming(promo)) {
                            <span class="rounded-full bg-blue-50 px-2 py-0.5 text-xs font-semibold text-blue-700">
                              Upcoming (Starts {{ promo.startsAtUtc | date: 'short' }})
                            </span>
                          }
                          @if (promo.maxUses && promo.usedCount >= promo.maxUses) {
                            <span class="rounded-full bg-amber-50 px-2 py-0.5 text-xs font-semibold text-amber-700">
                              Limit Reached
                            </span>
                          }
                        </div>
                        <div class="text-xs text-slate-600 flex flex-wrap gap-x-4 gap-y-1">
                          <span class="font-medium text-slate-800">
                            Discount: {{ promo.discountType === PromoDiscountType.Percentage ? (promo.discountValue + '% off') : ('$' + promo.discountValue + ' off') }}
                          </span>
                          <span>
                            Claims: <strong>{{ promo.usedCount }}</strong> / {{ promo.maxUses ? promo.maxUses : '∞' }}
                          </span>
                          @if (promo.startsAtUtc) {
                            <span>
                              Starts: {{ promo.startsAtUtc | date: 'short' }}
                            </span>
                          }
                          @if (promo.expiresAtUtc) {
                            <span>
                              Expires: {{ promo.expiresAtUtc | date: 'short' }}
                            </span>
                          } @else if (!promo.startsAtUtc) {
                            <span class="text-slate-400">Always valid</span>
                          }
                        </div>
                      </div>

                      @if (isManager()) {
                        <div class="flex items-center gap-2">
                          <button
                            type="button"
                            (click)="togglePromo(promo.publicId)"
                            class="rounded-lg border px-3 py-1.5 text-xs font-medium transition"
                            [class]="promo.isActive
                              ? 'border-amber-200 bg-amber-50 text-amber-800 hover:bg-amber-100'
                              : 'border-emerald-200 bg-emerald-50 text-emerald-800 hover:bg-emerald-100'"
                          >
                            {{ promo.isActive ? 'Deactivate' : 'Activate' }}
                          </button>
                          <button
                            type="button"
                            (click)="deletePromo(promo.publicId)"
                            class="rounded-lg border border-red-200 bg-red-50 px-3 py-1.5 text-xs font-medium text-red-700 hover:bg-red-100 transition"
                          >
                            Delete
                          </button>
                        </div>
                      }
                    </div>
                  }
                </div>
              }
            </div>
          </div>
        </div>
      }

      <!-- TEAM TAB -->
      @if (tab() === 'team' && isManager()) {
        <div class="grid gap-6 lg:grid-cols-2">
          <div class="rounded-xl border border-slate-200 bg-white p-5">
            <h2 class="font-bold text-slate-900">Add Staff / Manager</h2>
            <form (ngSubmit)="addMember()" class="mt-3 space-y-3">
              <input
                type="email"
                name="memberEmail"
                [(ngModel)]="memberEmail"
                placeholder="Staff email"
                required
                class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
              />
              <input
                type="text"
                name="memberFullName"
                [(ngModel)]="memberFullName"
                placeholder="Full name (if new account)"
                class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
              />
              <input
                type="password"
                name="memberPassword"
                [(ngModel)]="memberPassword"
                placeholder="Initial password (if new account)"
                class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
              />
              <select
                name="memberRole"
                [(ngModel)]="memberRole"
                class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
              >
                <option [ngValue]="StoreRole.Staff">Staff</option>
                <option [ngValue]="StoreRole.Manager">Manager</option>
              </select>
              <button
                type="submit"
                class="w-full rounded-lg bg-slate-900 py-2 text-sm font-semibold text-white"
              >
                Add Member
              </button>
            </form>
          </div>

          <div class="rounded-xl border border-slate-200 bg-white p-5">
            <h2 class="font-bold text-slate-900">Store Team</h2>
            <div class="mt-3 divide-y divide-slate-100 text-sm">
              @for (m of members(); track m.userId) {
                <div class="flex items-center justify-between py-2.5">
                  <div>
                    <p class="font-medium text-slate-900">{{ m.fullName }}</p>
                    <p class="text-xs text-slate-500">{{ m.email }}</p>
                  </div>
                  <div class="flex items-center gap-2">
                    <span class="rounded bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-700">
                      {{ roleText(m.role) }}
                    </span>
                    <button
                      type="button"
                      (click)="removeMember(m.userId)"
                      class="rounded border border-red-200 px-2 py-0.5 text-xs text-red-700"
                    >
                      Remove
                    </button>
                  </div>
                </div>
              }
            </div>
          </div>
        </div>
      }

      <!-- SETTINGS TAB (Manager/Owner only) -->
      @if (tab() === 'settings' && isManager()) {
        <div class="max-w-xl rounded-xl border border-slate-200 bg-white p-6">
          <h2 class="font-bold text-slate-900">Store Profile & Contact Details</h2>
          <p class="mt-1 text-xs text-slate-500">Shown to customers on the storefront and menu page.</p>

          <form (ngSubmit)="saveStoreProfile()" class="mt-4 space-y-3">
            <div>
              <label class="mb-1 block text-xs font-medium text-slate-700">Store Name</label>
              <input
                type="text"
                name="storeName"
                [(ngModel)]="storeName"
                required
                class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
              />
            </div>

            <div class="grid gap-3 sm:grid-cols-2">
              <div>
                <label class="mb-1 block text-xs font-medium text-slate-700">Contact Email</label>
                <input
                  type="email"
                  name="storeEmail"
                  [(ngModel)]="storeEmail"
                  class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                />
              </div>
              <div>
                <label class="mb-1 block text-xs font-medium text-slate-700">Phone</label>
                <input
                  type="text"
                  name="storePhone"
                  [(ngModel)]="storePhone"
                  class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                />
              </div>
            </div>

            <div>
              <label class="mb-1 block text-xs font-medium text-slate-700">Address</label>
              <input
                type="text"
                name="storeAddress"
                [(ngModel)]="storeAddress"
                placeholder="e.g. 123 Main St, Downtown"
                class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
              />
            </div>

            <div>
              <label class="mb-1 block text-xs font-medium text-slate-700">Opening Hours</label>
              <input
                type="text"
                name="storeOpeningHours"
                [(ngModel)]="storeOpeningHours"
                placeholder="e.g. Daily 10:00 AM - 10:00 PM"
                class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
              />
            </div>

            <div class="border-t border-slate-200 pt-4">
              <h3 class="text-sm font-bold text-slate-900">Delivery Rules & Fees</h3>
              <p class="mt-0.5 text-xs text-slate-500">Configure fulfillment rules for delivery orders.</p>

              <div class="mt-3 grid gap-3 sm:grid-cols-2">
                <div>
                  <label class="mb-1 block text-xs font-medium text-slate-700">Fixed Delivery Fee ($)</label>
                  <input
                    type="number"
                    step="0.25"
                    min="0"
                    name="storeDeliveryFee"
                    [(ngModel)]="storeDeliveryFee"
                    placeholder="e.g. 3.00"
                    class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                  />
                </div>
                <div>
                  <label class="mb-1 block text-xs font-medium text-slate-700">Minimum Order for Delivery ($)</label>
                  <input
                    type="number"
                    step="1"
                    min="0"
                    name="storeMinimumDeliveryOrder"
                    [(ngModel)]="storeMinimumDeliveryOrder"
                    placeholder="e.g. 15.00 (0 for no min)"
                    class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                  />
                </div>
              </div>

              <div class="mt-3 grid gap-3 sm:grid-cols-2">
                <div>
                  <label class="mb-1 block text-xs font-medium text-slate-700">Free Delivery on Orders Over ($)</label>
                  <input
                    type="number"
                    step="1"
                    min="0"
                    name="storeFreeDeliveryThreshold"
                    [(ngModel)]="storeFreeDeliveryThreshold"
                    placeholder="e.g. 35.00 (leave blank if none)"
                    class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                  />
                </div>
                <div>
                  <label class="mb-1 block text-xs font-medium text-slate-700">Est. Delivery / Prep Time</label>
                  <input
                    type="text"
                    name="storeEstimatedDeliveryTime"
                    [(ngModel)]="storeEstimatedDeliveryTime"
                    placeholder="e.g. 30-45 mins"
                    class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                  />
                </div>
              </div>
            </div>

            <button
              type="submit"
              [disabled]="savingProfile()"
              class="rounded-lg bg-slate-900 px-5 py-2 text-sm font-semibold text-white hover:bg-slate-800 disabled:opacity-50"
            >
              {{ savingProfile() ? 'Saving...' : 'Save Store Profile' }}
            </button>
          </form>
        </div>
      }

      <!-- Assign Driver Modal -->
      @if (assigningDriverOrder(); as targetOrder) {
        <div class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4 backdrop-blur-xs">
          <div class="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl border border-slate-200">
            <div class="flex items-center justify-between border-b border-slate-100 pb-3">
              <div class="flex items-center gap-2">
                <span class="text-xl">🛵</span>
                <h3 class="font-bold text-slate-900">Assign Driver & Dispatch</h3>
              </div>
              <button type="button" (click)="closeAssignDriver()" class="text-slate-400 hover:text-slate-600">✕</button>
            </div>

            <div class="mt-3 text-xs text-slate-600">
              Order <strong>{{ targetOrder.orderNumber }}</strong> · {{ targetOrder.customerName || 'Customer' }}
              <p class="mt-0.5 text-slate-500">📍 {{ targetOrder.deliveryAddress }}</p>
            </div>

            @if (driverModalError()) {
              <div class="mt-3 rounded-lg bg-red-50 p-2.5 text-xs text-red-700">{{ driverModalError() }}</div>
            }

            <div class="mt-4 space-y-3">
              <div>
                <label class="block text-xs font-semibold text-slate-700">Rider / Driver Name *</label>
                <input
                  type="text"
                  [(ngModel)]="driverNameInput"
                  placeholder="e.g. Juan Dela Cruz"
                  class="mt-1 w-full rounded-lg border border-slate-300 p-2 text-sm focus:border-slate-900 focus:outline-none"
                />
              </div>

              <div>
                <label class="block text-xs font-semibold text-slate-700">Rider Contact Number *</label>
                <input
                  type="tel"
                  [(ngModel)]="driverPhoneInput"
                  placeholder="e.g. 0917 123 4567"
                  class="mt-1 w-full rounded-lg border border-slate-300 p-2 text-sm focus:border-slate-900 focus:outline-none"
                />
              </div>
            </div>

            <div class="mt-6 flex items-center justify-end gap-2 border-t border-slate-100 pt-3">
              <button
                type="button"
                (click)="closeAssignDriver()"
                class="rounded-lg border border-slate-300 px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                type="button"
                (click)="submitAssignDriver()"
                [disabled]="submittingDriver()"
                class="rounded-lg bg-purple-600 px-4 py-1.5 text-xs font-semibold text-white hover:bg-purple-700 disabled:opacity-50"
              >
                {{ submittingDriver() ? 'Dispatching...' : 'Dispatch Order 🛵' }}
              </button>
            </div>
          </div>
        </div>
      }

      <!-- Complete Delivery Modal -->
      @if (completingDeliveryOrder(); as targetOrder) {
        <div class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4 backdrop-blur-xs">
          <div class="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl border border-slate-200">
            <div class="flex items-center justify-between border-b border-slate-100 pb-3">
              <div class="flex items-center gap-2">
                <span class="text-xl">✅</span>
                <h3 class="font-bold text-slate-900">Complete Delivery</h3>
              </div>
              <button type="button" (click)="closeCompleteDelivery()" class="text-slate-400 hover:text-slate-600">✕</button>
            </div>

            <div class="mt-3 text-xs text-slate-600">
              Order <strong>{{ targetOrder.orderNumber }}</strong> · Total: <strong>{{ targetOrder.totalAmount | currency }}</strong>
            </div>

            @if (completeModalError()) {
              <div class="mt-3 rounded-lg bg-red-50 p-2.5 text-xs text-red-700">{{ completeModalError() }}</div>
            }

            <div class="mt-4 space-y-4">
              <!-- 4-digit PIN input -->
              <div class="rounded-xl border border-amber-200 bg-amber-50/70 p-3">
                <label class="block text-xs font-bold text-amber-900">🔐 Customer 4-Digit Delivery PIN *</label>
                <p class="text-[11px] text-amber-800 mt-0.5">
                  Ask the customer for the 4-digit PIN displayed on their live tracking screen.
                </p>
                <input
                  type="text"
                  maxlength="4"
                  [(ngModel)]="deliveryPinInput"
                  placeholder="e.g. 1234"
                  class="mt-2 w-full rounded-lg border border-amber-300 bg-white p-2.5 text-center text-xl font-mono font-bold tracking-widest text-slate-900 focus:border-slate-900 focus:outline-none"
                />
              </div>

              <!-- Payment collection if Pay on Delivery -->
              @if (targetOrder.paymentMethod === PaymentMethod.PayOnDelivery) {
                <div class="rounded-xl border border-slate-200 bg-slate-50 p-3 space-y-2">
                  <label class="block text-xs font-bold text-slate-800">
                    Payment Collection: {{ targetOrder.totalAmount | currency }}
                  </label>
                  <p class="text-[11px] text-slate-500">How did the customer pay the rider?</p>
                  <div class="grid grid-cols-2 gap-2">
                    <button
                      type="button"
                      (click)="paymentCollectionInput = PaymentCollectionMethod.Cash"
                      class="rounded-lg border p-2 text-center text-xs font-semibold transition"
                      [class]="paymentCollectionInput === PaymentCollectionMethod.Cash
                        ? 'border-slate-900 bg-slate-900 text-white'
                        : 'border-slate-300 bg-white text-slate-700 hover:bg-slate-50'"
                    >
                      💵 Cash
                    </button>
                    <button
                      type="button"
                      (click)="paymentCollectionInput = PaymentCollectionMethod.GCash"
                      class="rounded-lg border p-2 text-center text-xs font-semibold transition"
                      [class]="paymentCollectionInput === PaymentCollectionMethod.GCash
                        ? 'border-blue-600 bg-blue-600 text-white'
                        : 'border-slate-300 bg-white text-slate-700 hover:bg-slate-50'"
                    >
                      📱 GCash QR
                    </button>
                  </div>
                </div>
              }
            </div>

            <div class="mt-6 flex items-center justify-end gap-2 border-t border-slate-100 pt-3">
              <button
                type="button"
                (click)="closeCompleteDelivery()"
                class="rounded-lg border border-slate-300 px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                type="button"
                (click)="submitCompleteDelivery()"
                [disabled]="submittingComplete()"
                class="rounded-lg bg-emerald-600 px-4 py-1.5 text-xs font-semibold text-white hover:bg-emerald-700 disabled:opacity-50"
              >
                {{ submittingComplete() ? 'Verifying PIN...' : 'Confirm Delivery ✓' }}
              </button>
            </div>
          </div>
        </div>
      }
    </div>
  `
})
export class StoreManageComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(FoodOrderingApiService);
  private readonly tenant = inject(TenantService);
  private readonly auth = inject(AuthService);
  readonly orderLive = inject(OrderLiveService);

  private readonly liveSubscriptions = new Subscription();
  private audioContext: AudioContext | null = null;

  readonly OrderStatus = OrderStatus;
  readonly OrderType = OrderType;
  readonly CustomerType = CustomerType;
  readonly StoreRole = StoreRole;
  readonly PromoDiscountType = PromoDiscountType;
  readonly imageUrl = resolveImageUrl;

  readonly PaymentMethod = PaymentMethod;
  readonly PaymentStatus = PaymentStatus;
  readonly PaymentCollectionMethod = PaymentCollectionMethod;

  readonly orderStatusOptions: Array<{ label: string; value: OrderStatus | null }> = [
    { label: 'All', value: null },
    { label: 'Pending', value: OrderStatus.Pending },
    { label: 'Confirmed', value: OrderStatus.Confirmed },
    { label: 'Preparing', value: OrderStatus.Preparing },
    { label: 'Ready', value: OrderStatus.Ready },
    { label: 'Out for Delivery', value: OrderStatus.OutForDelivery },
    { label: 'Completed', value: OrderStatus.Completed },
    { label: 'Cancelled', value: OrderStatus.Cancelled }
  ];

  readonly assigningDriverOrder = signal<Order | null>(null);
  driverNameInput = '';
  driverPhoneInput = '';
  readonly submittingDriver = signal(false);
  readonly driverModalError = signal<string | null>(null);

  readonly completingDeliveryOrder = signal<Order | null>(null);
  deliveryPinInput = '';
  paymentCollectionInput: PaymentCollectionMethod = PaymentCollectionMethod.Cash;
  readonly submittingComplete = signal(false);
  readonly completeModalError = signal<string | null>(null);

  readonly storeSlug = signal('');
  readonly store = signal<Store | null>(null);
  readonly tab = signal<'orders' | 'menu' | 'promos' | 'team' | 'settings'>('orders');
  readonly orders = signal<Order[]>([]);
  readonly orderStatusFilter = signal<OrderStatus | null>(null);
  readonly soundEnabled = signal(this.loadSoundPreference());
  readonly categories = signal<Category[]>([]);
  readonly menuItems = signal<MenuItem[]>([]);
  readonly promos = signal<PromoCode[]>([]);
  readonly members = signal<StoreMember[]>([]);
  readonly liveAlert = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly uploadingImage = signal(false);
  readonly savingProfile = signal(false);
  readonly editingCategoryPublicId = signal<string | null>(null);
  readonly editingItemPublicId = signal<string | null>(null);

  readonly isManager = computed(() =>
    this.auth.hasStoreRole(this.storeSlug(), StoreRole.Manager)
  );

  readonly filteredOrders = computed(() => {
    const filter = this.orderStatusFilter();
    const all = this.orders();
    return filter === null ? all : all.filter((o) => o.status === filter);
  });

  readonly orderCountsByStatus = computed(() => {
    const counts = new Map<OrderStatus, number>();
    for (const order of this.orders()) {
      counts.set(order.status, (counts.get(order.status) ?? 0) + 1);
    }
    return counts;
  });

  categoryName = '';
  categorySortOrder = 1;

  selectedCategoryId = '';
  itemName = '';
  itemDescription = '';
  itemPrice = 0;
  itemImageUrl = '';
  itemIsAvailable = true;
  itemSortOrder = 0;
  itemOptionGroups: SaveMenuItemOptionGroupRequest[] = [];

  memberEmail = '';
  memberFullName = '';
  memberPassword = '';
  memberRole = StoreRole.Staff;

  storeName = '';
  storeEmail = '';
  storePhone = '';
  storeAddress = '';
  storeOpeningHours = '';
  storeDeliveryFee = 0;
  storeMinimumDeliveryOrder = 0;
  storeFreeDeliveryThreshold: number | null = null;
  storeEstimatedDeliveryTime = '';

  promoCode = '';
  promoDiscountType: PromoDiscountType = PromoDiscountType.Percentage;
  promoDiscountValue = 10;
  promoMaxUses: number | null = null;
  promoStartsAt = '';
  promoExpiresAt = '';

  ngOnInit(): void {
    this.liveSubscriptions.add(
      this.orderLive.orderPlaced$.subscribe((order) => {
        if (order.storeSlug === this.storeSlug()) {
          this.orders.update((list) => upsertOrderList(list, order));
          this.liveAlert.set(`New ${this.orderTypeText(order.orderType).toLowerCase()} order ${order.orderNumber}!`);
          if (this.soundEnabled()) {
            this.playNewOrderChime();
          }
        }
      })
    );

    this.liveSubscriptions.add(
      this.orderLive.orderUpdated$.subscribe((order) => {
        if (order.storeSlug === this.storeSlug()) {
          this.orders.update((list) => upsertOrderList(list, order));
        }
      })
    );

    this.liveSubscriptions.add(
      this.orderLive.reconnected$.subscribe(() => this.refreshOrders())
    );

    this.liveSubscriptions.add(
      this.route.paramMap.subscribe((params) => {
        const previousSlug = this.storeSlug();
        const slug = params.get('storeSlug') ?? '';
        if (previousSlug && previousSlug !== slug) {
          void this.orderLive.leaveStoreKitchen(previousSlug);
        }

        this.storeSlug.set(slug);
        this.tenant.setStoreSlug(slug);
        this.liveAlert.set(null);
        this.cancelCategoryEdit();
        this.cancelEdit();
        this.refreshAll();
        if (slug) {
          void this.orderLive.joinStoreKitchen(slug);
        }
      })
    );
  }

  ngOnDestroy(): void {
    void this.orderLive.leaveStoreKitchen(this.storeSlug());
    this.liveSubscriptions.unsubscribe();
    if (this.audioContext) {
      void this.audioContext.close();
      this.audioContext = null;
    }
    this.tenant.setStoreSlug(null);
  }

  orderCount(status: OrderStatus | null): number {
    return status === null ? this.orders().length : (this.orderCountsByStatus().get(status) ?? 0);
  }

  toggleSound(): void {
    const next = !this.soundEnabled();
    this.soundEnabled.set(next);
    try {
      localStorage.setItem('fo_kitchen_sound', next ? '1' : '0');
    } catch {
      // Ignore storage failures
    }

    if (next) {
      this.playNewOrderChime();
    }
  }

  statusText(status: OrderStatus): string {
    return orderStatusLabel(status);
  }

  orderTypeText(type: OrderType): string {
    return orderTypeLabel(type);
  }

  roleText(role: StoreRole): string {
    return storeRoleLabel(role);
  }

  switchToPromos(): void {
    this.tab.set('promos');
    this.loadPromos();
  }

  loadPromos(): void {
    this.api.getStorePromos().subscribe({
      next: (list) => this.promos.set(list),
      error: (err) => this.setError(err, 'Could not load promo codes.')
    });
  }

  isPromoUpcoming(promo: PromoCode): boolean {
    return !!promo.startsAtUtc && new Date(promo.startsAtUtc).getTime() > Date.now();
  }

  createPromo(): void {
    if (!this.promoCode.trim()) {
      this.error.set('Promo code name is required.');
      return;
    }
    if (this.promoDiscountValue <= 0) {
      this.error.set('Discount value must be greater than zero.');
      return;
    }
    if (this.promoStartsAt && this.promoExpiresAt && new Date(this.promoStartsAt) > new Date(this.promoExpiresAt)) {
      this.error.set('Starts At date cannot be later than Expires At date.');
      return;
    }
    this.clearAlerts();
    this.api.createPromo({
      code: this.promoCode.trim(),
      discountType: this.promoDiscountType,
      discountValue: this.promoDiscountValue,
      maxUses: this.promoMaxUses && this.promoMaxUses > 0 ? this.promoMaxUses : null,
      startsAtUtc: this.promoStartsAt ? new Date(this.promoStartsAt).toISOString() : null,
      expiresAtUtc: this.promoExpiresAt ? new Date(this.promoExpiresAt).toISOString() : null
    }).subscribe({
      next: () => {
        this.message.set('Promo code created successfully.');
        this.promoCode = '';
        this.promoDiscountValue = 10;
        this.promoMaxUses = null;
        this.promoStartsAt = '';
        this.promoExpiresAt = '';
        this.loadPromos();
      },
      error: (err) => this.setError(err, 'Could not create promo code.')
    });
  }

  togglePromo(publicId: string): void {
    this.clearAlerts();
    this.api.togglePromo(publicId).subscribe({
      next: () => {
        this.loadPromos();
      },
      error: (err) => this.setError(err, 'Could not toggle promo code status.')
    });
  }

  deletePromo(publicId: string): void {
    if (!confirm('Are you sure you want to delete this promo code?')) {
      return;
    }
    this.clearAlerts();
    this.api.deletePromo(publicId).subscribe({
      next: () => {
        this.message.set('Promo code deleted.');
        this.loadPromos();
      },
      error: (err) => this.setError(err, 'Could not delete promo code.')
    });
  }

  switchToTeam(): void {
    this.tab.set('team');
    this.api.getStoreMembers().subscribe({
      next: (list) => this.members.set(list),
      error: (err) => this.setError(err, 'Could not load store members.')
    });
  }

  advanceOrder(publicId: string, status: OrderStatus): void {
    this.clearAlerts();
    this.api.updateOrderStatus(publicId, status).subscribe({
      next: () => this.refreshOrders(),
      error: (err) => this.setError(err, 'Could not update order status.')
    });
  }

  verifyGcashPayment(publicId: string): void {
    this.clearAlerts();
    this.api.verifyPayment(publicId).subscribe({
      next: (updated) => {
        this.message.set(`GCash payment verified for Order #${updated.orderNumber}.`);
        this.refreshOrders();
      },
      error: (err) => this.setError(err, 'Could not verify GCash payment.')
    });
  }

  /**
   * Opens the driver assignment modal for delivery orders in Preparing or Ready status.
   */
  openAssignDriver(order: Order): void {
    this.assigningDriverOrder.set(order);
    this.driverNameInput = order.driverName ?? '';
    this.driverPhoneInput = order.driverPhone ?? '';
    this.driverModalError.set(null);
  }

  closeAssignDriver(): void {
    this.assigningDriverOrder.set(null);
    this.driverNameInput = '';
    this.driverPhoneInput = '';
    this.driverModalError.set(null);
  }

  /**
   * Dispatches the order with the assigned rider name and phone, transitioning status to OutForDelivery.
   */
  submitAssignDriver(): void {
    const order = this.assigningDriverOrder();
    if (!order) return;

    const name = this.driverNameInput.trim();
    const phone = this.driverPhoneInput.trim();

    if (!name) {
      this.driverModalError.set('Please enter the delivery rider name.');
      return;
    }
    if (!phone) {
      this.driverModalError.set('Please enter rider phone number.');
      return;
    }

    this.submittingDriver.set(true);
    this.driverModalError.set(null);

    this.api.assignDriver(order.publicId, name, phone).subscribe({
      next: () => {
        this.submittingDriver.set(false);
        this.closeAssignDriver();
        this.message.set(`Rider assigned to order #${order.orderNumber}. Order is now out for delivery!`);
        this.refreshOrders();
      },
      error: (err) => {
        this.submittingDriver.set(false);
        this.driverModalError.set(err?.error?.error ?? err?.error?.message ?? 'Could not assign rider.');
      }
    });
  }

  /**
   * Opens the completion modal for OutForDelivery orders, prompting for the customer's 4-digit PIN.
   */
  openCompleteDelivery(order: Order): void {
    this.completingDeliveryOrder.set(order);
    this.deliveryPinInput = '';
    this.paymentCollectionInput = PaymentCollectionMethod.Cash;
    this.completeModalError.set(null);
  }

  closeCompleteDelivery(): void {
    this.completingDeliveryOrder.set(null);
    this.deliveryPinInput = '';
    this.completeModalError.set(null);
  }

  /**
   * Verifies the customer's 4-digit PIN and records payment collection (Cash or GCash QR) to complete delivery.
   */
  submitCompleteDelivery(): void {
    const order = this.completingDeliveryOrder();
    if (!order) return;

    const pin = this.deliveryPinInput.trim();
    if (order.orderType === OrderType.Delivery) {
      if (!pin || pin.length !== 4) {
        this.completeModalError.set('Please enter the 4-digit Delivery PIN from the customer.');
        return;
      }
    }

    this.submittingComplete.set(true);
    this.completeModalError.set(null);

    const paymentCollection = order.paymentMethod === PaymentMethod.PayOnDelivery ? this.paymentCollectionInput : undefined;

    this.api.updateOrderStatus(order.publicId, OrderStatus.Completed, paymentCollection, pin || undefined).subscribe({
      next: () => {
        this.submittingComplete.set(false);
        this.closeCompleteDelivery();
        this.message.set(`Order #${order.orderNumber} successfully completed.`);
        this.refreshOrders();
      },
      error: (err) => {
        this.submittingComplete.set(false);
        this.completeModalError.set(err?.error?.error ?? err?.error?.message ?? 'Could not complete delivery. Please verify the customer PIN.');
      }
    });
  }

  startEditCategory(category: Category): void {
    this.clearAlerts();
    this.editingCategoryPublicId.set(category.publicId);
    this.categoryName = category.name;
    this.categorySortOrder = category.sortOrder;
  }

  cancelCategoryEdit(): void {
    this.editingCategoryPublicId.set(null);
    this.categoryName = '';
    this.categorySortOrder = 1;
  }

  saveCategory(): void {
    this.clearAlerts();
    const editingId = this.editingCategoryPublicId();
    const action$ = editingId
      ? this.api.updateCategory(editingId, this.categoryName, this.categorySortOrder)
      : this.api.createCategory(this.categoryName, this.categorySortOrder);

    action$.subscribe({
      next: () => {
        this.cancelCategoryEdit();
        this.message.set(editingId ? 'Category updated.' : 'Category created.');
        this.refreshMenu();
      },
      error: (err) =>
        this.setError(err, editingId ? 'Could not update category.' : 'Could not create category.')
    });
  }

  moveCategory(index: number, offset: -1 | 1): void {
    const current = [...this.categories()];
    const targetIndex = index + offset;
    if (targetIndex < 0 || targetIndex >= current.length) {
      return;
    }

    [current[index], current[targetIndex]] = [current[targetIndex], current[index]];
    this.clearAlerts();
    this.api.reorderCategories(current.map((c) => c.publicId)).subscribe({
      next: (updated) => {
        this.categories.set(updated);
        this.refreshMenu();
      },
      error: (err) => this.setError(err, 'Could not reorder categories.')
    });
  }

  deleteCategory(publicId: string): void {
    this.clearAlerts();
    this.api.deleteCategory(publicId).subscribe({
      next: () => {
        if (this.editingCategoryPublicId() === publicId) {
          this.cancelCategoryEdit();
        }
        this.message.set('Category deleted.');
        this.refreshMenu();
      },
      error: (err) => this.setError(err, 'Could not delete category.')
    });
  }

  toggleAcceptingOrders(): void {
    const activeStore = this.store();
    if (!activeStore) {
      return;
    }

    this.clearAlerts();
    const nextValue = !activeStore.isAcceptingOrders;
    this.api.updateCurrentStoreSettings(nextValue).subscribe({
      next: (updated) => {
        this.applyStoreProfile(updated);
        this.message.set(
          updated.isAcceptingOrders
            ? 'Store is now open and accepting orders.'
            : 'Store is now closed and not accepting orders.'
        );
      },
      error: (err) => this.setError(err, 'Could not update store settings.')
    });
  }

  saveStoreProfile(): void {
    this.clearAlerts();
    this.savingProfile.set(true);
    this.api
      .updateCurrentStoreProfile({
        name: this.storeName,
        email: this.storeEmail,
        phone: this.storePhone,
        address: this.storeAddress,
        openingHours: this.storeOpeningHours,
        deliveryFee: this.storeDeliveryFee,
        minimumDeliveryOrder: this.storeMinimumDeliveryOrder,
        freeDeliveryThreshold: this.storeFreeDeliveryThreshold,
        estimatedDeliveryTime: this.storeEstimatedDeliveryTime
      })
      .subscribe({
        next: (updated) => {
          this.applyStoreProfile(updated);
          this.savingProfile.set(false);
          this.message.set('Store profile updated.');
        },
        error: (err) => {
          this.savingProfile.set(false);
          this.setError(err, 'Could not update store profile.');
        }
      });
  }

  onMenuImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement | null;
    const file = input?.files?.[0];
    if (!file) {
      return;
    }

    this.clearAlerts();
    this.uploadingImage.set(true);
    this.api.uploadMenuItemImage(file).subscribe({
      next: (res) => {
        this.itemImageUrl = res.imageUrl;
        this.uploadingImage.set(false);
        this.message.set('Image uploaded. Click Save/Add Menu Item to apply.');
        if (input) {
          input.value = '';
        }
      },
      error: (err) => {
        this.uploadingImage.set(false);
        this.setError(err, 'Could not upload image.');
        if (input) {
          input.value = '';
        }
      }
    });
  }

  addOptionGroup(): void {
    this.itemOptionGroups.push({
      name: '',
      isRequired: false,
      allowMultiple: false,
      sortOrder: this.itemOptionGroups.length + 1,
      options: [{ name: '', priceDelta: 0, sortOrder: 1 }]
    });
  }

  removeOptionGroup(groupIndex: number): void {
    this.itemOptionGroups.splice(groupIndex, 1);
  }

  addOption(groupIndex: number): void {
    const group = this.itemOptionGroups[groupIndex];
    if (!group) {
      return;
    }

    group.options.push({ name: '', priceDelta: 0, sortOrder: group.options.length + 1 });
  }

  removeOption(groupIndex: number, optionIndex: number): void {
    const group = this.itemOptionGroups[groupIndex];
    if (!group) {
      return;
    }

    group.options.splice(optionIndex, 1);
  }

  startEdit(item: MenuItem): void {
    this.clearAlerts();
    this.editingItemPublicId.set(item.publicId);
    this.selectedCategoryId = item.categoryPublicId;
    this.itemName = item.name;
    this.itemDescription = item.description;
    this.itemPrice = item.price;
    this.itemImageUrl = item.imageUrl ?? '';
    this.itemIsAvailable = item.isAvailable;
    this.itemSortOrder = item.sortOrder;
    this.itemOptionGroups = item.optionGroups.map((g) => ({
      name: g.name,
      isRequired: g.isRequired,
      allowMultiple: g.allowMultiple,
      sortOrder: g.sortOrder,
      options: g.options.map((o) => ({
        name: o.name,
        priceDelta: o.priceDelta,
        sortOrder: o.sortOrder
      }))
    }));
  }

  cancelEdit(): void {
    this.editingItemPublicId.set(null);
    this.selectedCategoryId = '';
    this.itemName = '';
    this.itemDescription = '';
    this.itemPrice = 0;
    this.itemImageUrl = '';
    this.itemIsAvailable = true;
    this.itemSortOrder = 0;
    this.itemOptionGroups = [];
  }

  saveMenuItem(): void {
    this.clearAlerts();
    const editingId = this.editingItemPublicId();
    const request: SaveMenuItemRequest = {
      categoryPublicId: this.selectedCategoryId,
      name: this.itemName,
      description: this.itemDescription,
      price: Number(this.itemPrice),
      imageUrl: this.itemImageUrl || null,
      isAvailable: editingId ? this.itemIsAvailable : true,
      sortOrder: editingId ? this.itemSortOrder : 0,
      optionGroups: this.itemOptionGroups.map((g, gIdx) => ({
        name: g.name,
        isRequired: g.isRequired,
        allowMultiple: g.allowMultiple,
        sortOrder: gIdx + 1,
        options: g.options.map((o, oIdx) => ({
          name: o.name,
          priceDelta: Number(o.priceDelta) || 0,
          sortOrder: oIdx + 1
        }))
      }))
    };

    const action$ = editingId
      ? this.api.updateMenuItem(editingId, request)
      : this.api.createMenuItem(request);

    action$.subscribe({
      next: () => {
        this.cancelEdit();
        this.message.set(editingId ? 'Menu item updated.' : 'Menu item created.');
        this.refreshMenu();
      },
      error: (err) =>
        this.setError(
          err,
          editingId ? 'Could not update menu item.' : 'Could not create menu item.'
        )
    });
  }

  toggleAvailability(item: MenuItem): void {
    this.clearAlerts();
    this.api
      .updateMenuItem(item.publicId, {
        categoryPublicId: item.categoryPublicId,
        name: item.name,
        description: item.description,
        price: item.price,
        imageUrl: item.imageUrl,
        isAvailable: !item.isAvailable,
        sortOrder: item.sortOrder
      })
      .subscribe({
        next: () => this.refreshMenu(),
        error: (err) => this.error.set(err?.error?.error ?? 'Could not update menu item.')
      });
  }

  deleteItem(publicId: string): void {
    this.clearAlerts();
    this.api.deleteMenuItem(publicId).subscribe({
      next: () => {
        if (this.editingItemPublicId() === publicId) {
          this.cancelEdit();
        }
        this.refreshMenu();
      },
      error: (err) => this.error.set(err?.error?.error ?? 'Could not delete menu item.')
    });
  }

  addMember(): void {
    this.clearAlerts();
    this.api
      .addStoreMember({
        email: this.memberEmail,
        role: this.memberRole,
        fullName: this.memberFullName || null,
        password: this.memberPassword || null
      })
      .subscribe({
        next: () => {
          this.memberEmail = '';
          this.memberFullName = '';
          this.memberPassword = '';
          this.message.set('Store member added.');
          this.switchToTeam();
        },
        error: (err) => {
          const list = err?.error?.errors;
          this.error.set(Array.isArray(list) ? list.join(' ') : 'Could not add store member.');
        }
      });
  }

  removeMember(userId: string): void {
    this.clearAlerts();
    this.api.removeStoreMember(userId).subscribe({
      next: () => this.switchToTeam(),
      error: (err) => this.error.set(err?.error?.error ?? 'Could not remove member.')
    });
  }

  private refreshAll(): void {
    const slug = this.storeSlug();
    if (slug) {
      this.api.getStoreBySlug(slug).subscribe({
        next: (store) => this.applyStoreProfile(store)
      });
    }
    this.refreshOrders();
    this.refreshMenu();
    this.loadPromos();
  }

  private applyStoreProfile(store: Store): void {
    this.store.set(store);
    this.storeName = store.name;
    this.storeEmail = store.email;
    this.storePhone = store.phone;
    this.storeAddress = store.address;
    this.storeOpeningHours = store.openingHours;
    this.storeDeliveryFee = store.deliveryFee ?? 0;
    this.storeMinimumDeliveryOrder = store.minimumDeliveryOrder ?? 0;
    this.storeFreeDeliveryThreshold = store.freeDeliveryThreshold ?? null;
    this.storeEstimatedDeliveryTime = store.estimatedDeliveryTime ?? '';
  }

  private refreshOrders(): void {
    this.api.getStoreOrders().subscribe({
      next: (list) => this.orders.set(list)
    });
  }

  private refreshMenu(): void {
    this.api.getCategories().subscribe((cats) => this.categories.set(cats));
    this.api.getMenuItems().subscribe((items) => this.menuItems.set(items));
  }

  private clearAlerts(): void {
    this.message.set(null);
    this.error.set(null);
  }

  private setError(err: { error?: { error?: string }; status?: number }, fallback: string): void {
    if (err?.error?.error) {
      this.error.set(err.error.error);
      return;
    }

    const suffix = err?.status ? ` (HTTP ${err.status})` : '';
    this.error.set(`${fallback}${suffix}`);
  }

  private loadSoundPreference(): boolean {
    try {
      return localStorage.getItem('fo_kitchen_sound') !== '0';
    } catch {
      return true;
    }
  }

  private playNewOrderChime(): void {
    try {
      const AudioCtx = window.AudioContext
        ?? (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
      if (!AudioCtx) {
        return;
      }

      if (!this.audioContext || this.audioContext.state === 'closed') {
        this.audioContext = new AudioCtx();
      }

      if (this.audioContext.state === 'suspended') {
        void this.audioContext.resume();
      }

      const now = this.audioContext.currentTime;
      this.scheduleTone(this.audioContext, 880, now, 0.14);
      this.scheduleTone(this.audioContext, 1174.66, now + 0.14, 0.22);
    } catch {
      // Ignore audio errors if blocked by browser autoplay policy
    }
  }

  private scheduleTone(ctx: AudioContext, frequency: number, startTime: number, duration: number): void {
    const oscillator = ctx.createOscillator();
    const gain = ctx.createGain();
    oscillator.type = 'sine';
    oscillator.frequency.setValueAtTime(frequency, startTime);

    gain.gain.setValueAtTime(0.001, startTime);
    gain.gain.exponentialRampToValueAtTime(0.18, startTime + 0.02);
    gain.gain.exponentialRampToValueAtTime(0.001, startTime + duration);

    oscillator.connect(gain);
    gain.connect(ctx.destination);
    oscillator.start(startTime);
    oscillator.stop(startTime + duration);
  }
}
