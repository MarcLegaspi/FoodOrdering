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
  Cancelled = 6,
  /** Rider assigned and dispatched; out for delivery to customer */
  OutForDelivery = 7
}

/** Payment option chosen by the customer */
export enum PaymentMethod {
  /** Pay rider upon arrival with cash or rider's GCash QR */
  PayOnDelivery = 1,
  /** Pre-paid online via GCash for express priority pickup */
  GCashOnline = 2,
  /** Pay at cashier counter upon pickup */
  PayAtCounter = 3
}

/** Payment lifecycle status */
export enum PaymentStatus {
  Pending = 1,
  Paid = 2,
  Failed = 3
}

/** Actual method rider collected payment on delivery */
export enum PaymentCollectionMethod {
  Cash = 1,
  GCash = 2
}

export enum OrderType {
  Pickup = 1,
  Delivery = 2
}

export enum CustomerType {
  Registered = 1,
  Guest = 2,
  WalkIn = 3
}

export enum PromoDiscountType {
  Percentage = 1,
  FixedAmount = 2
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
  address: string;
  openingHours: string;
  isActive: boolean;
  isAcceptingOrders: boolean;
  createdAtUtc: string;
  deliveryFee: number;
  minimumDeliveryOrder: number;
  freeDeliveryThreshold?: number | null;
  estimatedDeliveryTime: string;
  averageRating?: number | null;
  reviewCount: number;
}

export interface UpdateStoreProfileRequest {
  name: string;
  email: string;
  phone: string;
  address: string;
  openingHours: string;
  deliveryFee?: number;
  minimumDeliveryOrder?: number;
  freeDeliveryThreshold?: number | null;
  estimatedDeliveryTime?: string;
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

export interface MenuItemOption {
  publicId: string;
  name: string;
  priceDelta: number;
  sortOrder: number;
}

export interface MenuItemOptionGroup {
  publicId: string;
  name: string;
  isRequired: boolean;
  allowMultiple: boolean;
  sortOrder: number;
  options: MenuItemOption[];
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
  optionGroups: MenuItemOptionGroup[];
}

export interface SaveMenuItemOptionRequest {
  name: string;
  priceDelta: number;
  sortOrder: number;
}

export interface SaveMenuItemOptionGroupRequest {
  name: string;
  isRequired: boolean;
  allowMultiple: boolean;
  sortOrder: number;
  options: SaveMenuItemOptionRequest[];
}

export interface SaveMenuItemRequest {
  categoryPublicId: string;
  name: string;
  description: string;
  price: number;
  imageUrl?: string | null;
  isAvailable: boolean;
  sortOrder: number;
  optionGroups?: SaveMenuItemOptionGroupRequest[] | null;
}

export interface OrderItemOption {
  groupName: string;
  optionName: string;
  priceDelta: number;
}

export interface OrderItem {
  menuItemPublicId: string;
  itemName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  options: OrderItemOption[];
}

export interface Order {
  publicId: string;
  storeSlug: string;
  storeName: string;
  orderNumber: string;
  customerType: CustomerType;
  customerName: string;
  customerEmail?: string | null;
  status: OrderStatus;
  orderType: OrderType;
  customerPhone: string;
  deliveryAddress: string | null;
  deliveryLandmark?: string | null;
  subtotal: number;
  promoCode?: string | null;
  discountAmount: number;
  deliveryFee: number;
  totalAmount: number;
  estimatedDeliveryTime?: string;
  notes: string;
  paymentMethod: PaymentMethod;
  paymentStatus: PaymentStatus;
  cashChangeFor?: number | null;
  driverName?: string | null;
  driverPhone?: string | null;
  dispatchedAtUtc?: string | null;
  deliveredAtUtc?: string | null;
  paymentCollectedVia?: PaymentCollectionMethod | null;
  paidAtUtc?: string | null;
  paymentReference?: string | null;
  deliveryPin?: string | null;
  proofOfDeliveryImageUrl?: string | null;
  createdAtUtc: string;
  items: OrderItem[];
  review?: OrderReview | null;
}

export interface PlaceOrderRequest {
  items: { menuItemPublicId: string; quantity: number; selectedOptionPublicIds?: string[] }[];
  orderType: OrderType;
  customerName?: string;
  customerPhone: string;
  customerEmail?: string | null;
  deliveryAddress?: string | null;
  deliveryLandmark?: string | null;
  promoCode?: string | null;
  notes?: string | null;
  paymentMethod?: PaymentMethod;
  cashChangeFor?: number | null;
}

export interface AssignDriverRequest {
  driverName: string;
  driverPhone: string;
}

export interface UpdateOrderStatusRequest {
  status: OrderStatus;
  paymentCollectedVia?: PaymentCollectionMethod | null;
  deliveryPin?: string | null;
}

export interface PromoCode {
  publicId: string;
  code: string;
  discountType: PromoDiscountType;
  discountValue: number;
  maxUses?: number | null;
  usedCount: number;
  startsAtUtc?: string | null;
  expiresAtUtc?: string | null;
  isActive: boolean;
  createdAtUtc: string;
}

export interface CreatePromoCodeRequest {
  code: string;
  discountType: PromoDiscountType;
  discountValue: number;
  maxUses?: number | null;
  startsAtUtc?: string | null;
  expiresAtUtc?: string | null;
}

export interface ValidatePromoResponse {
  isValid: boolean;
  code: string;
  discountType: PromoDiscountType;
  discountValue: number;
  discountAmount: number;
  message?: string | null;
  error?: string | null;
}

export interface StoreMember {
  userId: string;
  fullName: string;
  email: string;
  role: StoreRole;
  createdAtUtc: string;
}

export interface OrderReview {
  publicId: string;
  rating: number;
  comment?: string | null;
  customerName: string;
  createdAtUtc: string;
}

export interface CreateReviewRequest {
  rating: number;
  comment?: string | null;
}

export interface StoreReviewSummary {
  averageRating?: number | null;
  reviewCount: number;
  recentReviews: OrderReview[];
}

export interface AddStoreMemberRequest {
  email: string;
  role: StoreRole;
  fullName?: string | null;
  password?: string | null;
}

export function orderTypeLabel(orderType: OrderType): string {
  return orderType === OrderType.Delivery ? 'Delivery' : 'Pickup';
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
    case OrderStatus.OutForDelivery:
      return 'Out for Delivery';
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
