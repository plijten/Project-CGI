# Project-CGI – Docentenportaal voor GitHub push-notificaties en CGI-gesprekken

Deze repository bevat een pragmatische ASP.NET Core MVC-opzet met een fase 1 MVP + fase 2 uitbreidingen.

## Architectuur

- `src/Web`: MVC controllers, views en endpoint `/webhooks/github`
- `src/Application`: use-cases/services (vraaggenerator, relevantieregels)
- `src/Domain`: entiteiten, enums, domeinmodel
- `src/Infrastructure`: EF Core context, webhook signature-validatie, pushverwerking, notificaties
- `tests/Unit`: unit tests voor kernlogica
- `tests/Integration`: contract-/flow tests

## Configuratie

1. Stel de SQL Server connectiestring in `src/Web/appsettings.json` in.
2. Stel `GitHub:WebhookSecret` in.
3. Maak de database aan met EF Core migraties (nog toe te voegen in runtime-omgeving met .NET SDK).

## Fase 1 functionaliteit

- Veilige webhook-ingang op `POST /webhooks/github`
- Validatie van `X-Hub-Signature-256`
- Idempotentie via unieke `DeliveryId`
- Opslag van push-event + commits + changed files
- Automatisch aanmaken van CGI-sessie + basisvragen
- Dashboard, push-detail en CGI-uitkomstformulier
- CRUD-beheer voor students, teachers en repositories

## Fase 2 functionaliteit

- `RuleProfile` met branch/filterdrempels
- Relevantiecheck en prioritering
- Contextspecifieke vraaggeneratie op basis van bestandspaden
- Notificatielogging via `NotificationLog`
- Filterhaakjes in dashboardcontroller voor status

## Webhook testdelivery

Gebruik in GitHub webhook-instellingen:
- Content type: `application/json`
- Secret: zelfde waarde als `GitHub:WebhookSecret`
- Event: `push`
- URL: `https://<host>/webhooks/github`

## Belangrijk

In deze omgeving is de .NET SDK niet beschikbaar; compileer- en runtimechecks moeten worden uitgevoerd in een omgeving met `dotnet`.
