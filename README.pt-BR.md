[English](README.en-US.md) · **Português**

# FIAP Games — Notifications API

E-mails de boas-vindas e confirmações de compra. Entrega via console por padrão; a entrega real via Resend é uma opção de configuração (`Email:Provider=resend`). Dono do schema `notifications` no Postgres.

## Rodar de forma independente

```bash
cp .env.example .env
docker compose up --build
```

Sobe este serviço mais seu próprio Postgres e RabbitMQ. API em `localhost:8082`, Swagger em `/swagger`.

## Rodar como parte do sistema

Implantado pelo chart Helm [`orchestration`](https://github.com/tc2-fiap/orchestration) junto com os outros quatro serviços de backend e o frontend — ver [`../orchestration/README.pt-BR.md`](../orchestration/README.pt-BR.md). Acessado pelo Ingress compartilhado em `/api/notifications/*` (somente admin).

## O que tem aqui

- `Domain/Notification.cs` — uma linha por notificação enviada, deduplicada por chave, armazenando o payload real de request/response do provedor (JSON real mesmo para o próprio registro sintético do canal console).
- `Domain/UserProjection.cs` — um read-model local (UserId → Name/Email) mantido atualizado a partir do `UserCreatedEvent`, já que o contrato fixo do `PaymentProcessedEvent` carrega apenas um `UserId`, e este serviço precisa de um endereço para enviar.
- Consome `UserCreatedEvent` (e-mail de boas-vindas) e `PaymentProcessedEvent` (confirmação de compra ou aviso de falha) — ambos idempotentes, controlados por uma chave de dedupe.
- `GET /api/notifications?orderId=` — somente admin, notificações de um pedido.
- `GET /api/notifications/admin` — somente admin, paginado, filtrável por `type`/`status`/`from`/`to`; toda notificação entre todos os pedidos, não só um (`../documentation/spec/notes.md` 43).

## Testar

```bash
cd tests/FiapGames.Notifications.Tests && dotnet test
```

## Documentação

A arquitetura completa, os contratos de eventos e o registro de decisões do projeto vivem no repositório `documentation` — [`github.com/tc2-fiap/documentation`](https://github.com/tc2-fiap/documentation) (ou `../documentation/` se você o tiver clonado como irmão) — ver [`ARCHITECTURE.pt-BR.md`](https://github.com/tc2-fiap/documentation/blob/main/architecture/ARCHITECTURE.pt-BR.md) e [`instructions.md`](https://github.com/tc2-fiap/documentation/blob/main/spec/instructions.md) §4.5 (em inglês).
