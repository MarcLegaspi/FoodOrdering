import { environment } from '../../../environments/environment';

export function resolveImageUrl(imageUrl: string | null | undefined): string | null {
  if (!imageUrl) {
    return null;
  }

  if (imageUrl.startsWith('/uploads/') && environment.apiUrl) {
    return `${environment.apiUrl}${imageUrl}`;
  }

  return imageUrl;
}
