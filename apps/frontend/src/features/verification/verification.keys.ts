export const verificationKeys = {
  all: ['verification'] as const,
  versions: (degreeCode: string) => [...verificationKeys.all, 'versions', degreeCode] as const,
};
