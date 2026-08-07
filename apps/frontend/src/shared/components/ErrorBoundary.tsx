import { Component, type ErrorInfo, type ReactNode } from 'react';
import { Button } from '@/shared/components/ui/button';
import { AlertTriangle, RefreshCw } from 'lucide-react';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false,
    error: null,
  };

  public static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    // eslint-disable-next-line no-console
    console.error('Uncaught React render error:', error, errorInfo);
  }

  private handleRetry = () => {
    this.setState({ hasError: false, error: null });
  };

  public render() {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback;
      }

      return (
        <div className="flex h-screen w-screen flex-col items-center justify-center p-6 bg-background text-foreground text-center">
          <div className="rounded-full bg-destructive/10 p-4 text-destructive mb-4">
            <AlertTriangle className="h-10 w-10" />
          </div>
          <h1 className="text-2xl font-bold tracking-tight">Something Went Wrong</h1>
          <p className="mt-2 text-sm text-muted-foreground max-w-md">
            An unexpected error occurred in the application user interface.
          </p>

          {this.state.error && (
            <div className="mt-4 max-w-lg rounded-md bg-muted p-3 text-xs font-mono text-muted-foreground text-left overflow-x-auto border">
              {this.state.error.message}
            </div>
          )}

          <div className="mt-6 flex gap-3">
            <Button onClick={this.handleRetry} className="gap-2">
              <RefreshCw className="h-4 w-4" />
              Try Again
            </Button>
            <Button
              variant="outline"
              onClick={() => {
                window.location.href = '/';
              }}
            >
              Go to Home
            </Button>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;
