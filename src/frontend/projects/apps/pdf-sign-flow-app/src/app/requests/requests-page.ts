import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { RequestsService, withLoadState } from '@pdf-sign-flow/shared/core';

@Component({
  selector: 'psf-requests-page',
  imports: [DatePipe, RouterLink],
  template: `
    <section class="space-y-6">
      <div class="flex items-center justify-between gap-4">
        <h1 class="text-2xl font-bold tracking-tight">My requests</h1>
        <a routerLink="/requests/new" class="button-link">New request</a>
      </div>

      @let current = state();
      @if (current.status === 'loading') {
        <p class="text-slate-600" role="status">Loading your requests…</p>
      } @else if (current.status === 'ready') {
        @if (current.value.length === 0) {
          <p class="text-slate-600">You have no requests yet. Start with a new draft.</p>
        } @else {
          <ul
            class="divide-y divide-slate-200 rounded-xl border border-slate-200 bg-white shadow-sm"
          >
            @for (request of current.value; track request.id) {
              <li>
                <a
                  [routerLink]="['/requests', request.id]"
                  class="flex flex-col gap-1 px-6 py-4 hover:bg-slate-50 sm:flex-row sm:items-center sm:justify-between"
                >
                  <span class="font-medium">{{ request.title }}</span>
                  <span class="text-sm text-slate-500">
                    {{ request.status }} ·
                    {{
                      request.documentCount === 1
                        ? '1 document'
                        : request.documentCount + ' documents'
                    }}
                    · Updated {{ request.updatedAtUtc | date: 'mediumDate' }}
                  </span>
                </a>
              </li>
            }
          </ul>
        }
      } @else {
        <p class="alert-error" role="alert">
          Your requests could not be loaded. Please try again later.
        </p>
      }
    </section>
  `,
})
export class RequestsPage {
  protected readonly state = toSignal(withLoadState(inject(RequestsService).list()), {
    requireSync: true,
  });
}
