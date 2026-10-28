import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach, beforeEach } from 'vitest';
// The real i18n setup, so components render actual English/Macedonian strings.
import i18n from '../locales/i18n';

beforeEach(async () => {
  localStorage.clear();
  sessionStorage.clear();
  await i18n.changeLanguage('en');
});

afterEach(() => {
  cleanup();
});
