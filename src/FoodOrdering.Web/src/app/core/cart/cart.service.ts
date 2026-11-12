import { Injectable, computed, signal } from '@angular/core';
import { MenuItem } from '../models/api.models';

export interface CartSelectedOption {
  publicId: string;
  groupName: string;
  name: string;
  priceDelta: number;
}

export interface CartItem {
  cartItemKey: string;
  menuItemPublicId: string;
  name: string;
  basePrice: number;
  unitPrice: number;
  quantity: number;
  selectedOptions: CartSelectedOption[];
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

  addItem(menuItem: MenuItem, selectedOptions: CartSelectedOption[] = []): void {
    const sortedOptions = [...selectedOptions].sort((a, b) => a.publicId.localeCompare(b.publicId));
    const optionKey = sortedOptions.map((o) => o.publicId).join(',');
    const cartItemKey = optionKey ? `${menuItem.publicId}:${optionKey}` : menuItem.publicId;
    const unitPrice = menuItem.price + sortedOptions.reduce((sum, opt) => sum + opt.priceDelta, 0);

    const current = this._items();
    const existing = current.find((i) => i.cartItemKey === cartItemKey);

    const updated = existing
      ? current.map((i) =>
          i.cartItemKey === cartItemKey
            ? { ...i, quantity: i.quantity + 1 }
            : i
        )
      : [
          ...current,
          {
            cartItemKey,
            menuItemPublicId: menuItem.publicId,
            name: menuItem.name,
            basePrice: menuItem.price,
            unitPrice,
            quantity: 1,
            selectedOptions: sortedOptions
          }
        ];

    this.commit(updated);
  }

  updateQuantity(cartItemKey: string, quantity: number): void {
    if (quantity <= 0) {
      this.removeItem(cartItemKey);
      return;
    }

    this.commit(
      this._items().map((i) =>
        i.cartItemKey === cartItemKey ? { ...i, quantity } : i
      )
    );
  }

  removeItem(cartItemKey: string): void {
    this.commit(this._items().filter((i) => i.cartItemKey !== cartItemKey));
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
      if (!raw) {
        return [];
      }

      const parsed = JSON.parse(raw) as Partial<CartItem>[];
      return parsed.map((item) => ({
        cartItemKey: item.cartItemKey ?? item.menuItemPublicId ?? '',
        menuItemPublicId: item.menuItemPublicId ?? '',
        name: item.name ?? '',
        basePrice: item.basePrice ?? item.unitPrice ?? 0,
        unitPrice: item.unitPrice ?? 0,
        quantity: item.quantity ?? 1,
        selectedOptions: item.selectedOptions ?? []
      }));
    } catch {
      return [];
    }
  }

  private storageKey(slug: string): string {
    return `fo_cart_${slug}`;
  }
}
