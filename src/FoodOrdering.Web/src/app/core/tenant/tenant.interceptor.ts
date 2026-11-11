import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { TenantService } from './tenant.service';

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const tenant = inject(TenantService);
  const slug = tenant.storeSlug();

  const isApiRequest = !environment.apiUrl || req.url.startsWith(environment.apiUrl);
  if (!slug || !isApiRequest || req.headers.has('X-Store-Slug')) {
    return next(req);
  }

  return next(req.clone({ setHeaders: { 'X-Store-Slug': slug } }));
};
