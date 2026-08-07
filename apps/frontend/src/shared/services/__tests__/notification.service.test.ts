import { describe, it, expect, vi } from 'vitest';
import { notification } from '../notification.service';
import { toast } from 'sonner';

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
    warning: vi.fn(),
    info: vi.fn(),
  },
}));

describe('notification.service', () => {
  it('should invoke underlying toast.success', () => {
    notification.success('Success message');
    expect(toast.success).toHaveBeenCalledWith('Success message');
  });

  it('should invoke underlying toast.error', () => {
    notification.error('Error message');
    expect(toast.error).toHaveBeenCalledWith('Error message');
  });

  it('should invoke underlying toast.warning', () => {
    notification.warning('Warning message');
    expect(toast.warning).toHaveBeenCalledWith('Warning message');
  });

  it('should invoke underlying toast.info', () => {
    notification.info('Info message');
    expect(toast.info).toHaveBeenCalledWith('Info message');
  });
});
