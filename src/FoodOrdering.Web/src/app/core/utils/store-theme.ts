import { resolveImageUrl } from './media-url';

export interface StoreVisuals {
  bannerUrl: string;
  logoUrl: string;
  cuisine: string;
  tagline: string;
  accentColor: string;
}

const KNOWN_STORES: Record<string, Partial<StoreVisuals>> = {
  'burger-barn': {
    bannerUrl: '/images/stores/burger-barn.jpg',
    logoUrl: '/images/stores/burger-barn-logo.svg',
    cuisine: 'Burgers · American · Fast Casual',
    tagline: 'Sizzling gourmet burgers & golden crisp fries',
    accentColor: '#D97706'
  },
  'pizza-palace': {
    bannerUrl: '/images/stores/pizza-palace.jpg',
    logoUrl: '/images/stores/pizza-palace-logo.svg',
    cuisine: 'Artisan Pizza · Italian · Pastas',
    tagline: 'Hand-stretched stone-oven pizzas & warm garlic knots',
    accentColor: '#DC2626'
  },
  'sushi-spot': {
    bannerUrl: '/images/stores/sushi-spot.jpg',
    logoUrl: '/images/stores/sushi-spot-logo.svg',
    cuisine: 'Sushi · Japanese · Rolls & Bowls',
    tagline: 'Fresh handcrafted sushi, sashimi & comforting miso',
    accentColor: '#0F766E'
  },
  'elis-goodies': {
    bannerUrl: '/images/stores/elis-goodies.jpg',
    logoUrl: '/images/stores/elis-goodies-logo.svg',
    cuisine: 'Bakery · Pastries · Sweet Treats',
    tagline: 'Freshly baked artisanal cookies, croissants & pastries',
    accentColor: '#BE185D'
  },
  'taco-town': {
    bannerUrl: '/images/stores/taco-town.jpg',
    logoUrl: '/images/stores/taco-town-logo.svg',
    cuisine: 'Tacos · Mexican · Street Food',
    tagline: 'Authentic street tacos, salsa & Mexican favorites',
    accentColor: '#EA580C'
  }
};

/**
 * Returns deterministic or configured visual assets and meta for any store.
 * Priority: Custom configured URL -> Preset catalog asset -> Dynamic fallback initials
 */
export function getStoreVisuals(
  slug?: string | null,
  name?: string | null,
  customLogoUrl?: string | null,
  customBannerUrl?: string | null
): StoreVisuals {
  const normalizedSlug = (slug || '').toLowerCase().trim();
  const known = KNOWN_STORES[normalizedSlug];

  const initials = (name || slug || 'Store')
    .split(/[\s-]+/)
    .slice(0, 2)
    .map(w => w[0]?.toUpperCase() || '')
    .join('') || 'FD';

  const defaultBanner = '/images/stores/burger-barn.jpg';
  const defaultLogo = `data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"><circle cx="50" cy="50" r="50" fill="%234F46E5"/><text x="50" y="58" font-size="34" font-family="system-ui,sans-serif" font-weight="bold" fill="white" text-anchor="middle">${initials}</text></svg>`;

  const resolvedLogo = customLogoUrl?.trim()
    ? (resolveImageUrl(customLogoUrl.trim()) || defaultLogo)
    : (known?.logoUrl || defaultLogo);

  const resolvedBanner = customBannerUrl?.trim()
    ? (resolveImageUrl(customBannerUrl.trim()) || defaultBanner)
    : (known?.bannerUrl || defaultBanner);

  return {
    bannerUrl: resolvedBanner,
    logoUrl: resolvedLogo,
    cuisine: known?.cuisine ?? 'Specialty Cuisine & Dining',
    tagline: known?.tagline ?? 'Delicious meals prepared fresh daily',
    accentColor: known?.accentColor ?? '#4F46E5'
  };
}
