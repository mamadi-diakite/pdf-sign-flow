# pdf-sign-flow

A web app to get PDF documents signed.

The sender uploads one or more PDFs, adds signers in steps and marks where each one must sign. Each signer gets a notification with a personal link, reviews the documents and attachments, draws or uploads a signature, and confirms with a 6-digit code. When everyone has signed, all parties can download the signed documents.

## Who it is for

Individuals and small organizations who need a simple way to get documents signed, without a heavy e-signature platform.

## What it is not

This is a simple electronic signature with an audit trail. It does not claim to be an advanced or qualified electronic signature.

## Stack

- Front end: Angular workspace (app + libraries)
- Back end: ASP.NET Core Web API (C#)
- Database: SQLite with EF Core

## Status

Early development (milestone M0). What works today:

- Create an account, log in, change your password. Sessions expire after 60 minutes.
- Create a draft signature request with a title and a message for signers.
- Upload the PDFs to sign (PDF only, checked by content, 20 MB max), see them in the draft and remove them.

Signers, signing steps, field placement, signing and tracking are planned (see [ROADMAP.md](ROADMAP.md)).

## Getting started

### Prerequisites

- [.NET SDK 10.0.403](https://dotnet.microsoft.com/download) or a later 10.0 feature band
- [Node.js 24](https://nodejs.org/) with npm 11

### Run the API

```bash
cd src/backend
dotnet run --project src/PdfSignFlow.Api --launch-profile http
```

The API listens on http://localhost:5081. In Development it creates the SQLite database in `src/backend/src/PdfSignFlow.Api/App_Data/`, applies the migrations, and adds the demo accounts below when the database has no users. Uploaded files are stored in `App_Data/files`.

The access tokens are signed with a key that is never stored in the repository. In Development, a random key is generated at startup, so you need to log in again after each restart. In any other environment, set a base64 key of at least 32 bytes:

```bash
export Jwt__SigningKey="$(openssl rand -base64 32)"
```

### Run the web app

```bash
cd src/frontend
npm install
npm start
```

Open http://localhost:4200. Calls to `/api` are sent to the API on port 5081.

### Demo accounts

All demo accounts use the password `TestDemo123!`. They are fictional and only exist in Development.

| Name | Email |
|------|-------|
| Anna Smith | anna.smith@example.com |
| Lucas Bernard | lucas.bernard@example.com |
| Emma Dubois | emma.dubois@example.com |
| Noah Petit | noah.petit@example.com |
| Chloe Moreau | chloe.moreau@example.com |

To start again from an empty demo, stop the API and delete `src/backend/src/PdfSignFlow.Api/App_Data/`.

### Run the tests

Back end (with coverage, migrations and generated code excluded):

```bash
cd src/backend
dotnet test --settings coverage.runsettings --collect "XPlat Code Coverage"
```

Front end (Vitest, the run fails below 100% coverage):

```bash
cd src/frontend
npm run test:ci
```

## Project layout

```
src/backend/    ASP.NET Core solution: Domain, Application.Contracts, Application, Infrastructure, Api, and their test projects
src/frontend/   Angular workspace: projects/apps/pdf-sign-flow-app and the shared libraries in projects/libs/shared
```

## License

[MIT](LICENSE)
