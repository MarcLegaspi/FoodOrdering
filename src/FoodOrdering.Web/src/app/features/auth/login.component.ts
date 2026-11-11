import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="mx-auto mt-10 max-w-md rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
      <h1 class="text-xl font-bold text-slate-900">Sign in</h1>
      <p class="mt-1 text-sm text-slate-500">Sign in as a Customer, Store Staff/Owner, or Admin.</p>

      @if (error()) {
        <div class="mt-4 rounded-lg bg-red-50 p-3 text-sm text-red-700">
          {{ error() }}
        </div>
      }

      <form (ngSubmit)="submit()" class="mt-5 space-y-4">
        <div>
          <label class="block text-sm font-medium text-slate-700">Email</label>
          <input
            type="email"
            name="email"
            [(ngModel)]="email"
            required
            class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-slate-900 focus:outline-none"
          />
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-700">Password</label>
          <input
            type="password"
            name="password"
            [(ngModel)]="password"
            required
            class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-slate-900 focus:outline-none"
          />
        </div>

        <button
          type="submit"
          [disabled]="loading()"
          class="w-full rounded-lg bg-slate-900 py-2 text-sm font-semibold text-white hover:bg-slate-800 disabled:opacity-50"
        >
          {{ loading() ? 'Signing in...' : 'Sign in' }}
        </button>
      </form>

      <p class="mt-4 text-center text-sm text-slate-500">
        No account yet?
        <a routerLink="/register" class="font-medium text-slate-900 underline">Create one</a>
      </p>
    </div>
  `
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  email = '';
  password = '';
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  submit(): void {
    this.error.set(null);
    this.loading.set(true);

    this.auth.login(this.email, this.password).subscribe({
      next: (res) => {
        this.loading.set(false);
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        if (returnUrl) {
          void this.router.navigateByUrl(returnUrl);
          return;
        }

        if (res.user.roles.includes('Admin')) {
          void this.router.navigateByUrl('/admin/stores');
        } else if (res.user.managedStores.length > 0) {
          void this.router.navigate(['/', res.user.managedStores[0].slug, 'manage']);
        } else {
          void this.router.navigateByUrl('/');
        }
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Invalid email or password.');
      }
    });
  }
}
