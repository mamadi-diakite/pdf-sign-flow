import { SignatureRequestSummary } from '@pdf-sign-flow/shared/core';
import { AppHarness } from '../../testing/app-harness';

const summaries: SignatureRequestSummary[] = [
  {
    id: 'r-1',
    title: 'Office lease',
    status: 'Draft',
    documentCount: 2,
    updatedAtUtc: '2026-11-05T12:00:00Z',
  },
  {
    id: 'r-2',
    title: 'Supplier contract',
    status: 'Draft',
    documentCount: 1,
    updatedAtUtc: '2026-11-04T12:00:00Z',
  },
];

describe('Requests page', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => app.http.verify());

  async function openList(): Promise<void> {
    app.signIn();
    await app.open('/requests');
  }

  it('is only for signed-in users', async () => {
    await app.open('/requests');

    expect(app.router.url).toBe('/login?returnUrl=%2Frequests');
  });

  it('lists the requests with their document count', async () => {
    await openList();
    expect(app.text('[role="status"]')).toBe('Loading your requests…');

    app.http.expectOne('/api/requests').flush(summaries);
    await app.settle();

    expect(app.texts('li a > span:first-child')).toEqual(['Office lease', 'Supplier contract']);
    const details = app.texts('li a > span:last-child').map((text) => text.replace(/\s+/g, ' '));
    expect(details).toEqual([
      'Draft · 2 documents · Updated Nov 5, 2026',
      'Draft · 1 document · Updated Nov 4, 2026',
    ]);
    expect(app.element.querySelector('li a')?.getAttribute('href')).toBe('/requests/r-1');
  });

  it('invites to start when there is no request', async () => {
    await openList();
    app.http.expectOne('/api/requests').flush([]);
    await app.settle();

    expect(app.text('section > p')).toBe('You have no requests yet. Start with a new draft.');
    expect(app.text('a.button-link')).toBe('New request');
  });

  it('explains when the list cannot be loaded', async () => {
    await openList();
    app.http.expectOne('/api/requests').flush(null, { status: 500, statusText: 'Server Error' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe(
      'Your requests could not be loaded. Please try again later.',
    );
  });
});
