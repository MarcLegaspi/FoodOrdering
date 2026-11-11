import { CurrencyPipe } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { CartService } from '../../core/cart/cart.service';
import { Category, MenuItem, Store } from '../../core/models/api.models';
import { FoodOrderingApiService } from '../../core/services/food-ordering-api.service';
import { TenantService } from '../../core/tenant/tenant.service';

@Component({
  selector: 'app-store-menu',
  imports: [CurrencyPipe, FormsModule, RouterLink],
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
            <h1 class="text-2xl font-bold text-slate-900">{{ activeStore.name }}</h1>
            <p class="mt-1 text-sm text-slate-500">{{ activeStore.email }} · {{ activeStore.phone }}</p>
          </div>

          @if (groupedMenu().length === 0) {
            <div class="rounded-xl border border-dashed border-slate-300 bg-white p-8 text-center text-sm text-slate-500">
              No menu items available yet.
            </div>
          }

          @for (group of groupedMenu(); track group.categoryName) {
            <section class="space-y-3">
              <h2 class="text-lg font-semibold text-slate-900">{{ group.categoryName }}</h2>

              <div class="grid gap-3 sm:grid-cols-2">
                @for (item of group.items; track item.publicId) {
                  <div class="flex flex-col justify-between overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
                    @if (item.imageUrl) {
                      <img
                        [src]="item.imageUrl"
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
                        (click)="cart.addItem(item)"
                        class="mt-4 w-full rounded-lg border border-slate-300 py-1.5 text-sm font-medium text-slate-800 hover:bg-slate-50"
                      >
                        Add to cart
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
              @for (item of cart.items(); track item.menuItemPublicId) {
                <div class="flex items-center justify-between py-3 text-sm">
                  <div>
                    <p class="font-medium text-slate-900">{{ item.name }}</p>
                    <p class="text-xs text-slate-500">{{ item.unitPrice | currency }} each</p>
                  </div>

                  <div class="flex items-center gap-2">
                    <button
                      type="button"
                      (click)="cart.updateQuantity(item.menuItemPublicId, item.quantity - 1)"
                      class="h-7 w-7 rounded border border-slate-300 text-slate-700 hover:bg-slate-50"
                    >
                      -
                    </button>
                    <span class="w-5 text-center font-medium">{{ item.quantity }}</span>
                    <button
                      type="button"
                      (click)="cart.updateQuantity(item.menuItemPublicId, item.quantity + 1)"
                      class="h-7 w-7 rounded border border-slate-300 text-slate-700 hover:bg-slate-50"
                    >
                      +
                    </button>
                  </div>
                </div>
              }
            </div>

            <div class="mt-4 space-y-3 border-t border-slate-200 pt-4">
              <textarea
                rows="2"
                [(ngModel)]="notes"
                placeholder="Order notes (optional)"
                class="w-full rounded-lg border border-slate-300 p-2 text-sm focus:border-slate-900 focus:outline-none"
              ></textarea>

              <div class="flex items-center justify-between font-bold text-slate-900">
                <span>Total</span>
                <span>{{ cart.totalAmount() | currency }}</span>
              </div>

              @if (auth.isAuthenticated()) {
                <button
                  type="button"
                  [disabled]="submitting()"
                  (click)="checkout()"
                  class="w-full rounded-lg bg-slate-900 py-2.5 text-sm font-semibold text-white hover:bg-slate-800 disabled:opacity-50"
                >
                  {{ submitting() ? 'Placing order...' : 'Place Order' }}
                </button>
              } @else {
                <a
                  routerLink="/login"
                  [queryParams]="{ returnUrl: '/' + activeStore.slug }"
                  class="block w-full rounded-lg bg-slate-900 py-2.5 text-center text-sm font-semibold text-white hover:bg-slate-800"
                >
                  Sign in to Place Order
                </a>
              }
            </div>
          }
        </aside>
      </div>
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
  notes = '';

  readonly groupedMenu = computed(() => {
    const items = this.menuItems();
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

  checkout(): void {
    this.orderError.set(null);
    this.submitting.set(true);

    this.api
      .placeOrder({
        items: this.cart.items().map((i) => ({
          menuItemPublicId: i.menuItemPublicId,
          quantity: i.quantity
        })),
        notes: this.notes
      })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.cart.clear();
          this.notes = '';
          void this.router.navigateByUrl('/orders');
        },
        error: (err) => {
          this.submitting.set(false);
          this.orderError.set(err?.error?.error ?? 'Could not place order.');
        }
      });
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
