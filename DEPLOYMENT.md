# Deploying UPCSG

Four pieces: **Neon** (Postgres), **Render** (the API), **Cloudflare R2** (uploaded
images), and **GitHub Pages or Vercel** (the Blazor frontend).

Do them in that order. Each one needs a value from the one before it, and doing them out
of order means going back to edit environment variables you have already set.

Nothing in this file is a secret. Every actual secret is set in a hosting dashboard and
never committed — if you find yourself pasting a password into a file in this repo, stop.

---

## 0. Before you start

Decide two URLs now, because several settings reference them and changing them later
means revisiting every step:

| Thing | Example | Yours |
| --- | --- | --- |
| API origin | `https://upcsg-api.onrender.com` | |
| Site origin | `https://upcsg.github.io/upcsg-web` or `https://upcsg.vercel.app` | |

If you plan to use a custom domain, set it up first and use it here. Swapping the origin
afterwards means re-editing the Google credential, the CORS list, and `appsettings.json`.

---

## 1. Neon (database)

You already have a Neon project — this is the one piece that is live.

1. **Rotate the password.** The current one has been on this machine in plain text
   throughout development and has been read by tooling more than once. Neon dashboard →
   your project → **Roles** → reset the password.
2. Copy the new **pooled** connection string. Pooled, not direct: Render's free instances
   sleep and reconnect often, and the pooler is what stops that exhausting connections.
3. Keep it somewhere safe for step 2. It is not going into this repo.

Connection strings look like:

```
Host=ep-xxx-pooler.region.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=...;SSL Mode=Require
```

### Applying migrations

The API does **not** migrate on startup — deliberately, so a bad migration cannot take
the site down on a restart. Apply them from your machine, pointing at Neon:

```bash
cd UpcsgWeb.Infrastructure && UPCSG_CONNECTION="<neon-connection-string>" dotnet ef database update
```

On Windows PowerShell:

```bash
cd UpcsgWeb.Infrastructure; $env:UPCSG_CONNECTION="<neon-connection-string>"; dotnet ef database update
```

`UpcsgDbContextFactory` refuses to run without `UPCSG_CONNECTION`, so there is no way to
accidentally migrate localhost while believing you hit production.

The `AddOfficerEmails` migration seeds `accabildo@up.edu.ph` as the founding officer. That
is what gives you a way into the admin pages on a fresh database.

---

## 2. Render (API)

New **Web Service**, pointed at this repo.

- **Runtime:** Docker, or .NET if you add a Dockerfile-free build. Simplest working setup:
  - Build: `dotnet publish UpcsgWeb.Api/UpcsgWeb.Api.csproj -c Release -o out`
  - Start: `dotnet out/UpcsgWeb.Api.dll`
- **Health check path:** `/health` — it touches the database, so a warm process on a dead
  connection still reports unhealthy.

### Environment variables

| Key | Value | Notes |
| --- | --- | --- |
| `ConnectionStrings__Neon` | your pooled Neon string | double underscore, not a colon |
| `Jwt__SigningKey` | 32+ random bytes | `openssl rand -base64 48`. Not reused from anywhere. |
| `Google__ClientId` | from step 4 | the API verifies tokens were issued for *your* client |
| `Google__RequiredHostedDomain` | `up.edu.ph` | optional but recommended — locks sign-in to UP accounts |
| `Cors__AllowedOrigins__0` | your site origin | no trailing slash |
| `Api__SelfUrl` | your API origin | used to build media URLs |
| `ASPNETCORE_ENVIRONMENT` | `Production` | **required** |

`ASPNETCORE_ENVIRONMENT=Production` is not cosmetic. `Features/Dev/DevSignInEndpoint`
hands out admin tokens to anyone who can reach it, and it is excluded from the endpoint
registry only when the host is not Development. Getting this wrong is a total
authorisation bypass.

### The free tier sleeps

Render's free web services spin down after inactivity and take ~30s to wake. The first
visitor after a quiet period will see the site load with seed data and the cart report an
error. Options: accept it, add an uptime pinger against `/health`, or pay for the
always-on tier. For a student org site, accepting it is reasonable — just know that is
what you are seeing.

---

## 3. Cloudflare R2 (uploaded images)

You wrote "R1" — the product is **R2**. Two buckets, because they have different rules:

1. **Public bucket** — merch photos, event posters, officer portraits. These are meant to
   be seen. Enable public access and note the public URL.
2. **Private bucket** — GCash receipts. These are proof of payment belonging to
   individual guilders and must **not** be publicly readable.

> ⚠️ **Receipts are currently served from public, guessable URLs.** That is open task #57
> and it is not fixed. Anyone with a receipt URL can read someone else's payment proof,
> and the URLs are predictable enough to enumerate. Either finish #57 (private bucket plus
> short-lived presigned GETs) before real orders go through, or accept that risk knowingly
> for the first drop. Do not let this one slide silently.

Create an **R2 API token** scoped to those buckets only, then set on Render:

| Key | Value |
| --- | --- |
| `Media__AccountId` | Cloudflare account id |
| `Media__AccessKeyId` | from the R2 token |
| `Media__SecretAccessKey` | from the R2 token |
| `Media__PublicBucket` | public bucket name |
| `Media__PublicBaseUrl` | the bucket's public URL |

With these unset the API falls back to local disk, which on Render is **ephemeral** — every
deploy silently loses every uploaded image. The API logs which provider it chose at
startup; check that line on your first deploy.

---

## 4. Google sign-in

Google Cloud Console → **APIs & Services → Credentials → Create OAuth client ID → Web
application**.

- **Authorised JavaScript origins:** your site origin, plus `http://localhost:5005` for
  local work. Origins only — no paths, no trailing slash.
- **Authorised redirect URIs:** not needed. Google Identity Services returns the token to
  the page; there is no redirect leg in this flow.

You will also need to fill in the OAuth consent screen. **Internal** is the right choice if
UP Cebu's Workspace allows it — that alone restricts sign-in to UP accounts. Otherwise use
External and rely on `Google__RequiredHostedDomain`.

Then set the client id in **two** places:

1. Render: `Google__ClientId` (the API verifies the token's audience matches)
2. `UpcsgWeb.FrontEnd/wwwroot/appsettings.json` → `Google.ClientId`

The client id is **not** a secret — it is sent to every browser that loads the login page.
What protects the flow is the authorised-origins list and the API verifying the token
server-side. There is no client *secret* in this design, which is why none is asked for.

The moment `Google.ClientId` is set, the login page swaps the development stand-in buttons
for the real Google button automatically. There is no flag to remember.

---

## 5. Frontend (GitHub Pages or Vercel)

Before publishing, edit `UpcsgWeb.FrontEnd/wwwroot/appsettings.json`:

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
- Add an empty `.nojekyll` file, or Jekyll strips `_framework/` and nothing loads at all.
- If publishing to a project page (`user.github.io/repo`), the `<base href>` in
  `index.html` must become `/repo/` — otherwise every asset 404s.
- Copy `index.html` to `404.html`. Pages has no SPA rewrite, so a deep link like
  `/events/<id>` returns a hard 404 without it.

### If Vercel

- Framework preset: **Other**. Output directory: `publish/wwwroot`.
- Add a rewrite so client-side routes resolve:

```json
{ "rewrites": [{ "source": "/(.*)", "destination": "/index.html" }] }
```

Vercel handles SPA routing and custom domains with less ceremony than Pages. If you have
no reason to prefer Pages, Vercel is the easier of the two here.

### Caching

Serve `index.html` with `Cache-Control: no-cache`. Every build re-fingerprints the .NET
runtime files (`dotnet.<hash>.js`), and a browser holding a cached `index.html` will ask
for a hash that no longer exists — giving a returning visitor a permanently blank page
that only a hard refresh fixes. This is not hypothetical; it happened during development.

---

## 6. First run

1. Open the site. Public pages should render live content, not seed data — if the events
   page shows a month you did not configure, `Api.BaseUrl` is wrong or the API is asleep.
2. Sign in with `accabildo@up.edu.ph`. You should land as an officer, because that address
   is seeded into the allowlist.
3. Go to **Admin → Officers** and add this year's ExeCom. Adding an address promotes an
   existing account immediately; otherwise it takes effect at their next sign-in.
4. Check the API's startup log for the media provider line. If it says local disk, the R2
   variables are not set and uploads will not survive the next deploy.
5. Place one real order end to end — cart, checkout, GCash receipt, acknowledge, release,
   receive — before announcing a drop.

### Handover

The officer allowlist is the whole access model. When the ExeCom changes, the outgoing
officers add the incoming ones and remove themselves. The last remaining officer cannot be
removed — the API refuses, because an empty allowlist is unrecoverable from inside the app
once the development sign-in endpoint is gone.

---

## Still open before this is production-ready

- **Task #57 — receipts are publicly readable.** See the warning in step 3.
- **Neon password rotation** (step 1). Do it before the first real order.
- Officer quotes and bios in the database are placeholder text I generated. Replace them.
- Home, About and Events throw an unhandled exception if the API is unreachable, blanking
  the page instead of showing an error. The other pages handle it. Worth evening out,
  especially given Render's free tier sleeps.
