# Study - Sprint 1 scope (Issue #1, #5, #6)

This package is intentionally scoped to:

- Issue #1: Home page.
- Issue #5: Subject management (list, create, edit, activate/deactivate, guarded delete).
- Issue #6: Assign subjects to lecturers (select active lecturer, assign/remove subject, view assignments).

## Minimal access prerequisite

A minimal Admin login/logout flow is retained only because Issue #5 and #6 require an authenticated Admin session. Student registration, account CRUD, lecturer CRUD, exam/question management, online exams, results, and practice features are not exposed in this package. Non-Admin accounts are not allowed to enter this reduced Admin workspace.

## Notes about supporting models

Some data entities and DbSets for questions/exams/practice remain in `Models` and `ApplicationDbContext` to keep compatibility with the existing SQL Server database and to prevent subject deletion when existing questions/exams still reference a subject. These entities do not have controllers, views, or navigable features in this package.

## Database prerequisites

- The existing SQL Server database must be reachable through `ConnectionStrings:DefaultConnection` in `appsettings.json`.
- `Roles` must contain an Admin role named `Admin` and a Lecturer role named `Lecturer`.
- An active Admin account must already exist.
- Lecturer accounts must already exist; this package does not create or manage Lecturer accounts.

## Run

Open `Study.sln` in Visual Studio with .NET 8 SDK, restore NuGet packages, verify the SQL Server connection string, then run the `Study` project. The application source was statically reviewed and packaged, but could not be built or runtime-tested in the creation environment because the .NET SDK is unavailable there.
