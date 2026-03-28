# 🔐 Auth Template

A production-grade authentication and authorization backend built with **ASP.NET 10** and **PostgreSQL**. Designed as a reusable starter for SaaS applications that need encrypted user data, tag-based permissions, device-pinned sessions, and a full admin dashboard — out of the box.

> **Built by [Márk Pásztor](https://github.com/pasztor-mark)** — This project reflects my approach to backend architecture: security-first, vertically sliced, and ready to extend, proven in personal hobby projects and production.

---

## Highlights

- **Field-Level Encryption** — Emails, usernames, and PII are AES-256-CBC encrypted at rest with blind-index lookups for querying
- **Pepper-Versioned Blind Indexes** — Searchable encrypted fields using HMAC-SHA256 with rotatable pepper keys
- **Device-Pinned Sessions** — Refresh tokens are bound to user-agent + IP, with middleware that rejects mismatched devices
- **Tag-Based RBAC** — Flexible permission system where tags (Member, Pro, Admin, etc.) map to granular feature permissions
- **Silent Token Refresh** — Middleware transparently refreshes expired JWTs using HTTP-only cookies, no client-side token handling
- **Soft Delete & GDPR Anonymization** — Users can deactivate accounts with full data anonymization and reactivation support
- **Activity Tracking** — Buffered heartbeat system with a background service that batch-writes page-level engagement data
- **Comprehensive Rate Limiting** — Per-endpoint fixed-window rate limiting across 13 policy tiers
- **Transactional Email System** — Templated HTML emails for confirmation, password recovery, security alerts, and account actions via SMTP/MailKit

---

## Architecture

```
auth-template/
├── Configuration/          Global config (rate limits, regexes, device rules)
├── Entities/               DbContext, data models, seed configurations
│   ├── Configuration/      EF seed data & permission mappings
│   ├── Data/               Entity classes (AppUser, AppPermission, etc.)
│   └── Interfaces/         ISoftDeletable, IAnonymizable
├── Enums/                  Shared enums
├── Features/               Vertical slices (one folder per domain)
│   ├── Auth/               Registration, login, logout, token management
│   │   ├── Attributes/     [WithPermissions], [OwnerOrPermission]
│   │   ├── Configuration/  Auth constants (token lifetimes, lockout rules)
│   │   ├── Controllers/    AuthController
│   │   ├── Entities/       AppRefreshToken, AppUserPreferences
│   │   ├── Responses/      SelfResponse, PermissionTransfer, etc.
│   │   ├── Services/       AuthService, IAuthService
│   │   ├── Transfer/       DTOs (RegisterDto, LoginDto, etc.)
│   │   ├── Utilities/      JWT, Refresh, Permissions, Activity, User
│   │   └── Validation/     FluentValidation rules
│   ├── Admin/              Dashboard KPIs, user management, audit logs
│   ├── Email/              Confirmation, password reset, email change
│   └── Profile/            User profiles, avatars, visibility toggle
├── Middleware/             TokenRefresh, DevicePinning, ErrorHandler
├── Options/                Strongly-typed config (Security, Database, CORS)
├── Responses/              Shared response envelope (Response<T>, Paged<T>)
├── Utilities/              Encryption, hashing, auditing, rate limiting
│   └── Security/           Encryptor, GeneralHasher, PasswordVerifier, PepperProvider
├── Migrations/             EF Core migration history
├── Program.cs              Application bootstrap & DI configuration
└── Dockerfile              Multi-stage Linux container build
```

Each **Feature** follows the same internal structure (Controllers → Services → Utilities → Transfer → Validation → Responses), keeping related code colocated and minimizing cross-feature coupling.

---

## Security Model

### Encryption at Rest

All PII fields (`Email`, `NormalizedEmail`, `UserName`, `NormalizedUserName`) are encrypted via AES-256-CBC with a random IV per value. EF Core value converters handle transparent encrypt/decrypt so application code works with plaintext.

### Blind Indexes

Since encrypted values can't be queried with `WHERE`, each encrypted field has a corresponding blind index column — an HMAC-SHA256 hash computed with a versioned pepper. This allows exact-match lookups on encrypted data without exposing the plaintext.

Peppers are versioned (1–7 slots), and the system automatically upgrades old indexes to the current version on read (lazy migration).

### Token Security

| Token | Storage | Lifetime | Protection |
|-------|---------|----------|------------|
| JWT Access | `HttpOnly` + `Secure` + `SameSite=None` cookie | 10 min | HMAC-SHA256 signed, device-pinned via `uah` claim |
| Refresh | `HttpOnly` + `Secure` + `SameSite=None` cookie | 3 months | HMAC-SHA256 hashed before storage, IP + UA indexed |

Refresh tokens are never stored in plaintext — only the HMAC hash is persisted. Device pinning compares the user-agent hash in the JWT against the current request's user-agent on every authenticated request.

### Permission System

```
Tags (roles)          Permissions (features)
┌──────────────┐      ┌─────────────────────────┐
│ Member       │──┬──▶│ Content.Read            │
│              │  ├──▶│ Content.Create           │
│              │  ├──▶│ Content.Update           │
│              │  └──▶│ Users.Read               │
├──────────────┤      ├─────────────────────────┤
│ ProTier      │─────▶│ Content.ProFeature       │
├──────────────┤      ├─────────────────────────┤
│ Administrator│──┬──▶│ Users.Delete             │
│              │  ├──▶│ System.AuditLogs.View    │
│              │  ├──▶│ System.Roles.Manage      │
│              │  └──▶│ System.Settings.Manage   │
└──────────────┘      └─────────────────────────┘
```

Tags are assigned to users. Each tag maps to a set of permissions via seed data. The `[WithPermissions("Permission.Name")]` attribute on endpoints checks the user's resolved permissions. Permissions are cached in-memory with a 10-minute TTL.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/) (or Docker)
- [Mailhog](https://github.com/mailhog/MailHog) (for local email testing)

### 1. Clone & Configure

```bash
git clone https://github.com/pasztor-mark/auth-template.git
cd auth-template
cp .env.example .env
```

Open `.env` and fill in your values. Generate keys with:

```bash
# AES / JWT / HMAC keys (32-byte, base64)
openssl rand -base64 32

# Blind index peppers (64-byte, hex)
openssl rand -hex 64
```

### 2. Start Dependencies

```bash
# PostgreSQL
docker run -d --name template-db \
  -e POSTGRES_USER=user \
  -e POSTGRES_PASSWORD=password \
  -e POSTGRES_DB=template-db \
  -p 2345:5432 postgres:17

# Mailhog
docker run -d --name mailhog -p 1025:1025 -p 8025:8025 mailhog/mailhog
```

### 3. Run Migrations & Start

```bash
dotnet ef database update
dotnet run
```

The API starts at `https://localhost:5377`. Mailhog UI is at `http://localhost:8025`.

---

## API Overview

### Auth (`/api/auth`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/register` | — | Create a new account |
| `POST` | `/login` | — | Authenticate with email/username + password |
| `POST` | `/refresh` | — | Refresh the access token via cookie |
| `POST` | `/logout/device` | ✅ | Log out current device |
| `POST` | `/logout` | ✅ | Log out all devices |
| `GET` | `/self` | ✅ | Get current user + permissions |
| `GET` | `/preferences` | ✅ | Get user preferences |
| `GET` | `/availability/email` | — | Check email availability |
| `GET` | `/availability/username` | — | Check username availability |
| `PATCH` | `/change/password` | ✅ | Change password (requires current) |
| `PATCH` | `/change/username` | ✅ | Change username |
| `PUT` | `/me/reactivate` | — | Reactivate a deactivated account |
| `DELETE` | `/me/delete` | ✅ | Deactivate & anonymize account |
| `POST` | `/heartbeat` | ✅ | Report page activity |

### Email (`/api/email`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/confirm/send` | ✅ | Send confirmation email |
| `POST` | `/confirm` | — | Confirm email with token |
| `POST` | `/forgot-password/send` | — | Send password reset email |
| `POST` | `/forgot-password` | — | Reset password with token |
| `POST` | `/change-email/send` | ✅ | Request email change |
| `POST` | `/change-email` | ✅ | Confirm email change |

### Profile (`/api/profile`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/me` | ✅ | Get own profile |
| `GET` | `/u/{username}` | — | Get public profile |
| `PUT` | `/u/{username}/update/core` | ✅¹ | Update bio, headline, location |
| `PATCH` | `/me/update/avatar` | ✅ | Upload profile picture |
| `PATCH` | `/{id}/update/visibility` | ✅¹ | Toggle profile visibility |
| `DELETE` | `/{id}/delete` | ✅¹ | Wipe profile data (GDPR) |

### Admin (`/api/admin`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/dashboard/summary` | ✅² | Full dashboard (growth, engagement, health) |
| `GET` | `/dashboard/user-growth` | ✅² | User registration stats |
| `GET` | `/dashboard/system-health` | ✅² | Flagged/banned user counts |
| `GET` | `/dashboard/users/tag/{tag}` | ✅² | List users by tag name |
| `GET` | `/dashboard/audit/{username}` | ✅² | User audit log |
| `GET` | `/u/{username}` | ✅² | Get user management view |
| `DELETE` | `/u/{username}/ban` | ✅² | Ban user with reason |
| `PATCH` | `/u/{username}/unban` | ✅² | Unban user |

> ¹ Owner or user with `Users.Update` permission  
> ² Requires specific system permissions (e.g., `System.AuditLogs.View`)

---

## Using as a Template

### Adding a New Feature

1. Create a folder under `Features/YourFeature/` with the standard subfolders:
   ```
   Features/YourFeature/
   ├── Controllers/
   ├── Services/
   ├── Transfer/
   ├── Responses/
   ├── Validation/
   ├── Entities/      (if new DB tables needed)
   └── Utilities/     (if feature-specific helpers needed)
   ```

2. Register your service in `Program.cs` under the `#region Services` block.

3. If your feature needs new permissions, add entries to `TagConstants`, `PermissionIds`, `PermissionDictionary`, and `AppPermissionConfiguration`.

### Adding New Encrypted Fields

1. Add the field to your entity
2. In `AppDbContext.OnModelCreating`, add a value converter using `_encryptor.Encrypt`/`Decrypt`
3. If the field needs to be searchable, add a corresponding `*Index` column and compute it with `_encryptor.GenerateBlindIndex` in `SaveChangesAsync`

### Customizing Rate Limits

Edit `Configuration/RateLimitValues.cs`. Each policy is a named `FixedWindowRateLimiter` registered in `RegisterAllPolicies()`. Apply to endpoints with `[EnableRateLimiting(nameof(RateLimits.YourPolicy))]`.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 10 |
| Framework | ASP.NET Core (Minimal Hosting) |
| Database | PostgreSQL 17 via Npgsql + EF Core 10 |
| Identity | ASP.NET Core Identity |
| Auth | JWT Bearer (cookie-based) |
| Validation | FluentValidation 12 + SharpGrip AutoValidation |
| Email | MailKit + MimeKit |
| Caching | IMemoryCache (in-process) |
| Containerization | Docker (multi-stage) |