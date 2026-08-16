import { z } from 'zod';

export const verificationSchema = z.object({
  degreeCode: z.string().min(1, 'Degree code is required.').trim(),
  version: z.coerce.number().int().positive().optional(),
});

export type VerificationFormData = z.infer<typeof verificationSchema>;
