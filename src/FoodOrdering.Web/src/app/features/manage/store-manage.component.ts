import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import {
  Category,
  MenuItem,
  Order,
  OrderStatus,
  OrderType,
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
                @if (order.notes) {
                  <span>Notes: {{ order.notes }}</span>
                }
              </div>

              <ul class="mt-3 space-y-1 text-sm text-slate-600">
                @for (item of order.items; track item.menuItemPublicId) {
                  <li>{{ item.quantity }}× {{ item.itemName }} ({{ item.lineTotal | currency }})</li>
                }
              </ul>

              @if (order.status !== OrderStatus.Completed && order.status !== OrderStatus.Cancelled) {
                <div class="mt-4 flex flex-wrap gap-2">
                  @if (order.status === OrderStatus.Pending) {
                    <button
                      type="button"
                      (click)="advanceOrder(order.publicId, OrderStatus.Confirmed)"
                      class="rounded-md bg-slate-900 px-3 py-1 text-xs font-medium text-white"
                    >
                      Confirm
                    </button>
                  }
                  @if (order.status === OrderStatus.Confirmed) {
                    <button
                      type="button"
                      (click)="advanceOrder(order.publicId, OrderStatus.Preparing)"
                      class="rounded-md bg-slate-900 px-3 py-1 text-xs font-medium text-white"
                    >
                      Start Preparing
                    </button>
                  }
                  @if (order.status === OrderStatus.Preparing) {
                    <button
                      type="button"
                      (click)="advanceOrder(order.publicId, OrderStatus.Ready)"
                      class="rounded-md bg-slate-900 px-3 py-1 text-xs font-medium text-white"
                    >
                      Mark Ready
                    </button>
                  }
                  @if (order.status === OrderStatus.Ready) {
                    <button
                      type="button"
                      (click)="advanceOrder(order.publicId, OrderStatus.Completed)"
                      class="rounded-md bg-emerald-700 px-3 py-1 text-xs font-medium text-white"
                    >
                      Complete
                    </button>
                  }
                  <button
                    type="button"
                    (click)="advanceOrder(order.publicId, OrderStatus.Cancelled)"
                    class="rounded-md border border-red-200 px-3 py-1 text-xs font-medium text-red-700"
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
  readonly StoreRole = StoreRole;
  readonly imageUrl = resolveImageUrl;

  readonly orderStatusOptions: Array<{ label: string; value: OrderStatus | null }> = [
    { label: 'All', value: null },
    { label: 'Pending', value: OrderStatus.Pending },
    { label: 'Confirmed', value: OrderStatus.Confirmed },
    { label: 'Preparing', value: OrderStatus.Preparing },
    { label: 'Ready', value: OrderStatus.Ready },
    { label: 'Completed', value: OrderStatus.Completed },
    { label: 'Cancelled', value: OrderStatus.Cancelled }
  ];

  readonly storeSlug = signal('');
  readonly store = signal<Store | null>(null);
  readonly tab = signal<'orders' | 'menu' | 'team' | 'settings'>('orders');
  readonly orders = signal<Order[]>([]);
  readonly orderStatusFilter = signal<OrderStatus | null>(null);
  readonly soundEnabled = signal(this.loadSoundPreference());
  readonly categories = signal<Category[]>([]);
  readonly menuItems = signal<MenuItem[]>([]);
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

  memberEmail = '';
  memberFullName = '';
  memberPassword = '';
  memberRole = StoreRole.Staff;

  storeName = '';
  storeEmail = '';
  storePhone = '';
  storeAddress = '';
  storeOpeningHours = '';

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
        openingHours: this.storeOpeningHours
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
      sortOrder: editingId ? this.itemSortOrder : 0
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
        this.error.set(
          err?.error?.error ?? (editingId ? 'Could not update menu item.' : 'Could not create menu item.')
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
  }

  private applyStoreProfile(store: Store): void {
    this.store.set(store);
    this.storeName = store.name;
    this.storeEmail = store.email;
    this.storePhone = store.phone;
    this.storeAddress = store.address;
    this.storeOpeningHours = store.openingHours;
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
