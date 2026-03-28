# Agent Ruleset — .NET Backend

This document is the authoritative reference for any AI agent working on a codebase forked from **auth-template**. Read this file in full before writing any code.

---

## 1. Universal Policies

- **Zero comments.** Write exactly zero lines of comments in source code. Explain technical trade-offs, indexing strategies, or design decisions in the chat interface only.
- **Zero `dynamic` types.** Never use `dynamic` or heavy `System.Reflection` usage in hot paths. This ensures AOT compatibility and predictable performance.
- **Symmetry.** If a frontend agent exists, the frontend must follow an analogous Vertical Slice structure to minimize context switching between codebases.
- **Async everywhere.** Every I/O-bound call must be `async`/`await`. Never use `.Result`, `.Wait()`, or `Task.Run()` to wrap synchronous code.
- **Passively monitor.** Do not intervene unprompted. Only act on explicit requests.

---

## 2. Architecture — Vertical Slice

### 2.1 Feature Structure

Every domain lives under `Features/<FeatureName>/`. Each feature is self-contained and follows this internal layout:

```
Features/<FeatureName>/
├── Attributes/        Custom authorization attributes (if needed)
├── Configuration/     Feature-specific constants and config classes
├── Controllers/       ASP.NET API controllers (thin, delegate to Services)
├── Entities/          EF Core entity classes owned by this feature
├── Enums/             Feature-scoped enums
├── Responses/         Outbound response models (returned to the client)
├── Services/          Business logic (interface + implementation pair)
├── Transfer/          Inbound DTOs (received from the client)
├── Utilities/         Feature-specific helpers, generators, resolvers
└── Validation/        FluentValidation validators for Transfer DTOs
```

Not every feature needs every subfolder. Create only what the feature requires, but always adhere to the internal layout of the existing features.

### 2.2 Shared Infrastructure

Code that is **not feature-specific** lives in top-level folders:

| Folder | Purpose |
|--------|---------|
| `Configuration/` | Global constants: regexes, rate limit definitions, device rules |
| `Entities/` | `AppDbContext`, shared entity models, seed configurations, interfaces |
| `Enums/` | Shared enums used across features |
| `Middleware/` | HTTP pipeline middleware (each in its own subfolder) |
| `Options/` | Strongly-typed `IOptions<T>` configuration classes |
| `Responses/` | Shared response envelope (`Response<T>`, `Paged<T>`) |
| `Utilities/` | Cross-cutting utilities (encryption, hashing, auditing, pagination) |
| `Validation/` | Shared validation rule helpers |

### 2.3 Dependency Direction

```
Controllers → Services → Utilities → Entities/DbContext
                      ↘ Transfer (DTOs)
                      ↘ Responses
```

- **Controllers** are thin. They extract claims/headers, call one service method, and wrap the result with `ResponseUtility<T>.HttpResponse()`.
- **Services** contain all business logic. They orchestrate multiple utilities and the DbContext within transactions where needed.
- **Utilities** are stateless helpers for a single concern (e.g., token generation, permission lookup, email sending).
- **Never** reference a Controller from a Service, or a Service from a Utility.

### 2.4 Cross-Feature Communication

Features may reference each other's **Utilities** and **Entities**, but never each other's **Services** or **Controllers**. If two features need to coordinate, extract the shared logic into a Utility or a shared service registered at the infrastructure level.

---

## 3. Data Layer

### 3.1 Database

- **ORM:** Entity Framework Core with PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL`.
- **Environment:** Use `DotNetEnv` to load `.env` files in development. Use `builder.Configuration.AddEnvironmentVariables()` to bind to `IOptions<T>` classes.
- **Connection string:** Constructed in `Program.cs` from individual `Database__*` env vars in development, or from a single `Database__ConnString` in production.

### 3.2 Query Strategy

- **Optimize for single-endpoint responses.** Each API call should ideally hit the database once. Use `IQueryable` projections (`.Select(...)`) to shape data into Response objects directly, avoiding loading full entity graphs.
- **Always use `AsNoTracking()`** for read-only queries.
- **Use `IgnoreQueryFilters()`** only when you explicitly need to bypass global filters (e.g., finding soft-deleted/flagged users for reactivation).
- **Never use `.ToList()` followed by LINQ-to-Objects** when the same query can be expressed in LINQ-to-Entities. Let PostgreSQL do the work.

### 3.3 Entity Conventions

- All entity primary keys are `Guid` unless the entity uses a composite key.
- Entities that support soft delete implement `ISoftDeletable` (`Flagged`, `DeletedAt`).
- Entities that support GDPR anonymization implement `IAnonymizable` (`Flagged`, `AnonymizedAt`, `Anonymize()`).
- `AppDbContext.SaveChangesAsync` intercepts `ISoftDeletable` entities on delete and converts them to soft deletes automatically.
- Use `[NotMapped]` for transient properties needed only during the save pipeline (e.g., `PlaintextEmailForIndexing`).

### 3.4 Seed Data

- Permission and tag seed data lives in `Entities/Configuration/`.
- Use `IEntityTypeConfiguration<T>` implementations applied via `builder.ApplyConfiguration(...)` in `OnModelCreating`.
- Static GUIDs for tags and permissions are defined in `TagConstants` and `PermissionIds` to ensure consistency across migrations and environments.

### 3.5 Migrations

- In development, `MigrateAsync()` runs at startup.
- Never use `EnsureCreatedAsync()` — it conflicts with the migration system.
- Ignore migration files (`Migrations/`) when reviewing or modifying code.

---

## 4. Security

### 4.1 Encryption at Rest (AES-256-CBC)

All PII fields are encrypted before storage using `IEncryptor.Encrypt()`. Decryption happens transparently via EF Core `ValueConverter<string, string>` in `OnModelCreating`.

Currently encrypted fields on `AppUser`:
- `Email`, `NormalizedEmail`, `UserName`, `NormalizedUserName`

Currently encrypted fields on `AppUserProfile`:
- `Bio`, `Headline`, `DateOfBirth`

**When adding a new encrypted field:**
1. Add the plaintext property to the entity.
2. In `OnModelCreating`, add a `.HasConversion(v => _encryptor.Encrypt(v), v => _encryptor.Decrypt(v))` converter.
3. If the field needs to be searchable, also add a blind index column (see §4.2).

### 4.2 Blind Indexes (HMAC-SHA256 + Versioned Peppers)

Encrypted fields cannot be queried with `WHERE`. For fields that need lookup, a **blind index** column stores `HMAC-SHA256(normalizedValue, pepper[version])`.

**Pattern for searchable encrypted fields:**
- Entity has both `Email` (encrypted) and `EmailIndex` (blind index).
- On save, `AppDbContext.SaveChangesAsync` computes the blind index from `PlaintextEmailForIndexing` using the current pepper version.
- On lookup, `UserUtils.GetAndUpgradeUserByEmailAsync` computes the blind index and queries the index column.

**Pepper versioning:** The system supports up to 7 pepper versions. Lookups try the current version first, then fall back to older versions. When a match is found on an old version, the index is transparently upgraded to the current version (lazy migration).

**When adding a new searchable encrypted field:**
1. Add both `FieldName` (encrypted, value converter) and `FieldNameIndex` (string, indexed).
2. Add a `[NotMapped] PlaintextFieldNameForIndexing` property.
3. In `SaveChangesAsync`, compute the blind index using `_encryptor.GenerateBlindIndex(plaintext, _currentVersion)`.
4. In lookup methods, implement the version-fallback loop pattern from `GetAndUpgradeUserByEmailAsync`.

### 4.3 Token Architecture

| Token | Cookie Name | Lifetime | Storage |
|-------|-------------|----------|---------|
| JWT Access Token | `X-Access-Token` | 10 minutes | `HttpOnly`, `Secure`, `SameSite=None` |
| Refresh Token | `X-Refresh-Token` | 3 months | `HttpOnly`, `Secure`, `SameSite=None` |

- Access tokens are JWT (HMAC-SHA256 signed) containing `sub`, `NameIdentifier`, `Name`, `Email`, role claims, permission claims, plus custom `uah` (user-agent hash) and `uav` (pepper version) claims.
- Refresh tokens are 32-byte random values. Only the `HMAC-SHA256(refreshToken)` is persisted — the raw token is never stored.
- The `TokenRefreshMiddleware` intercepts every request and silently refreshes the JWT if the access token cookie is missing but a valid refresh token cookie is present.
- Refresh tokens nearing expiration (within `RefreshTokenReplacementGracePeriodInDays`) are automatically replaced.

### 4.4 Device Pinning

The `DeviceIdentifierPinningMiddleware` compares the `uah` claim in the JWT against the HMAC of the current request's `User-Agent` header. Mismatches result in `401 Unauthorized`. This prevents token theft from being usable on a different device/browser.

Endpoints exempt from device pinning are configured in `DeviceIdentifierConfig.ShouldDisableIdentification()` (login, register, logout, refresh).

### 4.5 Permission Model

The system uses **Tag-Based Role-Based Access Control (RBAC)**:

```
User ──has──▶ UserTagPermission ──references──▶ Tag ──has──▶ TagPermission ──references──▶ Permission
```

- **Tags** are role-like groupings (Member, ProTier, PremiumTier, Moderator, Administrator, Banned).
- **Permissions** are granular feature flags (e.g., `Content.Read`, `System.AuditLogs.View`).
- **Mapping** is defined in `PermissionDictionary` and seeded via `AppUserTagConfiguration` / `AppUserTagPermissionConfiguration`.

**Authorization attributes:**
- `[WithPermissions("Permission.Name")]` — Requires the user to have at least one of the listed permissions.
- `[OwnerOrPermission("Permission.Name")]` — Allows if the user is the resource owner OR has the permission. Checks route values (`id`, `username`, `userId`, `usn`) and falls back to path segment parsing.

**When adding a new permission:**
1. Add a `const string` to `TagConstants` in the appropriate nested class.
2. Add a `static readonly Guid` to `PermissionIds`.
3. Add the seed entry to `AppPermissionConfiguration`.
4. Map it to the appropriate tags in `PermissionDictionary`.
5. Create an EF migration.

### 4.6 Rate Limiting

Each endpoint is protected by a named `FixedWindowRateLimiter` policy. Policies are defined in `Configuration/RateLimitValues.cs` and registered via the `RegisterAllPolicies()` extension method.

Apply with `[EnableRateLimiting(nameof(RateLimits.PolicyName))]`.

Available policies: `Email`, `PasswordChange`, `Login`, `Register`, `Search`, `Creation`, `ItemUpdate`, `ItemDelete`, `General`, `Refresh`, `UserUpdate`, `Heartbeat`, `ProfileUpdate`.

When adding a new endpoint, always apply a rate limit policy. If no existing policy fits, create a new one.

---

## 5. Request/Response Pipeline

### 5.1 Middleware Order

The middleware pipeline in `Program.cs` is order-sensitive. The current order is:

```
ForwardedHeaders → Routing → TokenRefreshMiddleware → CORS → CookiePolicy 
→ Authentication → (HTTPS/HSTS) → Session → ErrorHandlerMiddleware 
→ DeviceIdentifierPinningMiddleware → Authorization → RateLimiter → Controllers
```

Do not reorder middleware without understanding the implications. In particular:
- `TokenRefreshMiddleware` must run before `Authentication` so the refreshed JWT is available for the auth handler.
- `DeviceIdentifierPinningMiddleware` must run after `Authentication` so claims are populated.
- `ErrorHandlerMiddleware` must wrap all downstream middleware to catch unhandled exceptions.

### 5.2 Response Envelope

All API responses use a consistent envelope:

```json
{
  "data": <T or null>,
  "message": "string or null",
  "statusCode": 200,
  "errors": ["string"] or null
}
```

This is implemented via `LogicResult<T>` (service layer) and `ResponseUtility<T>.HttpResponse()` (controller layer).

### 5.3 The `LogicResult<T>` Pattern

Every service method returns `Task<LogicResult<T>>`. This encapsulates the HTTP status code, data, message, and errors without throwing exceptions for expected outcomes.

```csharp
// Success
return LogicResult<MyResponse>.Ok(data);
return LogicResult<MyResponse>.Created(data);
return LogicResult<MyResponse>.NoContent();

// Client errors
return LogicResult<MyResponse>.BadRequest("Validation failed");
return LogicResult<MyResponse>.NotFound("Resource not found");
return LogicResult<MyResponse>.Unauthenticated("Login required");     // 401
return LogicResult<MyResponse>.Unauthorized("Insufficient permissions"); // 403
return LogicResult<MyResponse>.Conflict("Already exists");

// Server errors
return LogicResult<MyResponse>.Error("Something went wrong");
```

Controllers convert this to HTTP with a single line:
```csharp
return ResponseUtility<MyResponse>.HttpResponse(await _service.DoSomethingAsync());
```

**Never throw exceptions for expected business logic outcomes.** Reserve exceptions for truly unexpected failures. Catch them at the service level and return `LogicResult.Error()`.

### 5.4 Validation

- Use **FluentValidation** for all inbound DTOs.
- Register validators via `AddValidatorsFromAssemblyContaining<AppDbContext>()`.
- Auto-validation is enabled via `SharpGrip.FluentValidation.AutoValidation` with a custom `ResultFactory` that formats errors into the standard response envelope.
- Validation classes live in `Features/<FeatureName>/Validation/` and follow the naming pattern `<DtoName>Validation.cs`.
- Reference shared rules from `Configuration/Regexes.cs` and constants from the feature's `Configuration/` folder.

**When generating a DTO, you must also provide:**
1. A FluentValidation validator with clear, user-facing error messages.
2. Appropriate constraints: `NotEmpty`, `MinimumLength`, `MaximumLength`, `Matches(regex)`.
3. The DTO and its validator in the chat for the frontend agent to replicate client-side.

---

## 6. Transfer Objects (DTOs)

### 6.1 Naming Conventions

| Direction | Suffix | Example | Location |
|-----------|--------|---------|----------|
| Client → Server | `Dto` | `RegisterDto`, `UpdateCoreProfileDto` | `Features/<Name>/Transfer/` |
| Server → Client | `Response` | `SelfResponse`, `ProfileResponse` | `Features/<Name>/Responses/` |

### 6.2 DTO Design Rules

- Use positional `record` types for simple DTOs: `public record LoginDto(string? email, string? username, string? password);`
- Use classes with init-only properties for complex DTOs with optional fields.
- Use `camelCase` for property names (JSON serialization default).
- Include a `Deconstruct` method or use records to enable tuple-style destructuring in services.
- Group related DTOs into subfolders (e.g., `Transfer/Create/`, `Transfer/Update/`).

### 6.3 Response Design Rules

- Response constructors should accept the entity and any supplementary data (e.g., permissions).
- Never expose entity navigation properties, internal IDs, or encrypted values in responses.
- For paginated endpoints, wrap the response list in `Paged<T>`.

---

## 7. Service Registration

All DI registrations happen in `Program.cs` within `#region` blocks:

| Lifetime | Use For |
|----------|---------|
| `Singleton` | Stateless, thread-safe utilities (Encryptor, PepperProvider, ActivityBuffer) |
| `Scoped` | Everything that touches DbContext or HttpContext (Services, Utilities, Hashers) |
| `Transient` | Rarely used. Only for truly stateless, disposable helpers. |
| `AddHostedService<T>` | Background services (e.g., ActivityRegister) |

When adding a new service:
1. Create the interface in the feature's `Services/` or `Utilities/` folder.
2. Create the implementation in the same folder.
3. Register in `Program.cs` under the appropriate `#region` and lifetime.

---

## 8. Email System

- Emails are sent via **MailKit** through `IEmailSenderClient`.
- Email templates are static methods in `Features/Email/Utilities/MimeMessages.cs`, returning `MimeMessage` objects with inline HTML.
- The sender client manages its own SMTP connection lifecycle and implements `IAsyncDisposable`.
- All user-provided content in emails must be HTML-encoded via `System.Net.WebUtility.HtmlEncode()`.
- URL parameters in email links must be encoded via `UrlSafeConverter.ToUrlSafe()` (Base64URL encoding).

---

## 9. Activity & Audit

### Audit Log (`AppUserUpdates`)
- `IAuditUtility.LogUserActivityAsync()` queues an audit entry on the change tracker.
- It does **not** call `SaveChangesAsync` — the caller is responsible for saving.
- Use audit logging for security-sensitive actions: login, password change, email change, ban, role change.

### Activity Tracking (`AppUserActivity`)
- Frontend sends heartbeats via `POST /api/auth/heartbeat` with `{ pageKey, seconds }`.
- The `ActivityBuffer` (singleton, `ConcurrentDictionary`) aggregates heartbeats in memory.
- The `ActivityRegister` (BackgroundService) flushes the buffer to the database every 5 minutes using bulk `ExecuteUpdateAsync` + insert fallback.

---

## 10. Controller Conventions

```csharp
[ApiController]
[Route("api/<feature>")]
public class MyController(IMyService _service, IHttpContextAccessor _http) : ControllerBase
{
    [HttpGet("resource")]
    [Authorize]
    [EnableRateLimiting(nameof(RateLimits.General))]
    public async Task<ActionResult<MyResponse>> GetResourceAsync(CancellationToken ct)
    {
        string? userId = _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return ResponseUtility<MyResponse>.HttpResponse(await _service.GetResourceAsync(userId, ct));
    }
}
```

**Rules:**
- Use primary constructor injection.
- Extract `userId` from claims in the controller, pass it as `string?` to the service.
- Always accept `CancellationToken` on GET endpoints and forward it to EF queries.
- Apply `[Authorize]` at the method or class level as appropriate.
- Apply `[EnableRateLimiting]` on every endpoint.
- Use `[WithPermissions]` or `[OwnerOrPermission]` for admin/privileged endpoints.
- Never put business logic in controllers.

---

## 11. Error Handling

- The `ErrorHandlerMiddleware` catches all unhandled exceptions and returns a `500` response in the standard envelope.
- For non-exception HTTP errors (401, 403, 404 from the framework), the middleware intercepts responses with `StatusCode >= 400` and wraps them in the envelope.
- Services should catch exceptions at transaction boundaries and return `LogicResult.Error()`.
- Never let database exceptions propagate to the controller. Catch `DbUpdateException` and return a user-friendly message.
- When rolling back transactions in catch blocks, always `await` the `RollbackAsync()` call.

---

## 12. Ignore List

When reviewing or modifying code, ignore these paths entirely:

- `bin/`, `obj/`
- `.idea/`
- `Migrations/`
- `.git/`
- `.DS_Store`
- `*.sln.DotSettings.user`

---

## 13. Checklist — Adding a New Feature

- [ ] Create `Features/<Name>/` with appropriate subfolders
- [ ] Define entities in `Features/<Name>/Entities/`
- [ ] Add `DbSet<T>` properties to `AppDbContext`
- [ ] Configure entity relationships/indexes in `OnModelCreating`
- [ ] Create Transfer DTOs in `Features/<Name>/Transfer/`
- [ ] Create FluentValidation validators in `Features/<Name>/Validation/`
- [ ] Create Response models in `Features/<Name>/Responses/`
- [ ] Define the service interface and implementation in `Features/<Name>/Services/`
- [ ] Register the service in `Program.cs` as `Scoped`
- [ ] Create the controller in `Features/<Name>/Controllers/`
- [ ] Apply `[Authorize]`, `[EnableRateLimiting]`, and permission attributes
- [ ] Add new permissions to `TagConstants`, `PermissionIds`, `PermissionDictionary`, `AppPermissionConfiguration` if needed
- [ ] Create an EF migration: `dotnet ef migrations add <Name>`
- [ ] Relay DTOs and response shapes to the frontend agent
