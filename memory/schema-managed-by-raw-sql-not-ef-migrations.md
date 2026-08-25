---
name: schema-managed-by-raw-sql-not-ef-migrations
description: DB schema is managed by raw SQL scripts, NOT EF migrations — never run `dotnet ef database update`
metadata:
  type: project
---

The EasySchoolDB schema is managed by **raw SQL scripts** (`School.API/Scripts/*.sql`, stored procedures, manual `ALTER TABLE`), NOT by EF Core migrations. The `__EFMigrationsHistory` table is **empty** and the app never calls `Database.Migrate()`/`EnsureCreated()` at startup. The EF model snapshot has drifted far from the real DB.

**Why:** Running `dotnet ef migrations add` scaffolds a huge **destructive** migration (drops `Exams`/`ExamResults`, renames `SMSSections`→`SMSSection`, creates DTO-named tables from accidentally-mapped DTOs). `dotnet ef migrations remove` also deleted the wrong (previously-committed) migration files + snapshot.

**How to apply:** To add a column/table, write an idempotent SQL script in `School.API/Scripts/` (e.g. `IF COL_LENGTH(...) IS NULL ALTER TABLE ... ADD ...`) and run it via `sqlcmd -S . -U sa -P 786 -d EasySchoolDB -i <file>`. EF runtime maps entities by model config (not the snapshot), so adding the entity property + the real DB column is enough — no migration needed. Never run `dotnet ef migrations add/remove/update` here. Connection: `Server=.;Database=EasySchoolDB;User Id=sa;Password=786`.
