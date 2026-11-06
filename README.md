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

Early development. Setup instructions will be added as the code lands.

## License

[MIT](LICENSE)
