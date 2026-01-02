# How to Get an Access Token in Postman (Entra External ID)

This guide shows how to generate an **OAuth2 access token** in Postman for calling the secured ExpenseTracker API.

You will use the **ExpenseTracker Postman** app registration (public/native client) and the **Authorization Code + PKCE** flow.

---

## What this token is used for

- You paste the **access token** into your Postman collection variable `access_token`
- Postman then sends:
  - `Authorization: Bearer <access_token>`
- Your API controller(s) with `[Authorize]` will return:
  - **401** if you have no/invalid token
  - **200** when the token is valid

---

## One-time prerequisites (already done, but good to know)

### Postman app registration (why it exists)
We created a **separate app registration** specifically for Postman because SPA app types can cause token redemption issues in Postman.

This Postman client is a **public client** (no secret) that supports PKCE.

Where it lives:
- Microsoft Entra (External ID tenant) → App registrations → **ExpenseTracker Postman**

---

## Postman OAuth2 configuration (values to enter)

In Postman, open the request you want to test (or your collection) and go to:

**Authorization tab → Type: OAuth 2.0**

Then click **Get New Access Token** and set these values.

### ✅ Values (copy/paste)

**Grant Type**
- `Authorization Code (With PKCE)`

**Callback URL**
- `https://oauth.pstmn.io/v1/browser-callback`

**Auth URL**
- `https://expensetrackerhqdev.ciamlogin.com/b19bd0da-f1e2-4781-a072-973d388c6016/oauth2/v2.0/authorize`

**Access Token URL**
- `https://expensetrackerhqdev.ciamlogin.com/b19bd0da-f1e2-4781-a072-973d388c6016/oauth2/v2.0/token`

**Client ID**
- `82fefff4-cbc4-41bd-8ab1-0405e0e6a162`

**Client Secret**
- *(leave blank — this is a public client)*

**Scope**
- `openid profile email api://66536591-2962-45c4-9be5-1b100381a561/access_as_user`

**PKCE**
- ✅ ON (required)

**Client Authentication**
- Use the default Postman option (no secret). If Postman asks:
  - “Send client credentials in body” is OK (it still sends only client_id for public clients)

---

## Steps to generate the token (every time you need a new one)

1. In Postman, go to **Authorization → OAuth 2.0**
2. Click **Get New Access Token**
3. Confirm the fields match the “Values” section above
4. Click **Request Token**
5. A browser window opens:
   - Sign in with your External ID user
   - Complete the flow
6. Postman returns with an access token
7. Click **Use Token**
8. Copy the **access_token** (optional but recommended)

---

## Put the token into your collection variables

Your collection is configured to use:

- `Authorization: Bearer {{access_token}}`

So to make all requests work:

1. Open the collection → **Variables**
2. Set:
   - `access_token` = *(paste the new access token here)*
3. Save

Now every request in the collection will authenticate automatically.

---

## Quick validation checklist

### If the API returns **200**
✅ Token is valid and accepted by the API

### If the API returns **401 Unauthorized**
Common causes:
- You pasted the **ID token** instead of the **access token**
- Token expired (just request a new one)
- Audience/authority mismatch (rare if your Helm values didn’t change)

### If the API returns **403 Forbidden**
Meaning:
- Token is valid, but you don’t have the required scope/claim
- Confirm your token contains:
  - `scp` includes `access_as_user`
  - `aud` matches the API app client id

---

## Reference: What these values mean (quick)

- **Auth URL / Token URL**: Entra External ID OAuth endpoints for your tenant
- **Client ID**: the Postman testing app registration id (public client)
- **Scope**: includes OIDC basics (`openid profile email`) plus your API permission scope:
  - `api://<API_APP_CLIENT_ID>/access_as_user`
- **PKCE**: security mechanism required for public clients (no client secret)

---