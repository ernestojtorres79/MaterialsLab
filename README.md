# Materials Testing Lab API

REST API for a construction materials testing laboratory. It manages projects, concrete mix designs, field samples and the compressive-strength tests run on their specimens, with a review workflow (Draft → Submitted → Approved / Rejected).

## Tech stack

- .NET 10, ASP.NET Core Web API (controllers)
- Entity Framework Core 10 with SQL Server (LocalDB by default)
- ASP.NET Core Identity (users and roles, Guid keys) with JWT bearer authentication
- OpenAPI document with the Scalar UI (Development only)

## Running the project

The database is created, migrated and seeded automatically when the application starts. Running it again does not duplicate data.

### Windows (SQL Server LocalDB)

Requires the .NET 10 SDK and SQL Server LocalDB (`(localdb)\MSSQLLocalDB`).

```bash
dotnet run --project src/MaterialsLab.Api
```

Open **http://localhost:5049/scalar**.

### macOS / Linux (SQL Server in Docker)

LocalDB is Windows-only, so run SQL Server in a container and point the app at it with a user secret (it overrides the LocalDB connection string on your machine only):

```bash
docker run -d --name sql --platform linux/amd64 \
  -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='<YourStrong!Passw0rd>' \
  -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest

dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1433;Database=MaterialsLab;User Id=sa;Password=<YourStrong!Passw0rd>;TrustServerCertificate=True" \
  --project src/MaterialsLab.Api

dotnet run --project src/MaterialsLab.Api
```

The JWT signing key for Development lives in `appsettings.Development.json`. In any other environment it must be supplied from a secret store or an environment variable (`Jwt__Key`, at least 32 characters).

## Test credentials

All seeded users share the password **`Matrix0001!`**.

| Username       | Role       | Notes                                          |
|----------------|------------|------------------------------------------------|
| `admin`        | Admin      |                                                |
| `reviewerTech` | Reviewer   |                                                |
| `tech1User`    | Technician | Assigned to project `PRJ-2026-014`             |
| `tech2User`    | Technician | Not assigned to any project (useful for 403/404 checks) |

Seeded data also includes one active project (`PRJ-2026-014`, Highway 59 Bridge Replacement) and one active mix design (`C-4000-A`, f'c = 4000 psi).

## Trying it in Scalar

1. `POST /api/auth/login` with `{ "username": "tech1User", "password": "Matrix0001!" }` and copy the `token`.
2. In Scalar, choose the **Bearer** authentication scheme and paste the token.
3. `GET /api/projects` and `GET /api/mixdesigns` to get the ids.
4. `POST /api/samples` with the project id, mix design id, `sampledAt`, `location` and specimens (at least one 28-day).
5. `PUT /api/samples/{sampleId}/specimens/{specimenId}/result` with `maxLoadLbf` and `testedAt` for each specimen.
6. `POST /api/samples/{id}/submit`, then log in as `reviewerTech` and `POST /api/samples/{id}/approve` or `/reject`.

Enums are serialized as strings (for example `"status": "Draft"`, `"age": "Days28"`).

## Endpoints

| Method | Route | Roles | Description |
|--------|-------|-------|-------------|
| POST | `/api/auth/login` | anonymous | Returns a JWT with the user's role claims |
| GET | `/api/projects` | any | Admin/Reviewer: all projects. Technician: assigned projects only |
| GET | `/api/mixdesigns` | any | Admin/Reviewer: all. Technician: active only |
| GET | `/api/mixdesigns/{id}` | any | Technicians get 404 for inactive mix designs |
| POST | `/api/mixdesigns` | Admin | Create (409 on duplicate code) |
| PUT | `/api/mixdesigns/{id}/inactivate` | Admin | Inactivate |
| DELETE | `/api/mixdesigns/{id}` | Admin | Delete (409 if any sample uses it) |
| GET | `/api/samples` | any | Filters: `projectId`, `mixDesignId`, `status`, `sampledFrom`, `sampledTo`. Technicians see only their projects' samples |
| GET | `/api/samples/{id}` | any | Technicians get 404 for samples outside their projects |
| POST | `/api/samples` | Technician (assigned) | Create a sample with its specimens |
| PUT | `/api/samples/{sampleId}/specimens/{specimenId}/result` | Technician (assigned) | Record max load and test time; the API calculates strength and Pass/Fail |
| POST | `/api/samples/{id}/submit` | Technician (assigned) | Draft/Rejected → Submitted (all specimens must have results) |
| POST | `/api/samples/{id}/approve` | Reviewer | Submitted → Approved |
| POST | `/api/samples/{id}/reject` | Reviewer | Submitted → Rejected |

Status codes: 200/201/204 on success, 400 for validation errors, 401 without a valid token, 403 for the wrong role (or a technician creating a sample on a project they are not assigned to), 404 for missing or not-visible resources, 409 for state and uniqueness conflicts.

## Business rules and design decisions

- **Sample codes** are `{ProjectCode}-{NNNN}`. The next number is a counter stored on the project (`NextSampleSequence`), so a number is never reused, even if a sample is deleted. A unique index on `(ProjectId, Sequence)` guards against races.
- **f'c snapshot:** the mix design's required strength is copied onto the sample at creation, so later mix design changes do not affect existing samples.
- **Atomic saves:** a sample, its specimens and the project's counter are saved in a single `SaveChanges` call, which EF Core runs in one transaction. If anything fails, nothing is saved.
- **Strength** = max load ÷ (π × (diameter ÷ 2)²), rounded to the nearest 10 psi with `MidpointRounding.AwayFromZero` (.NET defaults to banker's rounding, which would turn 4005 into 4000 instead of 4010). The client never sends a strength.
- **Tolerance** is measured from `sampledAt`: 7-day ± 6 h, 28-day ± 20 h. Results outside the window are rejected with 400.
- **Pass/Fail** is computed only when every 28-day specimen has a result: Pass if the average of the rounded 28-day strengths is ≥ the snapshotted f'c. 7-day results are informational. Re-entering a result recalculates it.
- **Visibility:** a technician who is not assigned to a project gets 404 for its samples (they should not learn that the sample exists). Creating a sample on such a project returns 403.
- **Deletes are restricted** at the database level for Sample → Project / MixDesign / creator, so a mix design in use cannot be removed even outside the API.
- Decimal precision: diameter `decimal(9,3)`, max load `decimal(12,1)`; dates are stored as `datetimeoffset`.

## Known gaps / next steps

Given the two-day time box, I prioritized the sample lifecycle (creation, results, calculations, workflow). Not yet implemented:

- Project management endpoints: create, update, close, assign/remove technicians (the seeded project shows the assignment model).
- Updating mix designs; editing or deleting samples while Draft/Rejected.
- Pagination on list endpoints.
- Automated tests (the `tests/MaterialsLab.Tests` project is set up; the strength, tolerance and Pass/Fail rules in `StrengthCalculator` are the first candidates).
- docker-compose for one-command setup on macOS/Linux.
- A consistent ProblemDetails error format (errors currently return `{ "message": "..." }` or the default validation response).
