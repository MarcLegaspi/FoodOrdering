import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import {
  Category,
  MenuItem,
  Order,
  OrderStatus,
  StoreMember,
  StoreRole,
  orderStatusLabel,
  storeRoleLabel
} from '../../core/models/api.models';
import { FoodOrderingApiService } from '../../core/services/food-ordering-api.service';
import { TenantService } from '../../core/tenant/tenant.service';

@Component({
  selector: 'app-store-manage',
  imports: [CurrencyPipe, DatePipe, FormsModule],
  template: `
    <div class="space-y-6">
      <div class="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-slate-900">Manage: {{ storeSlug() }}</h1>
          <p class="mt-1 text-sm text-slate-500">Store dashboard for orders, menu, and team members.</p>
        </div>

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
          }
        </div>
      </div>

      @if (message()) {
        <div class="rounded-lg bg-emerald-50 p-3 text-sm text-emerald-700">{{ message() }}</div>
      }
      @if (error()) {
        <div class="rounded-lg bg-red-50 p-3 text-sm text-red-700">{{ error() }}</div>
      }

      <!-- ORDERS TAB -->
      @if (tab() === 'orders') {
        <div class="space-y-4">
          @if (orders().length === 0) {
            <p class="rounded-xl border border-dashed border-slate-300 bg-white p-8 text-center text-sm text-slate-500">
              No orders for this store yet.
            </p>
          }

          @for (order of orders(); track order.publicId) {
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
              <div class="flex flex-wrap items-center justify-between gap-2">
                <div>
                  <span class="font-bold text-slate-900">{{ order.orderNumber }}</span>
                  <span class="ml-2 text-xs text-slate-400">{{ order.createdAtUtc | date: 'short' }}</span>
                </div>
                <div class="flex items-center gap-3">
                  <span class="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-700">
                    {{ statusText(order.status) }}
                  </span>
                  <span class="font-bold text-slate-900">{{ order.totalAmount | currency }}</span>
                </div>
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
            <h2 class="font-bold text-slate-900">Add Category</h2>
            <form (ngSubmit)="createCategory()" class="mt-3 flex gap-2">
              <input
                type="text"
                name="categoryName"
                [(ngModel)]="categoryName"
                placeholder="e.g. Burgers"
                required
                class="flex-1 rounded-lg border border-slate-300 px-3 py-2 text-sm"
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
                Add
              </button>
            </form>

            <ul class="mt-4 divide-y divide-slate-100 text-sm">
              @for (cat of categories(); track cat.publicId) {
                <li class="flex justify-between py-2">
                  <span class="font-medium text-slate-800">{{ cat.name }}</span>
                  <span class="text-slate-400">Sort: {{ cat.sortOrder }}</span>
                </li>
              }
            </ul>
          </div>

          <div class="rounded-xl border border-slate-200 bg-white p-5">
            <h2 class="font-bold text-slate-900">Add Menu Item</h2>
            <form (ngSubmit)="createMenuItem()" class="mt-3 space-y-3">
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

              <button
                type="submit"
                class="w-full rounded-lg bg-slate-900 py-2 text-sm font-semibold text-white"
              >
                Add Menu Item
              </button>
            </form>
          </div>
        </div>

        <div class="rounded-xl border border-slate-200 bg-white p-5">
          <h2 class="font-bold text-slate-900">Menu Items</h2>
          <div class="mt-3 divide-y divide-slate-100">
            @for (item of menuItems(); track item.publicId) {
              <div class="flex flex-wrap items-center justify-between gap-2 py-3 text-sm">
                <div>
                  <span class="font-semibold text-slate-900">{{ item.name }}</span>
                  <span class="ml-2 text-xs text-slate-400">{{ item.categoryName }}</span>
                </div>
                <div class="flex items-center gap-3">
                  <span class="font-medium text-slate-900">{{ item.price | currency }}</span>
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
    </div>
  `
})
export class StoreManageComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(FoodOrderingApiService);
  private readonly tenant = inject(TenantService);
  private readonly auth = inject(AuthService);

  readonly OrderStatus = OrderStatus;
  readonly StoreRole = StoreRole;

  readonly storeSlug = signal('');
  readonly tab = signal<'orders' | 'menu' | 'team'>('orders');
  readonly orders = signal<Order[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly menuItems = signal<MenuItem[]>([]);
  readonly members = signal<StoreMember[]>([]);
  readonly message = signal<string | null>(null);
  readonly error = signal<string | null>(null);

  readonly isManager = computed(() =>
    this.auth.hasStoreRole(this.storeSlug(), StoreRole.Manager)
  );

  categoryName = '';
  categorySortOrder = 1;

  selectedCategoryId = '';
  itemName = '';
  itemDescription = '';
  itemPrice = 0;

  memberEmail = '';
  memberFullName = '';
  memberPassword = '';
  memberRole = StoreRole.Staff;

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const slug = params.get('storeSlug') ?? '';
      this.storeSlug.set(slug);
      this.tenant.setStoreSlug(slug);
      this.refreshAll();
    });
  }

  ngOnDestroy(): void {
    this.tenant.setStoreSlug(null);
  }

  statusText(status: OrderStatus): string {
    return orderStatusLabel(status);
  }

  roleText(role: StoreRole): string {
    return storeRoleLabel(role);
  }

  switchToTeam(): void {
    this.tab.set('team');
    this.api.getStoreMembers().subscribe({
      next: (list) => this.members.set(list),
      error: (err) => this.error.set(err?.error?.error ?? 'Could not load store members.')
    });
  }

  advanceOrder(publicId: string, status: OrderStatus): void {
    this.clearAlerts();
    this.api.updateOrderStatus(publicId, status).subscribe({
      next: () => this.refreshOrders(),
      error: (err) => this.error.set(err?.error?.error ?? 'Could not update order status.')
    });
  }

  createCategory(): void {
    this.clearAlerts();
    this.api.createCategory(this.categoryName, this.categorySortOrder).subscribe({
      next: () => {
        this.categoryName = '';
        this.message.set('Category created.');
        this.refreshMenu();
      },
      error: (err) => this.error.set(err?.error?.error ?? 'Could not create category.')
    });
  }

  createMenuItem(): void {
    this.clearAlerts();
    this.api
      .createMenuItem({
        categoryPublicId: this.selectedCategoryId,
        name: this.itemName,
        description: this.itemDescription,
        price: Number(this.itemPrice),
        isAvailable: true,
        sortOrder: 0
      })
      .subscribe({
        next: () => {
          this.itemName = '';
          this.itemDescription = '';
          this.itemPrice = 0;
          this.message.set('Menu item created.');
          this.refreshMenu();
        },
        error: (err) => this.error.set(err?.error?.error ?? 'Could not create menu item.')
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
      next: () => this.refreshMenu(),
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
    this.refreshOrders();
    this.refreshMenu();
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
}
