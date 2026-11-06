import { formatFileSize } from './file-size';

describe('formatFileSize', () => {
  it('rounds small files to whole kilobytes, at least 1', () => {
    expect(formatFileSize(10)).toBe('1 KB');
    expect(formatFileSize(12_800)).toBe('13 KB');
  });

  it('shows megabytes with one decimal', () => {
    expect(formatFileSize(1024 * 1024)).toBe('1.0 MB');
    expect(formatFileSize(3.45 * 1024 * 1024)).toBe('3.5 MB');
  });
});
