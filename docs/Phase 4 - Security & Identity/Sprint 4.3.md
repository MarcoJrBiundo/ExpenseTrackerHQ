

# Sprint 4.3 — Authentication & Authorization (Entra External ID)

> **Purpose of this document**
>
> This file is the **authoritative playbook** for implementing authentication and authorization for a backend API using **Microsoft Entra External ID**, **JWT Bearer authentication**, and **.NET**.
>
> It is intentionally verbose.
>
> You should be able to:
> - Recreate this sprint on a brand‑new project
> - Understand *why* each step exists
> - Debug issues without guessing
> - Apply the same pattern to other APIs or environments

---

## 1. Sprint Goal (What “Done” Means)

By the end of Sprint 4.3:

- The API **rejects unauthenticated requests**
- The API **accepts valid JWT access tokens**
- Tokens are issued by **Microsoft Entra External ID**
- Authorization is enforced via `[Authorize]`
- No frontend application is required yet
- Testing is performed using **Postman**

This sprint is **about identity and trust**, not UI.

---

## 2. Why Entra External ID (Not Azure AD B2C)

Originally, Azure AD B2C was planned.

However:
- As of 2025, **Azure AD B2C is no longer available to new customers**
- Microsoft’s forward‑looking replacement is **Microsoft Entra External ID**

**Key takeaway:**
> External ID is the *correct* long‑term solution and aligns with modern Entra architecture.

The mental model remains the same:
- Identity lives outside your app
- Your API trusts tokens issued by Entra

---

## 3. High‑Level Architecture

```
User / Postman
     ↓
Entra External ID (OIDC + OAuth2)
     ↓  (JWT Access Token)
ExpenseTracker API
     ↓
[Authorize] middleware
```

Important separation of concerns:
- **Entra** → authenticates users
- **API** → validates tokens, enforces authorization
- **Database** → unaware of identity

---

## 4. Create the External ID Tenant

This tenant is **identity‑only**.

It does **not** contain:
- AKS
- Azure SQL
- Terraform resources

That is intentional.

### Steps

1. Azure Portal → **Microsoft Entra ID**
2. Manage tenants → **Create**
3. Choose **External** tenant
4. Provide:
   - Tenant name (e.g. `ExpenseTrackerHQ Dev`)
   - Initial domain
   - Region

This tenant exists **solely to issue tokens**.

---

## 5. App Registrations Overview

I create **three logical identities**:

| App | Purpose |
|---|---|
| API App | Represents the backend API |
| SPA App | Represents a future frontend |
| Postman App | Testing client (native/public) |

Each app has a *different role*.

---

## 6. Register the API Application

### Why this matters

The API app:
- Defines **what resource is being accessed**
- Defines **which scopes exist**
- Becomes the **audience (`aud`)** of access tokens

### Steps

1. External ID tenant → App registrations → New registration
2. Name: `ExpenseTracker API`
3. Supported account types:
   - **Accounts in this organizational directory only**
4. Redirect URI: *none*

### Result

You receive:
- **Application (client) ID** → used as JWT audience

---

## 7. Expose API Scopes

Scopes define **what a client is allowed to do**.

### Steps

1. API app → **Expose an API**
2. Set Application ID URI:

```
api://<API_CLIENT_ID>
```

3. Add scope:

| Field | Value |
|---|---|
| Scope name | `access_as_user` |
| Who can consent | Admins and users |
| Description | Access ExpenseTracker API |

### Why this is critical

- Access tokens contain `scp`
- The API validates that required scopes exist

---

## 8. Register the SPA Client (Placeholder)

Even though no frontend exists yet, this models reality.

### Purpose

- Represents a browser‑based app
- Used later in Phase 6

### Steps

1. App registrations → New registration
2. Name: `ExpenseTracker SPA`
3. Account types: same directory only
4. Redirect URI:
   - Platform: **Single‑page application**
   - URL: `http://localhost:5173`

### Permissions

- API permissions → Add
- Select ExpenseTracker API
- Add `access_as_user`

---

## 9. Register the Postman Client (Critical)

### Why this exists

Postman **cannot** redeem SPA tokens correctly.

So I create a **native/public client**:

- No client secret
- Uses Authorization Code + PKCE

### Steps

1. App registrations → New registration
2. Name: `ExpenseTracker Postman`
3. Account types: same directory only
4. Redirect URI:
   - Platform: **Mobile and desktop applications**
   - URL: `https://oauth.pstmn.io/v1/browser-callback`

This app is **only for testing**.

---

## 10. Optional Token Claims

I enable useful identity claims.

### Configuration

Postman app → Token configuration:

- ID token:
  - `email`
  - `given_name`
  - `family_name`

These are not required for auth but help future user mapping.

---

## 11. User Flow (Sign‑up / Sign‑in)

External ID uses **user flows** to issue tokens.

### Steps

1. External ID → User flows
2. Create **Sign up and sign in** flow
3. Configure:
   - Email as login
   - Default attributes
4. Attach applications:
   - SPA app
   - Postman app

This flow becomes the **authorization endpoint experience**.

---

## 12. OpenID Connect Metadata

This metadata powers JWT validation.

Example:
```
https://<tenant>.ciamlogin.com/<tenant-id>/v2.0/.well-known/openid-configuration
```

From this document I get:
- Issuer
- Signing keys
- Authorization endpoint
- Token endpoint

.NET uses this automatically.

---

## 13. API Configuration (.NET)

### Packages

```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

### Program.cs

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://<tenant>.ciamlogin.com/<tenant-id>/v2.0";
        options.Audience = "<API_CLIENT_ID>";
    });

builder.Services.AddAuthorization();
```

### Middleware Order

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Order matters.

---

## 14. Protecting Endpoints

I apply `[Authorize]`.

Example:

```csharp
[ApiController]
[Route("api/v1/users/{userId}/expenses")]
[Authorize]
public class ExpensesController : ControllerBase
{
}
```

Result:
- No token → **401**
- Invalid token → **401**
- Valid token → request proceeds

---

## 15. Why userId Is Still in the Route

At this stage:
- `userId` is a route parameter
- JWT identity is not yet mapped to domain users

This is **intentional**.

### Phase 6 change

Later I will:
- Extract `sub` from JWT
- Enforce ownership
- Remove userId from client control

Sprint 4.3 only proves **authentication works**.

---

## 16. Postman Token Generation (End‑to‑End Test)

### OAuth2 Settings Used

| Setting | Value |
|---|---|
| Grant type | Authorization Code (PKCE) |
| Callback URL | https://oauth.pstmn.io/v1/browser-callback |
| Auth URL | /oauth2/v2.0/authorize |
| Token URL | /oauth2/v2.0/token |
| Client ID | Postman app ID |
| Scope | openid profile email api://<API_ID>/access_as_user |

### Result

- Browser login
- Token issued
- Access token pasted into Postman

---

## 17. Validation Results

| Scenario | Result |
|---|---|
| No token | 401 |
| Invalid token | 401 |
| Valid token | 200 |
| Valid token + DB call | Data returned |

This confirms:
- Identity works
- Auth works
- Secure runtime is intact

---

## 18. Sprint 4.3 Exit Criteria (Final)

✔ API rejects unauthenticated requests  
✔ API accepts Entra‑issued tokens  
✔ JWT validation works end‑to‑end  
✔ No frontend dependency  

**Sprint 4.3 is complete.**

---

## 19. What Comes Next

Sprint 4.4:
- Azure API Management
- JWT validation at the edge
- Rate limiting
- Public surface lockdown

This sprint lays the foundation.

---

> **Final note**
>
> This implementation matches real enterprise systems.
> Nothing here is demo‑ware.
> This playbook is reusable across projects.