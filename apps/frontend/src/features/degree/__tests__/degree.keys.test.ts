import { describe, it, expect } from 'vitest';
import { degreeKeys } from '../degree.keys';

describe('degreeKeys', () => {
  it('should generate consistent query keys array structure', () => {
    expect(degreeKeys.all).toEqual(['degrees']);
    expect(degreeKeys.lists(1, 20)).toEqual(['degrees', 'list', { pageIndex: 1, pageSize: 20 }]);
    expect(degreeKeys.detail('123')).toEqual(['degrees', 'detail', '123']);
    expect(degreeKeys.batchStatus('batch-1')).toEqual(['degrees', 'batch', 'batch-1']);
  });

  it('should differentiate list keys from detail keys', () => {
    expect(degreeKeys.lists()).not.toEqual(degreeKeys.detail('123'));
  });
});
