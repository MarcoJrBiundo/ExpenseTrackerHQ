

# Sprint 4.4 — API Gateway & Edge Security (APIM)

## Phase
**Phase 4 — Security, Identity & Secure Runtime**

---

## Sprint Goal

> Expose the backend **only through Azure API Management (APIM)** and enforce **edge‑level security controls** such as:
>
> - JWT validation  
> - Rate limiting  
> - Header normalization  
> - HTTPS entry point  

At the end of this sprint:
- AKS is **not treated as a public API**
- APIM is the **single public entry point**
- Invalid or abusive traffic is rejected **before it reaches AKS, pods, or the database**

This sprint completes **Phase 4**.

---

## Why This Sprint Exists (Big Picture)

Before Sprint 4.4:
- The API was reachable directly through AKS ingress (`expense.local`)
- Authentication happened **inside the API only**
- There was no centralized traffic control or gateway enforcement

That setup is not production‑grade.

### Problems With Direct AKS Exposure
- Every bad request hits:
  - Ingress
  - Pods
  - Application middleware
- No centralized throttling
- No governance layer
- Hard to scale to multiple APIs later

### What APIM Adds
APIM becomes:
- The **edge gateway**
- The **security bouncer**
- The **traffic governor**
- The **single public surface**

Security is moved **outward**, closer to the internet.

---

## Sprint Scope (What Was Implemented)

### Feature: API Management (APIM) as Front Door

Tasks completed:
1. Provision APIM via Terraform  
2. Import backend API using OpenAPI  
3. Apply APIM policies:
   - Header normalization
   - Rate limiting
   - JWT validation

---

# Task 1 — Provision Azure API Management (Terraform)

## Why Terraform Is Used

APIM is:
- Slow to provision (15–30 minutes)
- Expensive if misconfigured
- A platform‑level resource

Terraform provides:
- Idempotency
- Documentation
- Versioned infrastructure
- Safe re‑creation

---

## What Was Created

- **Azure API Management instance**
- SKU: **Developer**
  - Lowest cost
  - No SLA
  - Intended for dev/test

### Output
Azure generates a public gateway URL:

```
https://apim-expensetracker-dev-<suffix>.azure-api.net
```

### Provisioning Time
APIM creation regularly takes **15–30 minutes**.
Terraform appearing “stuck” during this time is **normal**.

---

## Key APIM Concepts (Required Knowledge)

| Concept | Meaning |
|------|------|
| APIM Service | The gateway instance |
| API | Logical API exposed by APIM |
| Backend | Where APIM forwards traffic |
| Product | Grouping of APIs + subscription rules |
| Policy | XML‑based request/response control |

---

# Task 2 — Import Backend API into APIM

## Why Import OpenAPI

- Prevents manual route creation
- Guarantees route parity with the API
- Keeps APIM and API definitions aligned

The OpenAPI spec was sourced from the .NET API’s Swagger output.

---

## API URL Construction

APIM exposes APIs using:

```
https://{apim-name}.azure-api.net/{api-suffix}/{route}
```

Example:
```
https://apim-expensetracker-dev.azure-api.net/expense/api/v1/users/{userId}/expenses
```

### API URL Suffix
- Set to: `expense`
- Becomes part of every public URL
- Mandatory for correct routing

---

## Backend Configuration (Critical)

### Why `expense.local` Failed
- `expense.local` exists only on the developer machine
- APIM runs in Azure
- Azure cannot resolve local hostnames

### Correct Backend Target
APIM must forward requests to something **Azure can reach**.

In this sprint:
- Backend = **Ingress public IP**
- Example:
  ```
  http://4.229.xxx.xxx
  ```

---

## Host‑Based Ingress Requirement

Ingress routing is defined as:

```yaml
host: expense.local
```

If APIM forwards traffic without that Host header, NGINX will not match the route.

### Solution: Force Host Header in APIM

```xml
<set-header name="Host" exists-action="override">
  <value>expense.local</value>
</set-header>
```

This allows:
- APIM → IP routing
- Ingress → host‑based matching

---

# Task 3 — Apply APIM Policies

All policies are applied at the **API level**.

### Why API‑Level Policies
- Affects only this API
- Avoids global side effects
- Easier to reason about and evolve

---

## Policy Execution Order

APIM executes policies in this order:

1. `<inbound>`
2. `<backend>`
3. `<outbound>`
4. `<on-error>`

Anything in `<inbound>` executes **before AKS is contacted**.

---

## 3.1 Header Normalization & Security Headers

### Purpose
- Reduce information leakage
- Standardize requests and responses
- Prepare for observability (Phase 5)

---

### Correlation ID (Inbound)

```xml
<set-header name="x-correlation-id" exists-action="skip">
  <value>@(context.RequestId.ToString())</value>
</set-header>
```

- Preserves client‑provided ID if present
- Otherwise generates one at the gateway
- Enables end‑to‑end tracing later

---

### Remove Unwanted Headers (Inbound)

```xml
<set-header name="X-Powered-By" exists-action="delete" />
<set-header name="Server" exists-action="delete" />
```

- Prevents platform fingerprinting
- Defense‑in‑depth best practice

---

### Security Headers (Outbound)

```xml
<set-header name="X-Content-Type-Options" exists-action="override">
  <value>nosniff</value>
</set-header>

<set-header name="Referrer-Policy" exists-action="override">
  <value>no-referrer</value>
</set-header>

<set-header name="X-Frame-Options" exists-action="override">
  <value>DENY</value>
</set-header>

<set-header name="Cache-Control" exists-action="override">
  <value>no-store</value>
</set-header>
```

- Safe defaults for APIs
- Prevent caching and client‑side attacks

---

## 3.2 Rate Limiting

### Why Rate Limiting at APIM
- Stops abusive traffic before AKS
- Protects pods and SQL
- Enforces fairness

---

### Burst + Sustained Limits

#### Burst Protection
```xml
<rate-limit-by-key calls="10" renewal-period="10"
  counter-key="@(context.Subscription?.Key ?? "anonymous")" />
```

- Max 10 requests per 10 seconds

#### Sustained Protection
```xml
<rate-limit-by-key calls="60" renewal-period="60"
  counter-key="@(context.Subscription?.Key ?? "anonymous")" />
```

- Max 60 requests per minute

---

### Counter Key Explanation

```xml
context.Subscription?.Key ?? "anonymous"
```

- Subscription key present → per‑client limits
- No subscription → shared anonymous bucket
- Ideal for development environments

---

### Expected Behavior
- Limit exceeded → HTTP **429**
- Automatically resets after renewal period
- AKS never sees rejected traffic

---

## 3.3 JWT Validation at the Edge (Task 335)

### Why Validate JWT in APIM and API

This is **defense‑in‑depth**.

| Layer | Responsibility |
|------|---------------|
| APIM | Protect infrastructure |
| API | Protect business logic |

---

### OpenID Metadata

```xml
<openid-config url="https://expensetrackerhqdev.ciamlogin.com/b19bd0da-f1e2-4781-a072-973d388c6016/v2.0/.well-known/openid-configuration" />
```

Provides:
- Issuer
- Signing keys
- Token endpoints

---

### Audience Enforcement

Extracted from a real, working access token:

```json
"aud": "66536591-2962-45c4-9be5-1b100381a561"
```

---

### JWT Validation Policy

```xml
<validate-jwt header-name="Authorization"
              failed-validation-httpcode="401"
              failed-validation-error-message="Unauthorized">
  <openid-config url="https://expensetrackerhqdev.ciamlogin.com/b19bd0da-f1e2-4781-a072-973d388c6016/v2.0/.well-known/openid-configuration" />
  <required-claims>
    <claim name="aud">
      <value>66536591-2962-45c4-9be5-1b100381a561</value>
    </claim>
  </required-claims>
</validate-jwt>
```

---

### Resulting Behavior
- No token → 401 (APIM)
- Invalid token → 401 (APIM)
- Valid token → forwarded to API
- API still validates JWT independently

---

## Final Inbound Policy (Complete)

```xml
<inbound>
  <base />

  <validate-jwt header-name="Authorization"
                failed-validation-httpcode="401"
                failed-validation-error-message="Unauthorized">
    <openid-config url="https://expensetrackerhqdev.ciamlogin.com/b19bd0da-f1e2-4781-a072-973d388c6016/v2.0/.well-known/openid-configuration" />
    <required-claims>
      <claim name="aud">
        <value>66536591-2962-45c4-9be5-1b100381a561</value>
      </claim>
    </required-claims>
  </validate-jwt>

  <rate-limit-by-key calls="10" renewal-period="10"
    counter-key="@(context.Subscription?.Key ?? "anonymous")" />

  <rate-limit-by-key calls="60" renewal-period="60"
    counter-key="@(context.Subscription?.Key ?? "anonymous")" />

  <set-header name="Host" exists-action="override">
    <value>expense.local</value>
  </set-header>

  <set-header name="x-correlation-id" exists-action="skip">
    <value>@(context.RequestId.ToString())</value>
  </set-header>

  <set-header name="X-Powered-By" exists-action="delete" />
  <set-header name="Server" exists-action="delete" />
</inbound>
```

---

## Sprint 4.4 Exit Criteria — Met

- APIM is the single public entry point
- AKS is no longer treated as public
- JWT validated at the edge
- Rate limiting enforced
- Security headers applied
- HTTPS entry point functioning
- Defense‑in‑depth architecture achieved

---

## Final Notes

This sprint represents **real platform engineering**.
Nothing here is demo‑only.
Every decision scales.

This document is intended to be reused as a **reference implementation** for future projects.

---