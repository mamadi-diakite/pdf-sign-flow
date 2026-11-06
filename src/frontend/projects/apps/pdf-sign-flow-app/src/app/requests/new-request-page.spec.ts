import { SignatureRequestDetails } from '@pdf-sign-flow/shared/core';
import { AppHarness } from '../../testing/app-harness';

const created: SignatureRequestDetails = {
  id: 'r-9',
  title: 'Office lease',
  message: null,
  status: 'Draft',
  createdAtUtc: '2026-11-06T12:00:00Z',
  updatedAtUtc: '2026-11-06T12:00:00Z',
  documents: [],
  attachments: [],
};

describe('New request page', () => {
  let app: AppHarness;

  beforeEach(async () => {
    app = new AppHarness();
    app.signIn();
    await app.open('/requests/new');
  });

  afterEach(() => app.http.verify());

  it('asks for a title before calling the API', async () => {
    await app.submit();

    expect(app.texts('psf-field-errors p')).toEqual(['Enter a title.']);
  });

  it('shows the field errors returned by the API', async () => {
    await app.type('title', 'Office lease');

    await app.submit();
    app.http
      .expectOne('/api/requests')
      .flush(
        { title: 'One or more validation errors occurred.', errors: { Message: ['Too long.'] } },
        { status: 400, statusText: 'Bad Request' },
      );
    await app.settle();

    expect(app.texts('psf-field-errors p')).toEqual(['Too long.']);
  });

  it('creates the draft without an empty message and opens it', async () => {
    await app.type('title', 'Office lease');
    await app.type('message', '   ');

    await app.submit();
    const request = app.http.expectOne({ method: 'POST', url: '/api/requests' });
    expect(request.request.body).toEqual({ title: 'Office lease', message: null });
    request.flush(created);
    await app.settle();
    app.http.expectOne('/api/requests/r-9').flush(created);
    await app.settle();

    expect(app.router.url).toBe('/requests/r-9');
    expect(app.text('h1')).toBe('Office lease');
  });

  it('sends the message when there is one', async () => {
    await app.type('title', 'Office lease');
    await app.type('message', 'Please sign before Friday.');

    await app.submit();
    const request = app.http.expectOne('/api/requests');

    expect(request.request.body).toEqual({
      title: 'Office lease',
      message: 'Please sign before Friday.',
    });
    request.flush(null, { status: 500, statusText: 'Server Error' });
    await app.settle();
    expect(app.text('[role="alert"]')).toBe('Something went wrong. Please try again.');
  });
});
