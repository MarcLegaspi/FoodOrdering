import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class TenantService {
  private readonly _storeSlug = signal<string | null>(null);

  readonly storeSlug = this._storeSlug.asReadonly();

  setStoreSlug(slug: string | null): void {
    const normalized = slug?.trim().toLowerCase() || null;
    this._storeSlug.set(normalized);
  }
}
