import type { ReactNode } from 'react';

export interface NavItem {
  labelKey: string;
  path: string;
  icon: ReactNode;
}

const svgProps = {
  className: 'h-6 w-6',
  fill: 'none',
  viewBox: '0 0 24 24',
  strokeWidth: 1.7,
  stroke: 'currentColor',
} as const;

export const navItems: NavItem[] = [
  {
    labelKey: 'nav.dashboard',
    path: '/dashboard',
    icon: (
      <svg {...svgProps}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 12l8.954-8.955a1.5 1.5 0 012.122 0L22.5 12M4.5 9.75V21h5.25v-6h4.5v6h5.25V9.75" />
      </svg>
    ),
  },
  {
    labelKey: 'nav.calendar',
    path: '/calendar',
    icon: (
      <svg {...svgProps}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 8.25h18M3 8.25A2.25 2.25 0 015.25 6h13.5A2.25 2.25 0 0121 8.25v10.5A2.25 2.25 0 0118.75 21H5.25A2.25 2.25 0 013 18.75V8.25z" />
      </svg>
    ),
  },
  {
    labelKey: 'nav.tasks',
    path: '/tasks',
    icon: (
      <svg {...svgProps}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
    ),
  },
  {
    labelKey: 'nav.shopping',
    path: '/shopping',
    icon: (
      <svg {...svgProps}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 3h1.386c.51 0 .955.343 1.087.835l.383 1.437M4.5 5.25h15l-1.5 8.25H6L4.5 5.25zm2 12a.75.75 0 100 1.5.75.75 0 000-1.5zm10.5 0a.75.75 0 100 1.5.75.75 0 000-1.5z" />
      </svg>
    ),
  },
  {
    labelKey: 'nav.family',
    path: '/family',
    icon: (
      <svg {...svgProps}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z" />
      </svg>
    ),
  },
  {
    labelKey: 'nav.pickups',
    path: '/pickups',
    icon: (
      <svg {...svgProps}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M15 10.5a3 3 0 11-6 0 3 3 0 016 0z" />
        <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 10.5c0 7.142-7.5 11.25-7.5 11.25S4.5 17.642 4.5 10.5a7.5 7.5 0 1115 0z" />
      </svg>
    ),
  },
  {
    labelKey: 'nav.vault',
    path: '/vault',
    icon: (
      <svg {...svgProps}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M12 15v2.25m-6.75-9.75V6a6.75 6.75 0 0113.5 0v1.5m-15 0h16.5A1.5 1.5 0 0121 9v10.5a1.5 1.5 0 01-1.5 1.5h-15A1.5 1.5 0 013 19.5V9a1.5 1.5 0 011.5-1.5z" />
      </svg>
    ),
  },
];
