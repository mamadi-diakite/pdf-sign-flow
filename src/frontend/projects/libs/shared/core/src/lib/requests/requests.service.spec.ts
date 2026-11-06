import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideAppEnvironment } from '@pdf-sign-flow/shared/environment';
import { RequestsService } from './requests.service';

describe('RequestsService', () => {
  let service: RequestsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAppEnvironment({ apiBaseUrl: '/api' }),
      ],
    });
    service = TestBed.inject(RequestsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists, creates and reads requests', () => {
    service.list().subscribe();
    expect(http.expectOne({ method: 'GET', url: '/api/requests' }).request.body).toBeNull();

    service.create({ title: 'Lease', message: null }).subscribe();
    const create = http.expectOne({ method: 'POST', url: '/api/requests' });
    expect(create.request.body).toEqual({ title: 'Lease', message: null });

    service.get('r-1').subscribe();
    http.expectOne({ method: 'GET', url: '/api/requests/r-1' });
  });

  it('uploads a document as multipart form data', () => {
    const file = new File(['%PDF-1.7'], 'lease.pdf', { type: 'application/pdf' });

    service.uploadDocument('r-1', file).subscribe();

    const upload = http.expectOne({ method: 'POST', url: '/api/requests/r-1/documents' });
    const body = upload.request.body as FormData;
    expect((body.get('file') as File).name).toBe('lease.pdf');
  });

  it('removes a file', () => {
    service.removeFile('r-1', 'f-1').subscribe();

    http.expectOne({ method: 'DELETE', url: '/api/requests/r-1/files/f-1' });
  });
});
