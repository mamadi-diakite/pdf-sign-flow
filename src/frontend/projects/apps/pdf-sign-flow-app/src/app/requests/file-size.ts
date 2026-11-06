const kilobyte = 1024;
const megabyte = 1024 * kilobyte;

/** Short size for file lists, e.g. "12 KB" or "3.4 MB". */
export function formatFileSize(bytes: number): string {
  return bytes < megabyte
    ? `${Math.max(1, Math.round(bytes / kilobyte))} KB`
    : `${(bytes / megabyte).toFixed(1)} MB`;
}
