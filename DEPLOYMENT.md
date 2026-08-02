# Deploying UPCSG

Three services: **Supabase** (Postgres + uploaded images), **Render** (the API), and
**GitHub Pages or Vercel** (the Blazor frontend).

Do them in that order. Each needs a value from the one before it, and going out of order
means revisiting environment variables you have already set.

Nothing in this file is a secret. Every actual secret is set in a hosting dashboard and
never committed — if you find yourself pasting a password into a file in this repo, stop.

---

## Why Supabase for both

Supabase *is* Postgres, and its Storage speaks the S3 protocol, so it covers what Neon and
R2 were doing between them. For an org that hands over every year, one dashboard and one
set of credentials is worth real money in avoided confusion.

Two things to know going in:

- **Free projects pause after about a week of inactivity** and need a manual restore from
  the dashboard. Neon woke on its own; Supabase does not. Over a semester break the site
  can go down until someone clicks restore. The uptime pinger in step 2 avoids this by
  keeping traffic flowing — set it up, don't skip it.
- **Supabase Storage has real private buckets with signed URLs.** That is a direct
  improvement for the receipts problem (task #57), which R2 was also capable of but which
  is easier to reach here.

The application code names no vendor. `S3MediaStore` talks to whatever endpoint it is
given, so moving to R2, MinIO or AWS later is an environment-variable change.

---

## 0. Before you start

Decide two URLs now, because several settings reference them:

| Thing | Example | Yours |
| --- | --- | --- |
| API origin | `https://upcsg-api.onrender.com` | |
| Site origin | `https://upcsg.github.io/upcsg-web` or `https://upcsg.vercel.app` | |

If you want a custom domain, set it up first. Changing the origin later means re-editing
the Google credential, the CORS list, and `appsettings.json`.

---

## 1. Supabase

Create a project. Pick the region closest to Cebu — **Southeast Asia (Singapore)**,
`ap-southeast-1`. Save the database password it generates; you cannot see it again.

### 1a. Database

**Project Settings → Database → Connection string → .NET**. You want the **Session pooler**
(port `5432`), not the transaction pooler:

```
Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password=<password>;SSL Mode=Require
```

> Use the **session** pooler, port 5432. The transaction pooler on 6543 does not support
> prepared statements, which Npgsql uses by default — you would get intermittent
> `prepared statement "_p1" already exists` errors under load rather than a clean failure
> at startup. If you must use 6543, add `Max Auto Prepare=0;No Reset On Close=true`.

Apply the migrations from your machine:

```bash
cd UpcsgWeb.Infrastructure; $env:UPCSG_CONNECTION="<connection-string>"; dotnet ef database update
```

`UpcsgDbContextFactory` refuses to run without `UPCSG_CONNECTION`, so there is no way to
migrate localhost while believing you hit production. The `AddOfficerEmails` migration
seeds `accabildo@up.edu.ph`, which is what gives you a way into the admin pages.

The API does **not** migrate on startup, deliberately — a bad migration should not be able
to take the site down on a restart.

### 1b. Storage

**Storage → Create bucket**, twice:

1. `media` — **public**. Merch photos, event posters, officer portraits.
2. `receipts` — **private**. GCash proofs belong to individual guilders.

> ⚠️ **Receipts are not yet served privately.** Task #57 is open: the code currently writes
> receipts to the public bucket and serves them from guessable URLs, so anyone with a link
> can read someone else's payment proof. Creating the private bucket now costs nothing and
> means the fix is a config change later — but until #57 lands, treat receipt URLs as
> exposed. Decide knowingly before the first real drop.

**Project Settings → Storage → S3 access keys → New access key.** This is separate from the
anon/service keys — those are for Supabase's own REST API, not the S3 protocol.

Your values:

| Setting | Value |
| --- | --- |
| Endpoint | `https://<project-ref>.supabase.co/storage/v1/s3` |
| Region | `ap-southeast-1` (must match the project) |
| Public base URL | `https://<project-ref>.supabase.co/storage/v1/object/public/media` |

---

## 2. Render (API)

New **Web Service** pointed at this repo.

- Build: `dotnet publish UpcsgWeb.Api/UpcsgWeb.Api.csproj -c Release -o out`
- Start: `dotnet out/UpcsgWeb.Api.dll`
- Health check path: `/health` — it touches the database, so a warm process on a dead
  connection still reports unhealthy.

### Environment variables

| Key | Value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__Neon` | your Supabase session-pooler string |
| `Jwt__SigningKey` | 32+ random bytes — `openssl rand -base64 48` |
| `Google__ClientId` | from step 3 |
| `Google__RequiredHostedDomain` | `up.edu.ph` — optional, locks sign-in to UP accounts |
| `Cors__AllowedOrigins__0` | your site origin, no trailing slash |
| `Api__SelfUrl` | your API origin |
| `Media__ServiceUrl` | `https://<project-ref>.supabase.co/storage/v1/s3` |
| `Media__Region` | `ap-southeast-1` |
| `Media__AccessKeyId` | from the S3 access key |
| `Media__SecretAccessKey` | from the S3 access key |
| `Media__PublicBucket` | `media` |
| `Media__PublicBaseUrl` | `https://<project-ref>.supabase.co/storage/v1/object/public/media` |

Double underscores, not colons. The config key is still `ConnectionStrings__Neon` — the
name is historical and renaming it is a code change for no benefit.

**`ASPNETCORE_ENVIRONMENT=Production` is not cosmetic.** `Features/Dev/DevSignInEndpoint`
hands admin tokens to anyone who can reach it, and it is excluded from the endpoint
registry only when the host is not Development. Getting this wrong is a complete
authorisation bypass.

With the `Media__*` values unset the API falls back to local disk, which on Render is
**ephemeral** — every deploy silently loses every uploaded image. The API logs its chosen
provider at startup; it should say `Supabase Storage`. Check that line on the first deploy.

### Keep both services awake

Render free services sleep after ~15 minutes idle; Supabase free projects pause after ~7
days. One cron hitting the API's health endpoint solves both, because the health check
touches the database:

- [cron-job.org](https://cron-job.org) or UptimeRobot, every 10 minutes, `GET /health`.

Without it, the first visitor after a quiet spell waits ~30s for a cold start, and after a
long break the database needs a manual restore.

---

## 3. Google sign-in

Google Cloud Console → **APIs & Services → Credentials → Create OAuth client ID → Web
application**.

- **Authorised JavaScript origins:** your site origin, plus `http://localhost:5005`.
  Origins only — no paths, no trailing slashes.
- **Authorised redirect URIs:** none needed. Google Identity Services returns the token to
  the page; there is no redirect leg.

Fill in the OAuth consent screen. **Internal** is right if UP Cebu's Workspace allows it —
that alone restricts sign-in to UP accounts. Otherwise use External and set
`Google__RequiredHostedDomain`.

Set the client id in **two** places:

1. Render → `Google__ClientId` (the API checks the token's audience)
2. `UpcsgWeb.FrontEnd/wwwroot/appsettings.json` → `Google.ClientId`

The client id is **not** a secret — it is sent to every browser that loads the login page.
What protects the flow is the authorised-origins list plus the API verifying the token
server-side. There is no client *secret* in this design, which is why none is requested.

Setting `Google.ClientId` automatically swaps the development stand-in buttons for the real
Google button. There is no flag to remember.

---

## 4. Frontend

Edit `UpcsgWeb.FrontEnd/wwwroot/appsettings.json`:

```json
{
  "Api": { "BaseUrl": "https://your-api.onrender.com" },
  "Google": { "ClientId": "your-client-id.apps.googleusercontent.com" }
}
```

Publish:

```bash
dotnet publish UpcsgWeb.FrontEnd/UpcsgWeb.FrontEnd.csproj -c Release -o publish
```

The site is `publish/wwwroot`.

### If GitHub Pages

- Serve from `gh-pages` or `/docs`.
- Add an empty `.nojekyll`, or Jekyll strips `_framework/` and nothing loads at all.
- Publishing to a project page (`user.github.io/repo`) means `<base href>` in `index.html`
  must become `/repo/`, or every asset 404s.
- Copy `index.html` to `404.html`. Pages has no SPA rewrite, so `/events/<id>` is a hard
  404 without it.

### If Vercel

- Framework preset **Other**, output directory `publish/wwwroot`.
- Add a rewrite:

```json
{ "rewrites": [{ "source": "/(.*)", "destination": "/index.html" }] }
```

Vercel handles SPA routing and custom domains with less ceremony. With no reason to prefer
Pages, it is the easier of the two.

### Caching

Serve `index.html` as `Cache-Control: no-cache`. Every build re-fingerprints the runtime
files (`dotnet.<hash>.js`); a browser holding a cached `index.html` requests a hash that no
longer exists and gets a permanently blank page until a hard refresh. This is not
hypothetical — it happened during development.

---

## 5. First run

1. Open the site. Public pages should show live content. If the events page shows a month
   you did not configure, `Api.BaseUrl` is wrong or the API is asleep.
2. Check the Render startup log for the media provider line. It should read
   `Supabase Storage`; `local disk` means the `Media__*` variables did not take.
3. Sign in with `accabildo@up.edu.ph`. You should land as an officer.
4. **Admin → Officers**, add this year's ExeCom. Adding promotes an existing account
   immediately; otherwise it applies at their next sign-in.
5. Upload one image through the CMS and confirm it appears at a
   `.../storage/v1/object/public/media/...` URL.
6. Place one real order end to end — cart, checkout, GCash receipt, acknowledge, release,
   receive — before announcing a drop.

### Handover

The officer allowlist is the whole access model. When the ExeCom changes, outgoing officers
add the incoming ones and remove themselves. The last officer cannot be removed: an empty
allowlist is unrecoverable from inside the app once the development sign-in endpoint is
gone from the production build.

---

## Still open

- **Task #57 — receipts are publicly readable.** See the warning in step 1b.
- Officer quotes and bios in the database are placeholder text I generated. Replace them.
- Home, About and Events throw an unhandled exception when the API is unreachable, blanking
  the page rather than showing an error. Other pages handle it. Worth evening out, given
  free-tier cold starts.
- ~34 API endpoints still call repositories directly rather than going through MediatR.
  Cosmetic; the Orders and Officers areas show the target shape.
