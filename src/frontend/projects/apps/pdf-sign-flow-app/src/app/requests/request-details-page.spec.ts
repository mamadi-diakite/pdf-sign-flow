import { RequestFile, SignatureRequestDetails } from '@pdf-sign-flow/shared/core';
import { AppHarness } from '../../testing/app-harness';

function pdfFile(id: string, fileName: string, sizeBytes: number): RequestFile {
  return {
    id,
    fileName,
    contentType: 'application/pdf',
    sizeBytes,
    position: 1,
    uploadedAtUtc: '2026-11-06T12:00:00Z',
  };
}

const draft: SignatureRequestDetails = {
  id: 'r-1',
  title: 'Office lease',
  message: 'Please sign before Friday.',
  status: 'Draft',
  createdAtUtc: '2026-11-06T12:00:00Z',
  updatedAtUtc: '2026-11-06T12:00:00Z',
  documents: [pdfFile('f-1', 'lease.pdf', 12_800)],
  attachments: [pdfFile('f-2', 'floor-plan.pdf', 2 * 1024 * 1024)],
};

const newPdf = (name = 'annex.pdf') => new File(['%PDF-1.7'], name, { type: 'application/pdf' });

describe('Request details page', () => {
  let app: AppHarness;

  beforeEach(() => {
    app = new AppHarness();
    app.signIn();
  });

  afterEach(() => app.http.verify());

  async function openDraft(request: SignatureRequestDetails = draft): Promise<void> {
    await app.open('/requests/r-1');
    expect(app.text('[role="status"]')).toBe('Loading the request…');
    app.http.expectOne('/api/requests/r-1').flush(request);
    await app.settle();
  }

  function rows(list: string): string[] {
    return [...app.element.querySelectorAll(`[data-testid="${list}"] li > span`)].map((row) =>
      [...row.children].map((part) => part.textContent!.trim()).join(' '),
    );
  }

  it('shows the draft with its documents and attachments', async () => {
    await openDraft();

    expect(app.text('h1')).toBe('Office lease');
    expect(app.text('[data-testid="status"]')).toBe('Draft');
    expect(app.text('[data-testid="message"]')).toBe('Please sign before Friday.');
    expect(rows('documents')).toEqual(['lease.pdf 13 KB']);
    expect(rows('attachments')).toEqual(['floor-plan.pdf 2.0 MB']);
  });

  it('shows an empty draft without message nor attachments', async () => {
    await openDraft({ ...draft, message: null, documents: [], attachments: [] });

    expect(app.element.querySelector('[data-testid="message"]')).toBeNull();
    expect(app.text('[data-testid="documents"] li')).toBe('No document yet. Add the PDFs to sign.');
    expect(app.element.querySelector('[data-testid="attachments"]')).toBeNull();
  });

  it('says when the request does not exist', async () => {
    await app.open('/requests/r-404');
    app.http.expectOne('/api/requests/r-404').flush(null, { status: 404, statusText: 'Not Found' });
    await app.settle();

    expect(app.text('.alert-info')).toBe('This request does not exist or is not yours.');
  });

  it('explains when the request cannot be loaded', async () => {
    await app.open('/requests/r-1');
    app.http.expectOne('/api/requests/r-1').flush(null, { status: 500, statusText: 'Error' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe(
      'The request could not be loaded. Please try again later.',
    );
  });

  it('uploads a PDF and adds it to the documents', async () => {
    await openDraft();

    await app.choose('document', newPdf());
    expect(app.text('[role="status"]')).toBe('Saving your changes…');
    expect(app.button('#document').disabled).toBe(true);
    const upload = app.http.expectOne({ method: 'POST', url: '/api/requests/r-1/documents' });
    expect(((upload.request.body as FormData).get('file') as File).name).toBe('annex.pdf');
    upload.flush(pdfFile('f-3', 'annex.pdf', 900));
    await app.settle();

    expect(rows('documents')).toEqual(['lease.pdf 13 KB', 'annex.pdf 1 KB']);
    expect(app.element.querySelector('[role="status"]')).toBeNull();
    expect((app.element.querySelector('#document') as HTMLInputElement).value).toBe('');
  });

  it('ignores an empty file selection', async () => {
    await openDraft();

    await app.choose('document');

    expect(app.element.querySelector('[role="alert"]')).toBeNull();
  });

  it('refuses other files and big PDFs before uploading', async () => {
    await openDraft();

    await app.choose('document', new File(['hello'], 'notes.txt', { type: 'text/plain' }));
    expect(app.text('[role="alert"]')).toBe('Only PDF files can be signed.');

    const big = newPdf('scan.PDF');
    Object.defineProperty(big, 'size', { value: 20 * 1024 * 1024 + 1 });
    await app.choose('document', big);
    expect(app.text('[role="alert"]')).toBe('The file is larger than 20 MB.');
  });

  it('shows why the API refused the file', async () => {
    await openDraft();

    await app.choose('document', newPdf('fake.pdf'));
    app.http
      .expectOne('/api/requests/r-1/documents')
      .flush(
        { errors: { file: ['Only PDF files can be signed.'] } },
        { status: 400, statusText: 'Bad Request' },
      );
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('Only PDF files can be signed.');
    expect(rows('documents')).toEqual(['lease.pdf 13 KB']);
  });

  it('falls back to a general message for other upload errors', async () => {
    await openDraft();

    await app.choose('document', newPdf());
    app.http
      .expectOne('/api/requests/r-1/documents')
      .flush({ errors: { other: ['Unexpected.'] } }, { status: 400, statusText: 'Bad Request' });
    await app.settle();
    expect(app.text('[role="alert"]')).toBe('Something went wrong. Please try again.');

    await app.choose('document', newPdf());
    app.http
      .expectOne('/api/requests/r-1/documents')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await app.settle();
    expect(app.text('[role="alert"]')).toBe('Something went wrong. Please try again.');
  });

  it('removes documents and attachments', async () => {
    await openDraft();

    app.button('[aria-label="Remove lease.pdf"]').click();
    await app.settle();
    expect(app.button('[aria-label="Remove floor-plan.pdf"]').disabled).toBe(true);
    app.http.expectOne({ method: 'DELETE', url: '/api/requests/r-1/files/f-1' }).flush(null);
    await app.settle();
    expect(rows('documents')).toEqual([]);

    app.button('[aria-label="Remove floor-plan.pdf"]').click();
    await app.settle();
    app.http.expectOne({ method: 'DELETE', url: '/api/requests/r-1/files/f-2' }).flush(null);
    await app.settle();
    expect(app.element.querySelector('[data-testid="attachments"]')).toBeNull();
  });

  it('keeps the file when the removal fails', async () => {
    await openDraft();

    app.button('[aria-label="Remove lease.pdf"]').click();
    await app.settle();
    app.http
      .expectOne('/api/requests/r-1/files/f-1')
      .flush(null, { status: 404, statusText: 'Not Found' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('Something went wrong. Please try again.');
    expect(rows('documents')).toEqual(['lease.pdf 13 KB']);
  });
});
