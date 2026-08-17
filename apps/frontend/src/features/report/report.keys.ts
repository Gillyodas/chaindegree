export const reportKeys = {
  all: ['reports'] as const,
  lists: () => [...reportKeys.all, 'list'] as const,
  detail: (id: string) => [...reportKeys.all, 'detail', id] as const,
  evidence: (id: string) => [...reportKeys.all, 'evidence', id] as const,
};
