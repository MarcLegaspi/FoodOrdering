import { Injectable, computed, signal } from '@angular/core';
import { MenuItem } from '../models/api.models';

export interface CartItem {
  menuItemPublicId: string;
  name: string;
  unitPrice: number;
  quantity: number;
}

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly _storeSlug = signal<string | null>(null);
  private readonly _items = signal<CartItem[]>([]);

  readonly items = this._items.asReadonly();
  readonly itemCount = computed(() =>
    this._items().reduce((sum, item) => sum + item.quantity, 0)
  );
  readonly totalAmount = computed(() =>
    this._items().reduce((sum, item) => sum + item.unitPrice * item.quantity, 0)
  );

  setStore(slug: string): void {
    const normalized = slug.trim().toLowerCase();
    if (this._storeSlug() === normalized) {
      return;
    }

    this._storeSlug.set(normalized);
    this._items.set(this.readFromStorage(normalized));
  }

  addItem(menuItem: MenuItem): void {
    const current = this._items();
    const existing = current.find((i) => i.menuItemPublicId === menuItem.publicId);

    const updated = existing
      ? current.map((i) =>
          i.menuItemPublicId === menuItem.publicId
            ? { ...i, quantity: i.quantity + 1 }
            : i
        )
      : [
          ...current,
          {
            menuItemPublicId: menuItem.publicId,
            name: menuItem.name,
            unitPrice: menuItem.price,
            quantity: 1
          }
        ];

    this.commit(updated);
  }

  updateQuantity(menuItemPublicId: string, quantity: number): void {
    if (quantity <= 0) {
      this.removeItem(menuItemPublicId);
      return;
    }

    this.commit(
      this._items().map((i) =>
        i.menuItemPublicId === menuItemPublicId ? { ...i, quantity } : i
      )
    );
  }

  removeItem(menuItemPublicId: string): void {
    this.commit(this._items().filter((i) => i.menuItemPublicId !== menuItemPublicId));
  }

  clear(): void {
    this.commit([]);
  }

  private commit(items: CartItem[]): void {
    this._items.set(items);
    const slug = this._storeSlug();
    if (!slug) {
      return;
    }

    if (items.length === 0) {
      localStorage.removeItem(this.storageKey(slug));
    } else {
      localStorage.setItem(this.storageKey(slug), JSON.stringify(items));
    }
  }

  private readFromStorage(slug: string): CartItem[] {
    try {
      const raw = localStorage.getItem(this.storageKey(slug));
      return raw ? (JSON.parse(raw) as CartItem[]) : [];
    } catch {
      return [];
    }
  }

  private storageKey(slug: string): string {
    return `fo_cart:${slug}`;
  }
}
