# Roadmap

This is the working plan for pdf-sign-flow. Stories are grouped by epic and tagged with the milestone they belong to.

## Scope

In scope:
- Sending PDF documents for signature, with signers in steps and signature fields placed on pages.
- Signing with a drawn or uploaded signature, confirmed by a 6-digit code.
- Tracking requests and downloading the signed documents with their audit trail.

Out of scope:
- Not a qualified or advanced electronic signature under eIDAS. It is a simple electronic signature with an audit trail. No legal claims.
- PDF only. No Word or image documents to sign.
- No email in the first version. Notifications are in-app, behind an interface, so email delivery can be added later.
- No real user data. The demo runs on fictional accounts and documents.

## Glossary

| Term | Meaning |
|------|---------|
| Sender | A user who creates signature requests |
| Signature request | A set of documents, attachments and signers sent for signature |
| Document | A PDF that must be signed |
| Attachment | A file sent with the request for information only |
| Signer | A registered user asked to sign |
| Step | A group of signers. Step 2 starts when everyone in step 1 has signed |
| Signature field | A box on a document page assigned to one signer |
| Signing link | A personal link to the signing page, delivered in a notification |
| Confirmation code | A 6-digit code delivered in a notification to confirm a signature |
| Audit trail | The list of events of a request: sent, opened, signed, completed |

## Milestones

| Milestone | Goal |
|-----------|------|
| M0 Foundation | Runnable app: accounts, demo data, upload PDFs into a draft request |
| M1 Request builder | Signers, steps, signature fields placed on pages, send |
| M2 Signing | Notifications, signing page, drawn or uploaded signature, code confirmation, PDF stamping |
| M3 Tracking and delivery | Status follow-up, reminders, cancel, final notification with signed files, audit trail |
| M4 Quality | CI, tests, accessibility, docs |

---

## EPIC 1 — Project setup

| ID | Story | Milestone |
|----|-------|-----------|
| SETUP-1 | Repository with README, LICENSE (MIT), .gitignore, .editorconfig | M0 |
| SETUP-2 | .NET solution: Api, Domain, Application, Infrastructure and Tests projects | M0 |
| SETUP-3 | SQLite + EF Core, first migration, database created on startup in dev | M0 |
| SETUP-4 | Angular workspace with the main app and shared libraries | M0 |
| SETUP-5 | GitHub Actions: build and test both API and front end on each push | M4 |

## EPIC 2 — Accounts

| ID | Story | Milestone |
|----|-------|-----------|
| AUTH-1 | As a visitor, I create an account with name, email and password | M0 |
| AUTH-2 | As a user, I log in and get a token | M0 |
| AUTH-3 | As a user, I log out, and I am sent back to login when my session expires | M0 |
| AUTH-4 | As a user, I change my password with current password, new password and confirmation | M0 |
| AUTH-5 | Demo accounts (senders and signers) are seeded with a documented demo password | M0 |

Acceptance notes:
- Passwords are hashed. Demo emails use `@example.com`.

## EPIC 3 — Documents and storage

| ID | Story | Milestone |
|----|-------|-----------|
| DOC-1 | As a sender, I upload PDFs to sign. Non-PDF files and oversized files are rejected | M0 |
| DOC-2 | As a sender, I add attachments (PDF, images, Office files) for information only | M1 |
| DOC-3 | As a sender or signer, I preview a PDF page by page in the browser | M1 |
| DOC-4 | Files are stored outside the web root behind a storage interface (local disk first) | M0 |

Acceptance notes:
- File type is checked from the content, not only the extension.

## EPIC 4 — Request builder

| ID | Story | Milestone |
|----|-------|-----------|
| REQ-1 | As a sender, I create a draft request with a title and a message for signers | M0 |
| REQ-2 | As a sender, I see my draft with its documents and attachments, and remove files | M0 |
| REQ-3 | As a sender, I add signers by email. The email must match a registered account | M1 |
| REQ-4 | As a sender, I group signers into ordered steps | M1 |
| REQ-5 | As a sender, I place signature fields on document pages and assign each to a signer | M1 |
| REQ-6 | The request cannot be sent unless every signer has at least one field | M1 |
| REQ-7 | As a sender, I send the request. Step 1 signers are notified | M1 |

## EPIC 5 — Notifications

| ID | Story | Milestone |
|----|-------|-----------|
| NOTIF-1 | Notifications go through a sender interface. The first implementation stores them in-app | M2 |
| NOTIF-2 | As a user, I see my notifications with an unread count and mark them as read | M2 |
| NOTIF-3 | Signers get a notification with their signing link | M2 |
| NOTIF-4 | Signers get their confirmation code in a notification | M2 |
| NOTIF-5 | When a step is complete, the next step signers are notified | M2 |
| NOTIF-6 | When the request is complete, sender and signers get a notification with download links for the signed documents and attachments | M3 |

Acceptance notes:
- A code sent through the same channel as the login confirms intent but adds no real security. The README says so. Email or SMS delivery is planned.

## EPIC 6 — Signing

| ID | Story | Milestone |
|----|-------|-----------|
| SIGN-1 | As a signer, I open my link while logged in. Expired, used or cancelled links show a clear message | M2 |
| SIGN-2 | As a signer, I see the documents to sign and the attachments, and open each one | M2 |
| SIGN-3 | As a signer, I draw my signature with mouse, pen or finger | M2 |
| SIGN-4 | As a signer, I upload an image of my signature instead (PNG or JPEG) | M2 |
| SIGN-5 | As a signer, I see where my signature will go before confirming | M2 |
| SIGN-6 | As a signer, I confirm with the 6-digit code. Wrong codes are limited and codes expire | M2 |
| SIGN-7 | As a signer, I decline with a reason, which stops the request | M3 |

Acceptance notes:
- Link tokens and codes are random, stored hashed, and expire.
- A link only works for the signer it was issued to.

## EPIC 7 — PDF stamping

| ID | Story | Milestone |
|----|-------|-----------|
| PDF-1 | The signature image is stamped into the PDF at each of the signer's fields | M2 |
| PDF-2 | Stamping handles page size and rotation correctly | M2 |
| PDF-3 | A SHA-256 hash of each final document is recorded | M3 |
| PDF-4 | A summary page with the audit trail is appended to each signed document | M3 |

## EPIC 8 — Tracking

| ID | Story | Milestone |
|----|-------|-----------|
| TRACK-1 | As a sender, I list my requests with their status | M3 |
| TRACK-2 | As a sender, I open a request and see each signer's status and dates | M3 |
| TRACK-3 | As a sender, I cancel a request that is not complete | M3 |
| TRACK-4 | As a sender, I send a reminder to a signer who has not signed yet | M3 |
| TRACK-5 | As a sender, I download the signed documents as a ZIP | M3 |

## EPIC 9 — Audit trail

| ID | Story | Milestone |
|----|-------|-----------|
| AUDIT-1 | Every event is recorded with date and actor: created, sent, opened, code sent, signed, declined, completed | M3 |
| AUDIT-2 | As a sender, I see the audit trail of a request | M3 |

## EPIC 10 — Quality and docs

| ID | Story | Milestone |
|----|-------|-----------|
| QA-1 | API integration tests for the full signing flow | M4 |
| QA-2 | Unit tests for key Angular components and services | M4 |
| QA-3 | End-to-end test: create, send, sign with code, download the signed file (Playwright) | M4 |
| QA-4 | Accessibility pass on the signing page | M4 |
| QA-5 | Docs: architecture, demo accounts, limits of a simple signature | M4 |

---

## Later ideas

- Email delivery (SMTP), so signers can sign without an account
- Confirmation code by SMS
- Initials on every page
- Installable PWA for signing on mobile
- Reusable request templates
- Configurable link expiry and automatic reminders
- French and English UI
