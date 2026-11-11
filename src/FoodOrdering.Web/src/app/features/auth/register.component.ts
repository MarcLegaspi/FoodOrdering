import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="mx-auto mt-10 max-w-md rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
      <h1 class="text-xl font-bold text-slate-900">Create account</h1>
      <p class="mt-1 text-sm text-slate-500">Order from any store with one customer account.</p>

      @if (errors().length > 0) {
        <div class="mt-4 rounded-lg bg-red-50 p-3 text-sm text-red-700">
          @for (err of errors(); track err) {
            <p>{{ err }}</p>
          }
        </div>
      }

      <form (ngSubmit)="submit()" class="mt-5 space-y-4">
        <div>
          <label class="block text-sm font-medium text-slate-700">Full name</label>
          <input
            type="text"
            name="fullName"
            [(ngModel)]="fullName"
            required
            class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-slate-900 focus:outline-none"
          />
        </div>

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
          {{ loading() ? 'Creating account...' : 'Sign up' }}
        </button>
      </form>

      <p class="mt-4 text-center text-sm text-slate-500">
        Already have an account?
        <a routerLink="/login" class="font-medium text-slate-900 underline">Sign in</a>
      </p>
    </div>
  `
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  fullName = '';
  email = '';
  password = '';
  readonly loading = signal(false);
  readonly errors = signal<string[]>([]);

  submit(): void {
    this.errors.set([]);
    this.loading.set(true);

    this.auth.register(this.fullName, this.email, this.password).subscribe({
      next: () => {
        this.loading.set(false);
        void this.router.navigateByUrl('/');
      },
      error: (err) => {
        this.loading.set(false);
        const list = err?.error?.errors;
        this.errors.set(Array.isArray(list) ? list : ['Could not create account.']);
      }
    });
  }
}
