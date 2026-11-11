import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, StoreRole, UserInfo } from '../models/api.models';

const TOKEN_STORAGE_KEY = 'fo_access_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/auth`;

  private readonly _token = signal<string | null>(localStorage.getItem(TOKEN_STORAGE_KEY));
  private readonly _user = signal<UserInfo | null>(null);
  private loadedOnce = false;

  readonly token = this._token.asReadonly();
  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._token() !== null && this._user() !== null);
  readonly isAdmin = computed(() => this._user()?.roles.includes('Admin') ?? false);
  readonly managedStores = computed(() => this._user()?.managedStores ?? []);

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/login`, { email, password })
      .pipe(tap((res) => this.setSession(res)));
  }

  register(fullName: string, email: string, password: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/register`, { fullName, email, password })
      .pipe(tap((res) => this.setSession(res)));
  }

  ensureSessionLoaded(): Observable<boolean> {
    if (!this._token()) {
      return of(false);
    }

    if (this._user() && this.loadedOnce) {
      return of(true);
    }

    return this.http.get<UserInfo>(`${this.baseUrl}/me`).pipe(
      tap((user) => {
        this._user.set(user);
        this.loadedOnce = true;
      }),
      map(() => true),
      catchError(() => {
        this.logout();
        return of(false);
      })
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    this._token.set(null);
    this._user.set(null);
    this.loadedOnce = false;
  }

  getStoreRole(slug: string | null): StoreRole | null {
    if (!slug) {
      return null;
    }

    if (this.isAdmin()) {
      return StoreRole.Owner;
    }

    const normalized = slug.trim().toLowerCase();
    return this.managedStores().find((s) => s.slug === normalized)?.role ?? null;
  }

  hasStoreRole(slug: string | null, minRole: StoreRole): boolean {
    const role = this.getStoreRole(slug);
    return role !== null && role >= minRole;
  }

  private setSession(response: AuthResponse): void {
    localStorage.setItem(TOKEN_STORAGE_KEY, response.accessToken);
    this._token.set(response.accessToken);
    this._user.set(response.user);
    this.loadedOnce = true;
  }
}
