# DWES1 — Containerizing the CLI

Multi-stage build: `sdk:8.0` publishes the `Presentation` project, `runtime:8.0`

## Build & run

```bash
# from IRacingLeague/
docker build -t iracing-cli:1.0 .

# -it for the interactive Spectre menu; volume for the JSON data & logs; APP_ENV; port with sv user id
docker run -it -p 8009:8009 -v "$(pwd)/data:/app/data" -v "$(pwd)/logs:/app/logs" -e APP_ENV=local iracing-cli:1.0
```

The app stores JSON under `/app/data/<APP_ENV>` (e.g. `/app/data/local`), so the
volume mount makes that data survive container restarts on the host, and changing
`-e APP_ENV=...` switches to a different data sub-directory.

## Registry

```bash
docker login
docker tag iracing-cli:1.0 <user>/iracing-cli:1.0
docker push <user>/iracing-cli:1.0
docker pull <user>/iracing-cli:1.0
docker run -it <user>/iracing-cli:1.0
```

