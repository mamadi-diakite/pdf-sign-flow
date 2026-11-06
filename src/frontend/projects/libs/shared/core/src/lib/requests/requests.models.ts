export interface CreateRequestInput {
  readonly title: string;
  readonly message: string | null;
}

export interface SignatureRequestSummary {
  readonly id: string;
  readonly title: string;
  readonly status: string;
  readonly documentCount: number;
  readonly updatedAtUtc: string;
}

export interface RequestFile {
  readonly id: string;
  readonly fileName: string;
  readonly contentType: string;
  readonly sizeBytes: number;
  readonly position: number;
  readonly uploadedAtUtc: string;
}

export interface SignatureRequestDetails {
  readonly id: string;
  readonly title: string;
  readonly message: string | null;
  readonly status: string;
  readonly createdAtUtc: string;
  readonly updatedAtUtc: string;
  readonly documents: readonly RequestFile[];
  readonly attachments: readonly RequestFile[];
}

/** Same limit as the API, checked first in the browser to avoid a useless upload. */
export const MAX_DOCUMENT_BYTES = 20 * 1024 * 1024;
