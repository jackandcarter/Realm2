interface HttpErrorOptions {
  retryAfterMs?: number;
  retryable?: boolean;
}

export class HttpError extends Error {
  public readonly retryAfterMs?: number;

  public readonly retryable?: boolean;

  constructor(public status: number, message: string, options?: HttpErrorOptions) {
    super(message);
    this.name = 'HttpError';
    this.retryAfterMs = options?.retryAfterMs;
    this.retryable = options?.retryable;
  }
}

export function isHttpError(error: unknown): error is HttpError {
  return error instanceof HttpError;
}
