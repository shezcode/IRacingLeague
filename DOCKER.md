# DWES1 — Containerizing the CLI (Step 11)

Multi-stage build: `sdk:8.0` publishes the `Presentation` project, `runtime:8.0`
(not `aspnet` — this is a console app) runs the published dll.

## Build & run

```bash
# from IRacingLeague/
docker build -t iracing-cli:1.0 .

# -it for the interactive Spectre menu; volume for the JSON data; APP_ENV; port per the brief
docker run -it -p 8009:8009 -v "$(pwd)/data:/app/data" -e APP_ENV=local iracing-cli:1.0
```

The app stores JSON under `/app/data/<APP_ENV>` (e.g. `/app/data/local`), so the
volume mount makes that data survive container restarts on the host, and changing
`-e APP_ENV=...` switches to a different data sub-directory.

## Registry round-trip

```bash
docker login
docker tag iracing-cli:1.0 <user>/iracing-cli:1.0
docker push <user>/iracing-cli:1.0
docker pull <user>/iracing-cli:1.0          # on a clean machine
docker run -it <user>/iracing-cli:1.0
```

In this build environment the round-trip was verified against a **local registry**
(`registry:2` on `localhost:5000`) because pushing to a public registry needs
personal credentials — the mechanics (tag → push → remove local → pull fresh →
run) are identical. Substitute your Docker Hub user above to publish for real.

## Honest caveat (for the defence)

A pure console app **binds no network socket** — nothing listens on `8009`. The
`EXPOSE 8009` line and the `-p 8009:8009` flag are included only to satisfy the
brief's literal requirement. The genuinely functional cross-cutting bits are the
**volume** (JSON persistence to the host) and the **`APP_ENV` env var**.
