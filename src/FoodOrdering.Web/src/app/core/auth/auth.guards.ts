import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { StoreRole } from '../models/api.models';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.ensureSessionLoaded().pipe(
    map((ok) => (ok ? true : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } })))
  );
};

export const adminGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.ensureSessionLoaded().pipe(
    map((ok) => {
      if (!ok) {
        return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
      }
      return auth.isAdmin() ? true : router.createUrlTree(['/']);
    })
  );
};

export function storeRoleGuard(minRole: StoreRole): CanActivateFn {
  return (route, state) => {
    const auth = inject(AuthService);
    const router = inject(Router);
    const slug = route.paramMap.get('storeSlug');

    return auth.ensureSessionLoaded().pipe(
      map((ok) => {
        if (!ok) {
          return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
        }
        return auth.hasStoreRole(slug, minRole)
          ? true
          : router.createUrlTree([slug ? `/${slug}` : '/']);
      })
    );
  };
}
