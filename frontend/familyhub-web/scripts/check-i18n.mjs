/*
 * i18n parity check: every locale in src/locales must have exactly the same keys as English,
 * the same {{interpolation}} placeholders per key, and no empty strings.
 *
 * English and Macedonian share the same plural categories (_one/_other), so plural keys are
 * compared like any other key. Run with `npm run i18n:check`; exits non-zero on any mismatch.
 */

import { readdirSync, readFileSync, existsSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const LOCALES_DIR = join(dirname(fileURLToPath(import.meta.url)), '..', 'src', 'locales');
const REFERENCE = 'en';

/** Flattens nested translation objects into a Map of dotted key → leaf value. */
function flatten(node, prefix = '', out = new Map()) {
  for (const [key, value] of Object.entries(node)) {
    const path = prefix ? `${prefix}.${key}` : key;
    if (value !== null && typeof value === 'object' && !Array.isArray(value)) {
      flatten(value, path, out);
    } else {
      out.set(path, value);
    }
  }
  return out;
}

function placeholders(value) {
  return [...String(value).matchAll(/\{\{\s*([^}\s,]+)[^}]*\}\}/g)].map((m) => m[1]).sort().join(',');
}

function load(locale) {
  return flatten(JSON.parse(readFileSync(join(LOCALES_DIR, locale, 'translation.json'), 'utf8')));
}

const locales = readdirSync(LOCALES_DIR, { withFileTypes: true })
  .filter((entry) => entry.isDirectory() && existsSync(join(LOCALES_DIR, entry.name, 'translation.json')))
  .map((entry) => entry.name);

const reference = load(REFERENCE);
const problems = [];

for (const locale of locales) {
  const keys = locale === REFERENCE ? reference : load(locale);

  for (const [key, value] of keys) {
    if (typeof value !== 'string') {
      problems.push(`${locale}: "${key}" is not a string`);
    } else if (value.trim() === '') {
      problems.push(`${locale}: "${key}" is empty`);
    }
  }

  if (locale === REFERENCE) {
    continue;
  }

  for (const [key, value] of reference) {
    if (!keys.has(key)) {
      problems.push(`${locale}: missing key "${key}"`);
    } else if (placeholders(value) !== placeholders(keys.get(key))) {
      problems.push(
        `${locale}: placeholders differ for "${key}" (${REFERENCE}: [${placeholders(value)}], ` +
          `${locale}: [${placeholders(keys.get(key))}])`,
      );
    }
  }
  for (const key of keys.keys()) {
    if (!reference.has(key)) {
      problems.push(`${locale}: extra key "${key}" (not in ${REFERENCE})`);
    }
  }
}

if (problems.length > 0) {
  console.error(`i18n parity check failed (${problems.length} problem(s)):`);
  for (const problem of problems) {
    console.error(`  - ${problem}`);
  }
  process.exit(1);
}

console.log(`i18n parity OK: ${locales.join(', ')} — ${reference.size} keys each.`);
