import { useState, useRef, type DragEvent, type ChangeEvent } from 'react';
import { UploadCloud, File, X, AlertCircle } from 'lucide-react';
import { Button } from '@/shared/components/ui/button';
import { cn } from '@/shared/lib/utils';

export interface FileUploadProps {
  accept?: string;
  maxSizeMB?: number;
  onFileSelect: (file: File | null) => void;
  selectedFile?: File | null;
  error?: string;
  className?: string;
}

export function FileUpload({
  accept = '.pdf,.png,.jpg,.jpeg',
  maxSizeMB = 5,
  onFileSelect,
  selectedFile: initialFile,
  error: externalError,
  className,
}: FileUploadProps) {
  const [file, setFile] = useState<File | null>(initialFile ?? null);
  const [isDragOver, setIsDragOver] = useState(false);
  const [internalError, setInternalError] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const maxSizeBytes = maxSizeMB * 1024 * 1024;

  const validateAndSelectFile = (candidate: File) => {
    setInternalError(null);

    // Validate size (UX only)
    if (candidate.size > maxSizeBytes) {
      const err = `File size exceeds maximum allowed limit of ${maxSizeMB}MB.`;
      setInternalError(err);
      return;
    }

    setFile(candidate);
    onFileSelect(candidate);
  };

  const handleDragOver = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragOver(true);
  };

  const handleDragLeave = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragOver(false);
  };

  const handleDrop = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragOver(false);

    const droppedFile = e.dataTransfer.files[0];
    if (droppedFile) {
      validateAndSelectFile(droppedFile);
    }
  };

  const handleInputChange = (e: ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0];
    if (selected) {
      validateAndSelectFile(selected);
    }
  };

  const handleRemove = () => {
    setFile(null);
    setInternalError(null);
    onFileSelect(null);
    if (inputRef.current) {
      inputRef.current.value = '';
    }
  };

  const displayError = externalError ?? internalError;

  return (
    <div className={cn('space-y-2', className)}>
      {!file ? (
        <div
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
          onClick={() => inputRef.current?.click()}
          className={cn(
            'flex flex-col items-center justify-center rounded-lg border-2 border-dashed p-6 text-center cursor-pointer transition-colors',
            isDragOver
              ? 'border-primary bg-primary/5'
              : 'border-muted-foreground/25 hover:border-primary/50 hover:bg-accent/50',
            displayError && 'border-destructive bg-destructive/5',
          )}
        >
          <input
            ref={inputRef}
            type="file"
            accept={accept}
            onChange={handleInputChange}
            className="hidden"
          />
          <UploadCloud className="h-8 w-8 text-muted-foreground mb-2" />
          <p className="text-sm font-medium">
            Drag and drop your file here, or <span className="text-primary underline">browse</span>
          </p>
          <p className="text-xs text-muted-foreground mt-1">
            Supported formats: PDF, PNG, JPG (Max {maxSizeMB}MB)
          </p>
        </div>
      ) : (
        <div className="flex items-center justify-between rounded-lg border p-3 bg-muted/30">
          <div className="flex items-center gap-3 truncate">
            <File className="h-5 w-5 text-primary shrink-0" />
            <div className="truncate text-xs">
              <p className="font-medium truncate">{file.name}</p>
              <p className="text-muted-foreground">{(file.size / (1024 * 1024)).toFixed(2)} MB</p>
            </div>
          </div>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            onClick={handleRemove}
            className="h-7 w-7 text-muted-foreground hover:text-destructive"
          >
            <X className="h-4 w-4" />
          </Button>
        </div>
      )}

      {displayError && (
        <div className="flex items-center gap-1.5 text-xs text-destructive">
          <AlertCircle className="h-3.5 w-3.5" />
          <span>{displayError}</span>
        </div>
      )}
    </div>
  );
}

export default FileUpload;
