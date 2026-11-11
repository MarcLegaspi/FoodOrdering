import { Routes } from '@angular/router';
import { adminGuard, authGuard, storeRoleGuard } from './core/auth/auth.guards';
import { StoreRole } from './core/models/api.models';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/storefront/store-list.component').then((m) => m.StoreListComponent)
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/register.component').then((m) => m.RegisterComponent)
  },
  {
    path: 'orders',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/orders/my-orders.component').then((m) => m.MyOrdersComponent)
  },
  {
    path: 'admin/stores',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./features/admin/admin-stores.component').then((m) => m.AdminStoresComponent)
  },
  {
    path: ':storeSlug/manage',
    canActivate: [storeRoleGuard(StoreRole.Staff)],
    loadComponent: () =>
      import('./features/manage/store-manage.component').then((m) => m.StoreManageComponent)
  },
  {
    path: ':storeSlug',
    loadComponent: () =>
      import('./features/storefront/store-menu.component').then((m) => m.StoreMenuComponent)
  },
  {
    path: '**',
    redirectTo: ''
  }
];
