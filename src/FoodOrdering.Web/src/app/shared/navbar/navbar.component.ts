import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { TenantService } from '../../core/tenant/tenant.service';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink],
  template: `
    <header class="border-b border-slate-200 bg-white">
      <div class="mx-auto flex max-w-6xl items-center justify-between px-4 py-3">
        <div class="flex items-center gap-6">
          <a routerLink="/" class="text-lg font-bold tracking-tight text-slate-900">
            FoodOrdering
          </a>

          @if (tenant.storeSlug(); as slug) {
            <span class="rounded-full bg-amber-100 px-2.5 py-0.5 text-xs font-medium text-amber-800">
              {{ slug }}
            </span>
          }
        </div>

        <nav class="flex items-center gap-4 text-sm font-medium text-slate-600">
          <a routerLink="/" class="hover:text-slate-900">Stores</a>

          @if (auth.isAuthenticated()) {
            <a routerLink="/orders" class="hover:text-slate-900">My Orders</a>

            @for (store of auth.managedStores(); track store.storePublicId) {
              <a
                [routerLink]="['/', store.slug, 'manage']"
                class="rounded-md bg-slate-100 px-2.5 py-1 text-slate-800 hover:bg-slate-200"
              >
                Manage {{ store.name }}
              </a>
            }

            @if (auth.isAdmin()) {
              @if (tenant.storeSlug(); as slug) {
                <a
                  [routerLink]="['/', slug, 'manage']"
                  class="rounded-md bg-slate-100 px-2.5 py-1 text-slate-800 hover:bg-slate-200"
                >
                  Manage {{ slug }}
                </a>
              }

              <a
                routerLink="/admin/stores"
                class="rounded-md bg-indigo-50 px-2.5 py-1 text-indigo-700 hover:bg-indigo-100"
              >
                Admin
              </a>
            }

            <span class="hidden text-xs text-slate-400 sm:inline">
              {{ auth.user()?.fullName }}
            </span>

            <button
              type="button"
              (click)="onLogout()"
              class="rounded-md border border-slate-300 px-3 py-1 text-slate-700 hover:bg-slate-50"
            >
              Logout
            </button>
          } @else {
            <a routerLink="/login" class="hover:text-slate-900">Login</a>
            <a
              routerLink="/register"
              class="rounded-md bg-slate-900 px-3 py-1.5 text-white hover:bg-slate-800"
            >
              Sign up
            </a>
          }
        </nav>
      </div>
    </header>
  `
})
export class NavbarComponent {
  readonly auth = inject(AuthService);
  readonly tenant = inject(TenantService);
  private readonly router = inject(Router);

  onLogout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/');
  }
}
