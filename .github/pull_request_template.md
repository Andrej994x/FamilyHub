## Summary
<!-- What does this change and why? Link the roadmap phase or issue. -->

## Changes
-

## How to verify
<!-- Steps a reviewer can follow locally, including any setup (user-secrets, migrations). -->

## Checklist
- [ ] CI is green (backend build, EF Core migrations, frontend build & lint, i18n parity, secret scan)
- [ ] Model changes include a migration (`dotnet tool run dotnet-ef migrations add <Name>`)
- [ ] New UI strings are added to **both** `en` and `mk` translation files
- [ ] No secrets in code or config: real values live in user-secrets or environment variables
- [ ] README / docs updated if setup or behavior changed
- [ ] Tested manually where CI can't (e.g. push notifications, PWA install, mobile layout)

## Screenshots
<!-- For UI changes: before/after, desktop and mobile. Delete if not applicable. -->
