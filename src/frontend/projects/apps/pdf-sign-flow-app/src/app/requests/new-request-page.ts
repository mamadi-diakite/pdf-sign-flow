import { Component, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { FormSubmission, RequestsService } from '@pdf-sign-flow/shared/core';
import { FieldErrors } from '@pdf-sign-flow/shared/ui';

@Component({
  selector: 'psf-new-request-page',
  imports: [ReactiveFormsModule, RouterLink, FieldErrors],
  template: `
    <form class="form-card max-w-xl" [formGroup]="form" (ngSubmit)="create()" novalidate>
      <div class="space-y-2">
        <h1 class="text-2xl font-bold tracking-tight">New request</h1>
        <p class="text-slate-600">
          Give your request a title. You add the PDFs to sign on the next screen.
        </p>
      </div>

      @if (submission.errors().form; as message) {
        <p class="alert-error" role="alert">{{ message }}</p>
      }

      <div>
        <label class="form-label" for="title">Title</label>
        <input id="title" class="form-input" type="text" formControlName="title" maxlength="200" />
        <psf-field-errors [messages]="submission.messagesFor('title')" />
      </div>

      <div>
        <label class="form-label" for="message">Message for signers (optional)</label>
        <textarea
          id="message"
          class="form-input"
          rows="4"
          formControlName="message"
          maxlength="2000"
        ></textarea>
        <psf-field-errors [messages]="submission.messagesFor('message')" />
      </div>

      <button class="button-primary" type="submit" [disabled]="submission.pending()">
        Create draft
      </button>
      <p class="text-center text-sm">
        <a routerLink="/requests" class="link">Back to my requests</a>
      </p>
    </form>
  `,
})
export class NewRequestPage {
  private readonly requests = inject(RequestsService);
  private readonly router = inject(Router);

  protected readonly form = inject(NonNullableFormBuilder).group({
    title: ['', Validators.required],
    message: [''],
  });
  protected readonly submission = new FormSubmission(this.form, { title: 'Enter a title.' });

  protected create(): void {
    const { title, message } = this.form.getRawValue();
    this.submission.submit(
      () => this.requests.create({ title, message: message.trim() || null }),
      (request) => void this.router.navigate(['/requests', request.id]),
    );
  }
}
