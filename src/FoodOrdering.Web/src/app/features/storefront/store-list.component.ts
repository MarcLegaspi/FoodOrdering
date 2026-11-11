import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Store } from '../../core/models/api.models';
import { FoodOrderingApiService } from '../../core/services/food-ordering-api.service';
import { TenantService } from '../../core/tenant/tenant.service';

@Component({
  selector: 'app-store-list',
  imports: [RouterLink],
  template: `
    <div class="space-y-6">
      <div>
        <h1 class="text-2xl font-bold text-slate-900">Browse Stores</h1>
        <p class="mt-1 text-sm text-slate-500">Pick a restaurant to view its menu and place an order.</p>
      </div>

      @if (loading()) {
        <p class="text-sm text-slate-500">Loading stores...</p>
      } @else if (error()) {
        <div class="rounded-lg bg-red-50 p-4 text-sm text-red-700">{{ error() }}</div>
      } @else {
        <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          @for (store of stores(); track store.publicId) {
            <a
              [routerLink]="['/', store.slug]"
              class="group rounded-xl border border-slate-200 bg-white p-5 shadow-sm transition hover:border-slate-400"
            >
              <div class="flex items-center justify-between">
                <h2 class="text-lg font-semibold text-slate-900 group-hover:underline">
                  {{ store.name }}
                </h2>
                @if (store.isAcceptingOrders) {
                  <span class="rounded-full bg-emerald-50 px-2.5 py-0.5 text-xs font-medium text-emerald-700">
                    Open
                  </span>
                } @else {
                  <span class="rounded-full bg-amber-50 px-2.5 py-0.5 text-xs font-medium text-amber-800">
                    Closed
                  </span>
                }
              </div>
              <p class="mt-1 text-xs text-slate-400">/{{ store.slug }}</p>
              @if (store.address) {
                <p class="mt-3 text-sm text-slate-600">{{ store.address }}</p>
              }
              @if (store.openingHours) {
                <p class="mt-1 text-xs text-slate-500">{{ store.openingHours }}</p>
              }
              @if (store.email) {
                <p class="mt-2 text-xs text-slate-400">{{ store.email }}</p>
              }
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
}
