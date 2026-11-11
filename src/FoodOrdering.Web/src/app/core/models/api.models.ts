export enum StoreRole {
  Staff = 1,
  Manager = 2,
  Owner = 3
}

export enum OrderStatus {
  Pending = 1,
  Confirmed = 2,
  Preparing = 3,
  Ready = 4,
  Completed = 5,
  Cancelled = 6
}

export interface ManagedStore {
  storePublicId: string;
  slug: string;
  name: string;
  role: StoreRole;
}

export interface UserInfo {
  id: string;
  fullName: string;
  email: string;
  roles: string[];
  managedStores: ManagedStore[];
}

export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  user: UserInfo;
}

export interface Store {
  publicId: string;
  slug: string;
  name: string;
  email: string;
  phone: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface CreateStoreRequest {
  slug: string;
  name: string;
  email: string;
  phone: string;
  ownerEmail?: string | null;
  ownerFullName?: string | null;
  ownerPassword?: string | null;
}

export interface Category {
  publicId: string;
  name: string;
  sortOrder: number;
  isActive: boolean;
  createdAtUtc: string;
}

export interface MenuItem {
  publicId: string;
  categoryPublicId: string;
  categoryName: string;
  name: string;
  description: string;
  price: number;
  imageUrl: string | null;
  isAvailable: boolean;
  sortOrder: number;
  createdAtUtc: string;
}

export interface SaveMenuItemRequest {
  categoryPublicId: string;
  name: string;
  description: string;
  price: number;
  imageUrl?: string | null;
  isAvailable: boolean;
  sortOrder: number;
}

export interface OrderItem {
  menuItemPublicId: string;
  itemName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface Order {
  publicId: string;
  storeSlug: string;
  storeName: string;
  orderNumber: string;
  status: OrderStatus;
  totalAmount: number;
  notes: string;
  createdAtUtc: string;
  items: OrderItem[];
}

export interface PlaceOrderRequest {
  items: { menuItemPublicId: string; quantity: number }[];
  notes?: string | null;
}

export interface StoreMember {
  userId: string;
  fullName: string;
  email: string;
  role: StoreRole;
  createdAtUtc: string;
}

export interface AddStoreMemberRequest {
  email: string;
  role: StoreRole;
  fullName?: string | null;
  password?: string | null;
}

export function orderStatusLabel(status: OrderStatus): string {
  switch (status) {
    case OrderStatus.Pending:
      return 'Pending';
    case OrderStatus.Confirmed:
      return 'Confirmed';
    case OrderStatus.Preparing:
      return 'Preparing';
    case OrderStatus.Ready:
      return 'Ready';
    case OrderStatus.Completed:
      return 'Completed';
    case OrderStatus.Cancelled:
      return 'Cancelled';
    default:
      return 'Unknown';
  }
}

export function storeRoleLabel(role: StoreRole): string {
  switch (role) {
    case StoreRole.Owner:
      return 'Owner';
    case StoreRole.Manager:
      return 'Manager';
    case StoreRole.Staff:
      return 'Staff';
    default:
      return 'Member';
  }
}
