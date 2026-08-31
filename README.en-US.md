**English** · [Português](README.pt-BR.md)

# FIAP Games — Notifications API

Welcome emails and purchase confirmations. Console delivery by default; real delivery via Resend is an opt-in config switch (`Email:Provider=resend`). Owns the `notifications` Postgres schema.

## Run standalone

```bash
cp .env.example .env
docker compose up --build
```

Brings up this service plus its own Postgres and RabbitMQ. API on `localhost:8082`, Swagger at `/swagger`.

## Run as part of the system

Deployed by the [`orchestration`](https://github.com/tc2-fiap/orchestration) Helm chart alongside the other four backend services and the frontend — see [`../orchestration/README.en-US.md`](../orchestration/README.en-US.md). Reached through the shared Ingress at `/api/notifications/*` (admin-only).

## What's here

- `Domain/Notification.cs` — one row per notification sent, deduplicated by key, storing the actual provider request/response payload (real JSON even for the console channel's own synthetic record).
- `Domain/UserProjection.cs` — a local read-model (UserId → Name/Email) kept current from `UserCreatedEvent`, since `PaymentProcessedEvent`'s fixed contract carries only a `UserId` and this service needs an address to send to.
- Consumes `UserCreatedEvent` (welcome email) and `PaymentProcessedEvent` (purchase confirmation or failure notice) — both idempotent, keyed by a dedupe key.
- `GET /api/notifications?orderId=` — admin-only, one order's notifications.
- `GET /api/notifications/admin` — admin-only, paginated, filterable by `type`/`status`/`from`/`to`; every notification across every order, not just one (`../documentation/spec/notes.md` 43).

## Test

```bash
cd tests/FiapGames.Notifications.Tests && dotnet test
```

## Documentation

Full architecture, event contracts, and the project-wide decision record live in the `documentation` repo — [`github.com/tc2-fiap/documentation`](https://github.com/tc2-fiap/documentation) (or `../documentation/` if you have it cloned as a sibling) — see [`DOCUMENTATION.en-US.md`](https://github.com/tc2-fiap/documentation/blob/main/narrative/DOCUMENTATION.en-US.md) and [`instructions.md`](https://github.com/tc2-fiap/documentation/blob/main/spec/instructions.md) §4.5.
