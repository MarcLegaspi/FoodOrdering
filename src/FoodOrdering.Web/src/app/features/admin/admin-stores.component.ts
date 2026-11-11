import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Store } from '../../core/models/api.models';
import { FoodOrderingApiService } from '../../core/services/food-ordering-api.service';
import { TenantService } from '../../core/tenant/tenant.service';

@Component({
  selector: 'app-admin-stores',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="space-y-6">
      <div>
        <h1 class="text-2xl font-bold text-slate-900">Platform Admin · Stores</h1>
        <p class="mt-1 text-sm text-slate-500">Create new tenant stores and assign their Store Owner.</p>
      </div>

      @if (message()) {
        <div class="rounded-lg bg-emerald-50 p-3 text-sm text-emerald-700">{{ message() }}</div>
      }
      @if (error()) {
        <div class="rounded-lg bg-red-50 p-3 text-sm text-red-700">{{ error() }}</div>
      }

      <div class="grid gap-6 lg:grid-cols-2">
        <div class="rounded-xl border border-slate-200 bg-white p-5">
          <h2 class="font-bold text-slate-900">Create Store</h2>
          <form (ngSubmit)="createStore()" class="mt-4 space-y-3">
            <input
              type="text"
              name="slug"
              [(ngModel)]="slug"
              placeholder="Slug (e.g. taco-town)"
              required
              class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
            />
            <input
              type="text"
              name="name"
              [(ngModel)]="name"
              placeholder="Store name"
              required
              class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
            />
            <input
              type="email"
              name="email"
              [(ngModel)]="email"
              placeholder="Store email"
              class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
            />
            <input
              type="text"
              name="phone"
              [(ngModel)]="phone"
              placeholder="Store phone"
              class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
            />

            <div class="border-t border-slate-100 pt-3">
              <p class="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-400">
                Optional Initial Owner
              </p>
              <div class="space-y-2">
                <input
                  type="email"
                  name="ownerEmail"
                  [(ngModel)]="ownerEmail"
                  placeholder="Owner email"
                  class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                />
                <input
                  type="text"
                  name="ownerFullName"
                  [(ngModel)]="ownerFullName"
                  placeholder="Owner full name (if new account)"
                  class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                />
                <input
                  type="password"
                  name="ownerPassword"
                  [(ngModel)]="ownerPassword"
                  placeholder="Owner password (if new account)"
                  class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                />
              </div>
            </div>

            <button
              type="submit"
              class="w-full rounded-lg bg-slate-900 py-2 text-sm font-semibold text-white hover:bg-slate-800"
            >
              Create Store
            </button>
          </form>
        </div>

        <div class="rounded-xl border border-slate-200 bg-white p-5">
          <h2 class="font-bold text-slate-900">Existing Stores ({{ stores().length }})</h2>
          <div class="mt-3 divide-y divide-slate-100 text-sm">
            @for (s of stores(); track s.publicId) {
              <div class="flex items-center justify-between py-2.5">
                <div>
                  <p class="font-semibold text-slate-900">{{ s.name }}</p>
                  <p class="text-xs text-slate-400">/{{ s.slug }}</p>
                </div>
                <a [routerLink]="['/', s.slug]" class="text-xs font-medium text-slate-700 underline">
                  Open
                </a>
              </div>
            }
          </div>
        </div>
      </div>
    </div>
  `
})
export class AdminStoresComponent implements OnInit {
  private readonly api = inject(FoodOrderingApiService);
  private readonly tenant = inject(TenantService);

  readonly stores = signal<Store[]>([]);
  readonly message = signal<string | null>(null);
  readonly error = signal<string | null>(null);

  slug = '';
  name = '';
  email = '';
  phone = '';
  ownerEmail = '';
  ownerFullName = '';
  ownerPassword = '';

  ngOnInit(): void {
    this.tenant.setStoreSlug(null);
    this.loadStores();
  }

  createStore(): void {
    this.message.set(null);
    this.error.set(null);

    this.api
      .createStore({
        slug: this.slug,
        name: this.name,
        email: this.email,
        phone: this.phone,
        ownerEmail: this.ownerEmail || null,
        ownerFullName: this.ownerFullName || null,
        ownerPassword: this.ownerPassword || null
      })
      .subscribe({
        next: (created) => {
          this.message.set(`Created store '${created.name}' (/${created.slug}).`);
          this.slug = '';
          this.name = '';
          this.email = '';
          this.phone = '';
          this.ownerEmail = '';
          this.ownerFullName = '';
          this.ownerPassword = '';
          this.loadStores();
        },
        error: (err) => this.error.set(err?.error?.error ?? 'Could not create store.')
      });
  }

  private loadStores(): void {
    this.api.getStores().subscribe((list) => this.stores.set(list));
  }
}
