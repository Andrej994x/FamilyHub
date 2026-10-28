---
name: ef-migration
description: Safe EF Core schema changes and migration review for FamilyHub (pinned dotnet-ef, Up/Down review checklist, data-loss = S3, never touch the local database). Use when an entity, DbContext or entity configuration changes, or when reviewing a migration.
---

# EF Core migration (FamilyHub)

Rules: AGENTS.md → *Backend conventions* (Data, Migrations). Tooling: `dotnet-tools.json` pins `dotnet-ef` 8.0.11. **Always** use `dotnet tool run dotnet-ef`, never a global `dotnet ef`.

## 1. Change the model
- Entity in `Models/`, configuration in `Data/Configurations/` (`IEntityTypeConfiguration<T>`).
- Store enums as strings (`HasConversion<string>()`). Use `DateTimeOffset` (UTC) for timestamps and `DateOnly` for calendar dates.
- Think about delete behaviour (Cascade, Restrict or NoAction) for every new relationship. Member references may need clearing in code (see `FamilyService` member removal).

## 2. Create the migration
```bash
cd backend/FamilyHub.Api
dotnet tool run dotnet-ef migrations add <DescriptiveName>
```
- Name it after the intent (`AddPetVaccinations`), not "Update1".
- Never edit or delete a migration that already exists on `main`. To change something already shipped, add a new migration.
- `dotnet tool run dotnet-ef migrations remove` (only for a migration that isn't on `main` yet) asks for owner approval.

## 3. Review the generated `Up`/`Down` and the snapshot
Read the migration as SQL-to-be. Classify the result:

| Pattern in the migration | Risk |
|---|---|
| Only new tables, nullable columns, indexes on new columns | S2 |
| `DropColumn`/`DropTable`, or a rename that EF generated as drop + add (check for `RenameColumn` versus `DropColumn` + `AddColumn`) | **S3: data loss** |
| Nullable → required without `defaultValue`, type narrowing, `maxLength` shrink | **S3: fails or truncates on existing rows** |
| New unique index or FK on existing data | **S3** unless existing data is proven clean |
| `Sql(...)` data updates, delete-behaviour changes on existing FKs | **S3** |

For S3, stop and present the plan and the migration to the owner before going further. Also check:
- `Down` really reverses `Up`;
- the snapshot (`AppDbContextModelSnapshot.cs`) diff contains only the intended model change.

## 4. Verify without touching any real database
```bash
cd backend/FamilyHub.Api
dotnet tool run dotnet-ef migrations has-pending-model-changes   # must say "No changes"
dotnet tool run dotnet-ef migrations script --idempotent -o <scratchpad>/migrations.sql   # review the SQL
cd .. && dotnet test FamilyHub.Api.IntegrationTests   # applies all migrations to a disposable Testcontainers SQL Server (Docker)
```
- **Never run `dotnet tool run dotnet-ef database update`** against LocalDB, the local FamilyHub database or any shared server. The guard asks for approval, because it is owner-only.
- Starting the API in Development also migrates the configured database, so it asks for approval too.
- CI re-validates the migration: the **EF Core migrations** job applies all migrations to an empty SQL Server 2022 and re-runs the idempotent script.
- Add or extend integration tests when the change affects queries, constraints, `ExecuteUpdate`/`ExecuteDelete` or delete behaviour (InMemory unit tests don't enforce relational rules).

## 5. Report
Give:
- the migration name;
- the risk classification with its reason;
- the `has-pending-model-changes` result;
- the exact test counts;
- anything the owner must decide.
