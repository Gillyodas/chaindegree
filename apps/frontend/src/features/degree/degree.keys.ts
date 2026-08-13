export const degreeKeys = {
  all: ['degrees'] as const,
  lists: () => [...degreeKeys.all, 'list'] as const,
  detail: (id: string) => [...degreeKeys.all, 'detail', id] as const,
  batchStatus: (batchId: string) => [...degreeKeys.all, 'batch', batchId] as const,
};
