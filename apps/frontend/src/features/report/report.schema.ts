import { z } from 'zod';

export const MAX_FILE_SIZE_BYTES = 5 * 1024 * 1024; // 5MB
export const ALLOWED_FILE_TYPES = [
  'application/pdf',
  'image/png',
  'image/jpeg',
  'image/jpg',
];
export const ALLOWED_FILE_EXTENSIONS = ['.pdf', '.png', '.jpg', '.jpeg'];

export function isValidFileExtension(fileName: string): boolean {
  const lowerName = fileName.toLowerCase();
  return ALLOWED_FILE_EXTENSIONS.some((ext) => lowerName.endsWith(ext));
}

export const submitReportSchema = z.object({
  degreeId: z.string().uuid({ message: 'Invalid degree ID.' }),
  reportType: z.enum(['Administrative_Error', 'Fraudulent_Data'], {
    errorMap: () => ({ message: 'Please select a valid report type.' }),
  }),
  description: z
    .string({ required_error: 'Description is required.' })
    .trim()
    .min(10, 'Description must be at least 10 characters long.')
    .max(2000, 'Description cannot exceed 2000 characters.'),
  evidenceFile: z
    .custom<File>((val) => val instanceof File, {
      message: 'An evidence file is required.',
    })
    .refine((file) => file.size > 0, {
      message: 'Evidence file cannot be empty.',
    })
    .refine((file) => file.size <= MAX_FILE_SIZE_BYTES, {
      message: 'File size must not exceed 5MB.',
    })
    .refine(
      (file) =>
        ALLOWED_FILE_TYPES.includes(file.type) || isValidFileExtension(file.name),
      {
        message: 'Only PDF, PNG, and JPG files are supported.',
      },
    ),
});

export type SubmitReportFormValues = z.infer<typeof submitReportSchema>;
