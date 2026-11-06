import { NgTemplateOutlet } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  LoadState,
  MAX_DOCUMENT_BYTES,
  RequestFile,
  RequestsService,
  SignatureRequestDetails,
  toFormErrors,
  withLoadState,
} from '@pdf-sign-flow/shared/core';
import { finalize, Observable } from 'rxjs';
import { formatFileSize } from './file-size';

const unexpectedError = 'Something went wrong. Please try again.';

@Component({
  selector: 'psf-request-details-page',
  imports: [NgTemplateOutlet, RouterLink],
  template: `
    <section class="space-y-8">
      <a routerLink="/requests" class="link">← My requests</a>

      @let current = state();
      @if (current.status === 'loading') {
        <p class="text-slate-600" role="status">Loading the request…</p>
      } @else if (current.status === 'ready') {
        @let request = current.value;
        <div class="space-y-2">
          <div class="flex items-center gap-3">
            <h1 class="text-2xl font-bold tracking-tight">{{ request.title }}</h1>
            <span
              class="rounded-full bg-slate-100 px-3 py-1 text-xs font-medium text-slate-700"
              data-testid="status"
              >{{ request.status }}</span
            >
          </div>
          @if (request.message) {
            <p class="whitespace-pre-line text-slate-600" data-testid="message">
              {{ request.message }}
            </p>
          }
        </div>

        @if (fileError(); as message) {
          <p class="alert-error" role="alert">{{ message }}</p>
        }

        <section class="space-y-4" aria-labelledby="documents-title">
          <h2 id="documents-title" class="text-lg font-semibold">Documents to sign</h2>
          <ul
            class="divide-y divide-slate-200 rounded-xl border border-slate-200 bg-white shadow-sm"
            data-testid="documents"
          >
            @for (file of request.documents; track file.id) {
              <ng-container
                *ngTemplateOutlet="fileRow; context: { $implicit: file, request: request }"
              />
            } @empty {
              <li class="px-6 py-4 text-slate-500">No document yet. Add the PDFs to sign.</li>
            }
          </ul>
          <div>
            <label class="form-label" for="document">Add a PDF (20 MB max)</label>
            <input
              id="document"
              class="mt-1 block text-sm"
              type="file"
              accept="application/pdf,.pdf"
              [disabled]="busy()"
              (change)="upload(request, $event)"
            />
          </div>
          @if (busy()) {
            <p class="text-sm text-slate-600" role="status">Saving your changes…</p>
          }
        </section>

        @if (request.attachments.length > 0) {
          <section class="space-y-4" aria-labelledby="attachments-title">
            <h2 id="attachments-title" class="text-lg font-semibold">Attachments</h2>
            <ul
              class="divide-y divide-slate-200 rounded-xl border border-slate-200 bg-white shadow-sm"
              data-testid="attachments"
            >
              @for (file of request.attachments; track file.id) {
                <ng-container
                  *ngTemplateOutlet="fileRow; context: { $implicit: file, request: request }"
                />
              }
            </ul>
          </section>
        }

        <ng-template #fileRow let-file let-request="request">
          <li class="flex items-center justify-between gap-4 px-6 py-3">
            <span>
              <span class="font-medium">{{ file.fileName }}</span>
              <span class="ml-2 text-sm text-slate-500">{{ size(file) }}</span>
            </span>
            <button
              type="button"
              class="text-sm text-rose-700 hover:underline disabled:opacity-60"
              [attr.aria-label]="'Remove ' + file.fileName"
              [disabled]="busy()"
              (click)="remove(request, file)"
            >
              Remove
            </button>
          </li>
        </ng-template>
      } @else if (current.status === 'missing') {
        <p class="alert-info">This request does not exist or is not yours.</p>
      } @else {
        <p class="alert-error" role="alert">
          The request could not be loaded. Please try again later.
        </p>
      }
    </section>
  `,
})
export class RequestDetailsPage {
  private readonly requests = inject(RequestsService);

  protected readonly state = signal<LoadState<SignatureRequestDetails>>({ status: 'loading' });
  protected readonly busy = signal(false);
  protected readonly fileError = signal<string | null>(null);

  constructor() {
    const id = inject(ActivatedRoute).snapshot.paramMap.get('id')!;
    withLoadState(this.requests.get(id)).subscribe((state) => this.state.set(state));
  }

  protected size(file: RequestFile): string {
    return formatFileSize(file.sizeBytes);
  }

  protected upload(request: SignatureRequestDetails, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) {
      return;
    }

    const problem = checkDocument(file);
    if (problem) {
      this.fileError.set(problem);
      return;
    }

    this.run(this.requests.uploadDocument(request.id, file), (document) => ({
      ...request,
      documents: [...request.documents, document],
    }));
  }

  protected remove(request: SignatureRequestDetails, file: RequestFile): void {
    this.run(this.requests.removeFile(request.id, file.id), () => ({
      ...request,
      documents: request.documents.filter((document) => document.id !== file.id),
      attachments: request.attachments.filter((attachment) => attachment.id !== file.id),
    }));
  }

  /** One change at a time, so each answer applies to the request the user saw. */
  private run<T>(call: Observable<T>, apply: (value: T) => SignatureRequestDetails): void {
    this.busy.set(true);
    this.fileError.set(null);
    call.pipe(finalize(() => this.busy.set(false))).subscribe({
      next: (value) => this.state.set({ status: 'ready', value: apply(value) }),
      error: (error: unknown) => {
        const errors = toFormErrors(error);
        this.fileError.set(errors.fields['file']?.[0] ?? errors.form ?? unexpectedError);
      },
    });
  }
}

function checkDocument(file: File): string | null {
  if (!file.name.toLowerCase().endsWith('.pdf')) {
    return 'Only PDF files can be signed.';
  }

  return file.size > MAX_DOCUMENT_BYTES ? 'The file is larger than 20 MB.' : null;
}
