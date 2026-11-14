import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Store } from '../../core/models/api.models';
import { FoodOrderingApiService } from '../../core/services/food-ordering-api.service';
import { TenantService } from '../../core/tenant/tenant.service';
import { StoreVisuals, getStoreVisuals } from '../../core/utils/store-theme';

@Component({
  selector: 'app-store-list',
  imports: [RouterLink],
  template: `
    <div class="space-y-6">
      <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2">
        <div>
          <h1 class="text-2xl font-bold tracking-tight text-slate-900">Browse Restaurants</h1>
          <p class="mt-1 text-sm text-slate-500">Pick a store to view its menu, order food for delivery, or pick up in-store.</p>
        </div>
      </div>

      @if (loading()) {
        <div class="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          @for (placeholder of [1, 2, 3]; track placeholder) {
            <div class="animate-pulse rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
              <div class="h-44 w-full rounded-xl bg-slate-200"></div>
              <div class="mt-6 h-5 w-2/3 rounded bg-slate-200"></div>
              <div class="mt-2 h-4 w-1/2 rounded bg-slate-100"></div>
              <div class="mt-4 h-4 w-3/4 rounded bg-slate-100"></div>
            </div>
          }
        </div>
      } @else if (error()) {
        <div class="rounded-xl border border-red-200 bg-red-50 p-5 text-sm text-red-700">
          <div class="font-semibold">Unable to load stores</div>
          <p class="mt-1 text-xs text-red-600">{{ error() }}</p>
        </div>
      } @else if (stores().length === 0) {
        <div class="rounded-2xl border border-dashed border-slate-300 bg-slate-50/50 p-12 text-center">
          <div class="text-3xl">🍽️</div>
          <h3 class="mt-3 text-base font-semibold text-slate-900">No active restaurants found</h3>
          <p class="mt-1 text-sm text-slate-500">Check back later or register a store via the platform manager.</p>
        </div>
      } @else {
        <div class="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          @for (store of stores(); track store.publicId) {
            @let visuals = getVisuals(store);
            <a
              [routerLink]="['/', store.slug]"
              class="group relative flex flex-col overflow-hidden rounded-2xl border border-slate-200/90 bg-white shadow-sm transition-all duration-300 hover:-translate-y-1 hover:border-slate-300 hover:shadow-xl"
            >
              <!-- Cover Banner -->
              <div class="relative h-44 w-full overflow-hidden bg-slate-100">
                <img
                  [src]="visuals.bannerUrl"
                  [alt]="store.name"
                  class="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105"
                  (error)="onBannerError($event)"
                />
                <!-- Subtle gradient overlay -->
                <div class="absolute inset-0 bg-gradient-to-t from-black/60 via-black/10 to-transparent"></div>

                <!-- Open / Closed Badge -->
                <div class="absolute top-3 right-3">
                  @if (store.isAcceptingOrders) {
                    <span class="inline-flex items-center gap-1.5 rounded-full bg-emerald-500/95 px-3 py-1 text-xs font-semibold text-white shadow-sm backdrop-blur-sm">
                      <span class="h-2 w-2 rounded-full bg-white animate-pulse"></span>
                      Open Now
                    </span>
                  } @else {
                    <span class="inline-flex items-center rounded-full bg-slate-900/80 px-3 py-1 text-xs font-semibold text-slate-200 shadow-sm backdrop-blur-sm">
                      Closed
                    </span>
                  }
                </div>

                <!-- Store Logo Emblem (Cleanly positioned on the banner) -->
                <div class="absolute bottom-3 left-3.5 z-10">
                  <div class="h-14 w-14 overflow-hidden rounded-2xl border-2 border-white bg-white p-1 shadow-lg transition-transform duration-300 group-hover:scale-105">
                    <img
                      [src]="visuals.logoUrl"
                      [alt]="store.name + ' logo'"
                      class="h-full w-full object-contain"
                      (error)="onLogoError($event, store)"
                    />
                  </div>
                </div>

                <!-- Star Rating Pill (Bottom Right of Banner) -->
                @if (store.averageRating) {
                  <div class="absolute bottom-3 right-3 z-10 inline-flex items-center gap-1 rounded-full bg-white/95 px-2.5 py-0.5 text-xs font-bold text-amber-800 shadow-sm backdrop-blur-sm">
                    <span class="text-amber-500">★</span>
                    <span>{{ store.averageRating.toFixed(1) }}</span>
                    <span class="text-slate-400 font-normal text-[11px]">({{ store.reviewCount }})</span>
                  </div>
                }
              </div>

              <!-- Store Info Body -->
              <div class="flex flex-1 flex-col p-5 pt-4">
                <!-- Title & Slug -->
                <div class="flex items-start justify-between gap-2">
                  <div class="min-w-0 flex-1">
                    <h2 class="truncate text-lg font-bold text-slate-900 transition-colors group-hover:text-primary-600">
                      {{ store.name }}
                    </h2>
                    <p class="truncate text-xs font-medium text-slate-500">
                      {{ visuals.cuisine }}
                    </p>
                  </div>
                  <span class="shrink-0 rounded bg-slate-100 px-2 py-0.5 text-[11px] font-mono font-medium text-slate-500">
                    /{{ store.slug }}
                  </span>
                </div>

                <!-- Tagline -->
                <p class="mt-2 line-clamp-1 text-xs text-slate-600">
                  {{ visuals.tagline }}
                </p>

                <!-- Delivery Metrics -->
                <div class="mt-4 flex flex-wrap items-center gap-x-3 gap-y-1 border-t border-slate-100 pt-3 text-xs text-slate-600">
                  @if (store.estimatedDeliveryTime) {
                    <span class="inline-flex items-center gap-1 font-medium text-slate-700">
                      <span>⏱</span> {{ store.estimatedDeliveryTime }}
                    </span>
                  }
                  <span class="inline-flex items-center gap-1">
                    <span>🛵</span>
                    @if (store.deliveryFee === 0) {
                      <span class="font-semibold text-emerald-600">Free delivery</span>
                    } @else {
                      <span>₱{{ store.deliveryFee.toFixed(2) }} fee</span>
                    }
                  </span>
                  @if (store.freeDeliveryThreshold) {
                    <span class="rounded bg-emerald-50 px-1.5 py-0.5 text-[10px] font-semibold text-emerald-700">
                      Free over ₱{{ store.freeDeliveryThreshold }}
                    </span>
                  }
                </div>

                @if (store.address) {
                  <p class="mt-2 truncate text-xs text-slate-400 flex items-center gap-1">
                    <span>📍</span> {{ store.address }}
                  </p>
                }

                <!-- Action Footer -->
                <div class="mt-4 flex items-center justify-between border-t border-slate-50 pt-3 text-xs">
                  <span class="font-semibold text-primary-600 group-hover:text-primary-700 group-hover:underline">
                    View Menu & Order →
                  </span>
                  @if (store.openingHours) {
                    <span class="text-slate-400 text-[11px] truncate max-w-[140px] text-right">
                      {{ store.openingHours }}
                    </span>
                  }
                </div>
              </div>
            </a>
          }
        </div>
      }
    </div>
  `
})
export class StoreListComponent implements OnInit {
  private readonly api = inject(FoodOrderingApiService);
  private readonly tenant = inject(TenantService);

  readonly stores = signal<Store[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.tenant.setStoreSlug(null);
    this.api.getStores().subscribe({
      next: (list) => {
        this.stores.set(list.filter((s) => s.isActive));
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load stores. Make sure the .NET API is running on http://localhost:5230.');
        this.loading.set(false);
      }
    });
  }

  getVisuals(store: Store): StoreVisuals {
    return getStoreVisuals(store.slug, store.name, store.logoUrl, store.bannerImageUrl);
  }

  onBannerError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.src = '/images/stores/burger-barn.jpg';
  }

  onLogoError(event: Event, store: Store): void {
    const img = event.target as HTMLImageElement;
    const initials = (store.name || store.slug || 'FD')
      .split(/[\s-]+/)
      .slice(0, 2)
      .map(w => w[0]?.toUpperCase() || '')
      .join('') || 'FD';
    img.src = `data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"><circle cx="50" cy="50" r="50" fill="%234F46E5"/><text x="50" y="58" font-size="34" font-family="system-ui,sans-serif" font-weight="bold" fill="white" text-anchor="middle">${initials}</text></svg>`;
  }
}
