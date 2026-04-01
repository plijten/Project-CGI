# Project-CGI

Docentenportaal voor GitHub push-notificaties, CGI-beoordeling, studentreflectie en AI-ondersteunde code review.

## Inhoud

- [Overzicht](#overzicht)
- [Architectuur](#architectuur)
- [Benodigdheden](#benodigdheden)
- [Installatie](#installatie)
- [Configuratie](#configuratie)
- [Database en migraties](#database-en-migraties)
- [Applicatie starten](#applicatie-starten)
- [Eerste gebruiker aanmaken](#eerste-gebruiker-aanmaken)
- [GitHub webhook instellen](#github-webhook-instellen)
- [Belangrijke endpoints](#belangrijke-endpoints)
- [Werkwijze in de applicatie](#werkwijze-in-de-applicatie)
- [AI code review en reflectie](#ai-code-review-en-reflectie)
- [Testen](#testen)
- [Beveiligingsadvies](#beveiligingsadvies)

## Overzicht

De applicatie verwerkt `push`-events uit GitHub en zet die om naar CGI-sessies. Daarbij worden:

- pushes opgeslagen in de database
- repositories gekoppeld aan studenten en docenten
- CGI-vragen en reflectievragen gegenereerd
- AI-code review uitgevoerd wanneer een OpenAI key beschikbaar is
- studentantwoorden geanalyseerd voor docentinzicht
- audit logging bijgehouden van acties in het portaal

## Architectuur

- `src/Web`: webapplicatie, controllers, views, authenticatie en UI
- `src/Application`: interfaces, DTO's en applicatielogica
- `src/Domain`: entiteiten en enums
- `src/Infrastructure`: EF Core, persistence, webhookverwerking en OpenAI-integratie
- `tests/Unit`: unit tests
- `tests/Integration`: integratietests

## Benodigdheden

Voor lokale installatie is nodig:

- `.NET 8 SDK`
- `SQL Server` of `SQL Server LocalDB`
- toegang tot een GitHub repository waarop je een webhook kunt instellen
- optioneel: een `OpenAI API key`
- optioneel: `ngrok` of een andere tunnel voor lokale webhook-tests vanuit GitHub

## Installatie

1. Clone de repository.
2. Open de solution of map in Visual Studio of via CLI.
3. Controleer of de connection string in `src/Web/appsettings.json` naar een beschikbare SQL Server wijst.
4. Stel de benodigde secrets in.
5. Maak de database aan via EF Core migraties.
6. Start de webapplicatie.

## Configuratie

De applicatie leest standaard uit `src/Web/appsettings.json`.

Voorbeeldconfiguratie:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=DocentenportaalCgi;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "GitHub": {
    "WebhookSecret": "vervang-dit-door-een-eigen-sterke-secret"
  },
  "OpenAI": {
    "ApiKey": "",
    "Model": "gpt-4o-mini",
    "BaseUrl": "https://api.openai.com/v1/"
  }
}
```

### Uitleg configuratie

- `ConnectionStrings:DefaultConnection`: SQL Server verbinding
- `GitHub:WebhookSecret`: secret die zowel in GitHub als in de app moet staan
- `OpenAI:ApiKey`: optioneel, voor automatische code review en analyse van reflectie-antwoorden
- `OpenAI:Model`: OpenAI modelnaam
- `OpenAI:BaseUrl`: OpenAI API basis-URL

## Database en migraties

De migraties staan in `src/Infrastructure/Migrations`.

### Database aanmaken of updaten

Voer uit vanaf de root van het project:

```powershell
dotnet ef database update --project "src/Infrastructure/Infrastructure.csproj"
```

### Nieuwe migratie toevoegen

```powershell
dotnet ef migrations add <NaamVanDeMigratie> --project "src/Infrastructure/Infrastructure.csproj"
```

Voorbeelden van bestaande migraties:

- `InitialCreate`
- `AddAuthAndAudit`
- `AddAiReviewAndReflection`

## Applicatie starten

Via Visual Studio:

- stel `src/Web` in als startup project
- start de applicatie met `https`

Via CLI:

```powershell
dotnet run --project "src/Web/Web.csproj"
```

Lokale development URLs staan in `src/Web/Properties/launchSettings.json`.

Standaard is de app bereikbaar op:

- `https://localhost:64868`
- `http://localhost:64869`

## Eerste gebruiker aanmaken

Omdat beheer achter login zit, moet eerst een docentaccount worden aangemaakt.

1. Start de applicatie.
2. Open `/Auth/BootstrapTeacher`.
3. Maak het eerste docentaccount aan.
4. Log daarna in via `/Auth/Login`.

Na inloggen kan de docent:

- studenten aanmaken
- docenten aanmaken
- repositories registreren
- repositories koppelen aan student en docent

## GitHub webhook instellen

### Webhook URL

De applicatie luistert op:

- `POST /webhooks/github`

Lokaal wordt dat bijvoorbeeld:

- `https://localhost:64868/webhooks/github`

GitHub kan je lokale machine niet direct bereiken. Gebruik daarom voor lokale tests een publieke tunnel, bijvoorbeeld `ngrok`:

```powershell
ngrok http https://localhost:64868
```

Voorbeeld publieke URL:

- `https://abc123.ngrok-free.app/webhooks/github`

### Instellingen in GitHub

Ga in de repository naar `Settings` -> `Webhooks` -> `Add webhook` en vul in:

- `Payload URL`: `https://<jouw-host>/webhooks/github`
- `Content type`: `application/json`
- `Secret`: exact dezelfde waarde als `GitHub:WebhookSecret`
- `Which events?`: `Just the push event`
- `Active`: aan

### Wat moet in het repository-scherm van de applicatie

Voor iedere GitHub repository die webhook-events naar deze app stuurt, moet in `Admin/Repositories` een repository worden geregistreerd.

Velden:

- `GitHubRepoId`: het numerieke GitHub repository ID
- `Owner`: GitHub owner of organisatie
- `Name`: repositorynaam
- `DefaultBranch`: meestal `main`
- `IsActive`: aanzetten

Daarna moet ook een koppeling worden gemaakt tussen:

- repository
- student
- docent
- opdrachtnaam

### GitHub repository ID opzoeken

Voorbeeld via browser of API:

```text
https://api.github.com/repos/<owner>/<repo>
```

Zoek in de JSON naar het veld `id`.

Voorbeeld via PowerShell:

```powershell
Invoke-RestMethod https://api.github.com/repos/plijten/Endurix.cc.api | Select-Object id, full_name
```

## Belangrijke endpoints

### Publieke endpoints

- `POST /webhooks/github`
  - ontvangt GitHub `push`-events
  - valideert `X-Hub-Signature-256`

### Authenticatie

- `GET /Auth/Login`
- `POST /Auth/Login`
- `POST /Auth/Logout`
- `GET /Auth/BootstrapTeacher`
- `POST /Auth/BootstrapTeacher`

### Dashboard en beheer

- `GET /`
- `GET /Dashboard/Index`
- `GET /Admin/Students`
- `GET /Admin/Teachers`
- `GET /Admin/Repositories`
- `POST /Admin/AddStudent`
- `POST /Admin/AddTeacher`
- `POST /Admin/AddRepository`
- `POST /Admin/AddEnrollment`

### CGI

- `GET /cgi/{sessionId}`
  - toont CGI, commits, AI-review en reflectie
- `POST /cgi/{sessionId}`
  - docent slaat beoordeling op
- `POST /cgi/{sessionId}/reflect`
  - student slaat reflectie-antwoorden op

### Push details

- `GET /pushes/{id}`

## Werkwijze in de applicatie

1. Maak een docentaccount aan via bootstrap.
2. Log in als docent.
3. Voeg studenten en eventueel extra docenten toe.
4. Voeg repositories toe in `Admin/Repositories`.
5. Koppel per repository een student en docent.
6. Stel de webhook in op GitHub.
7. Doe een push naar de gekoppelde repository.
8. De webhook maakt automatisch een `PushEvent` en een `CgiSession` aan.
9. De student logt in en beantwoordt reflectievragen.
10. De docent beoordeelt de CGI en zet die op afgerond.

## AI code review en reflectie

Als `OpenAI:ApiKey` is ingesteld, gebeurt bij een binnenkomende push automatisch het volgende:

- samenvatting van de wijziging genereren
- docentinzicht genereren
- voorstel voor beoordeling genereren
- risico-signalen genereren
- extra reflectievragen genereren

Wanneer de student antwoorden invult:

- worden de antwoorden geanalyseerd
- wordt ingeschat of antwoorden inhoudelijk of verdacht zijn
- krijgt de docent extra signalen bij mogelijk onzin- of ontwijkgedrag

Als geen OpenAI key is ingesteld, gebruikt de applicatie fallback-logica zodat de workflow blijft werken.

## Testen

Unit en integration tests draaien met:

```powershell
dotnet test
```

## Beveiligingsadvies

- commit geen echte secrets naar Git
- zet `GitHub:WebhookSecret` en `OpenAI:ApiKey` bij voorkeur in environment variables of user secrets
- roteer direct een key wanneer die per ongeluk in broncode of screenshots terechtkomt
- gebruik in productie altijd `https`
- beperk webhook-events tot alleen `push`

## Opmerking

De applicatie gebruikt controllers en views binnen ASP.NET Core. De CGI-workflow, login, audit logging en webhookverwerking zijn volledig afhankelijk van een correct geconfigureerde database en repository-koppelingen.
