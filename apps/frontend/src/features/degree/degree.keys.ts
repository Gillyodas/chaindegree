export const degreeKeys = {
  all: ['degrees'] as const,
  lists: (pageIndex?: number, pageSize?: number) =>
    [...degreeKeys.all, 'list', { pageIndex, pageSize }] as const,
  detail: (id: string) => [...degreeKeys.all, 'detail', id] as const,
  batchStatus: (batchId: string) => [...degreeKeys.all, 'batch', batchId] as const,
};
