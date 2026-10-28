import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { LanguageSwitcher } from './LanguageSwitcher';
import i18n from '../locales/i18n';
import { LANGUAGE_STORAGE_KEY } from '../utils/constants';

describe('LanguageSwitcher', () => {
  it('offers English and Macedonian with the current language highlighted', () => {
    render(<LanguageSwitcher />);

    expect(screen.getByRole('button', { name: 'EN' })).toHaveClass('bg-brand-600');
    expect(screen.getByRole('button', { name: 'МК' })).not.toHaveClass('bg-brand-600');
  });

  it('switches to Macedonian and remembers the choice', async () => {
    render(<LanguageSwitcher />);

    await userEvent.click(screen.getByRole('button', { name: 'МК' }));

    expect(i18n.resolvedLanguage).toBe('mk');
    expect(localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBe('mk');
    expect(screen.getByRole('button', { name: 'МК' })).toHaveClass('bg-brand-600');
  });
});
