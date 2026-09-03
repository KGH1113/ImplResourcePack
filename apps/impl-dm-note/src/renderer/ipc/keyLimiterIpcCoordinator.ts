import {
  IpcVersionMismatchError,
  tryConnect,
  type IpcNamespaceDetail,
} from '@adofai-ipc/client';
import type { KeyLimiterSyncRequest } from './keyLimiterSnapshot';
import { nativeIpcFetch } from './nativeIpcFetch';

const NAMESPACE = 'impl-resourcepack';
const SYNC_METHOD = 'key-limiter.sync';

export type KeyLimiterConnectionState =
  | 'disconnected'
  | 'connecting'
  | 'ready'
  | 'retrying'
  | 'version-mismatch'
  | 'disposed';

export interface KeyLimiterIpcClient {
  health(): Promise<unknown>;
  waitForNamespace(
    namespace: string,
    options: { status: 'ready'; timeoutMs: number; pollIntervalMs: number },
  ): Promise<IpcNamespaceDetail>;
  call<TResult = unknown, TParams = unknown>(options: {
    namespace: string;
    method: string;
    params: TParams;
  }): Promise<TResult>;
}

export interface KeyLimiterSyncResponse {
  ok: boolean;
  applied: boolean;
  reason: string;
  enabled: boolean;
  sessionId: string;
  revision: number;
  supportedKeys: string[];
  unsupportedKeys: string[];
}

export interface KeyLimiterCoordinatorOptions {
  connect?: () => Promise<KeyLimiterIpcClient>;
  debounceMs?: number;
  initialRetryMs?: number;
  maximumRetryMs?: number;
  connectionCheckMs?: number;
  onStateChange?: (state: KeyLimiterConnectionState, error?: unknown) => void;
  onSyncStart?: (request: KeyLimiterSyncRequest) => void;
  onSyncResult?: (
    request: KeyLimiterSyncRequest,
    response: KeyLimiterSyncResponse,
  ) => void;
  onSyncError?: (request: KeyLimiterSyncRequest, error: unknown) => void;
}

export class KeyLimiterIpcCoordinator {
  private readonly connect: () => Promise<KeyLimiterIpcClient>;
  private readonly debounceMs: number;
  private readonly initialRetryMs: number;
  private readonly maximumRetryMs: number;
  private readonly connectionCheckMs: number;
  private readonly onStateChange?: KeyLimiterCoordinatorOptions['onStateChange'];
  private readonly onSyncStart?: KeyLimiterCoordinatorOptions['onSyncStart'];
  private readonly onSyncResult?: KeyLimiterCoordinatorOptions['onSyncResult'];
  private readonly onSyncError?: KeyLimiterCoordinatorOptions['onSyncError'];
  private client: KeyLimiterIpcClient | null = null;
  private latest: KeyLimiterSyncRequest | null = null;
  private pending: KeyLimiterSyncRequest | null = null;
  private debounceTimer: ReturnType<typeof setTimeout> | null = null;
  private retryTimer: ReturnType<typeof setTimeout> | null = null;
  private connectionCheckTimer: ReturnType<typeof setTimeout> | null = null;
  private running = false;
  private disposed = false;
  private retryMs: number;
  private state: KeyLimiterConnectionState = 'disconnected';

  constructor(options: KeyLimiterCoordinatorOptions = {}) {
    this.connect =
      options.connect ?? (() => tryConnect({ fetch: nativeIpcFetch }));
    this.debounceMs = options.debounceMs ?? 75;
    this.initialRetryMs = options.initialRetryMs ?? 500;
    this.maximumRetryMs = options.maximumRetryMs ?? 10_000;
    this.connectionCheckMs = options.connectionCheckMs ?? 1_000;
    this.retryMs = this.initialRetryMs;
    this.onStateChange = options.onStateChange;
    this.onSyncStart = options.onSyncStart;
    this.onSyncResult = options.onSyncResult;
    this.onSyncError = options.onSyncError;
  }

  get connectionState(): KeyLimiterConnectionState {
    return this.state;
  }

  enqueue(request: KeyLimiterSyncRequest): void {
    if (this.disposed || this.state === 'version-mismatch') return;

    this.latest = request;
    this.pending = request;
    this.clearConnectionCheckTimer();
    if (this.retryTimer || this.running) return;

    this.clearDebounceTimer();
    this.debounceTimer = setTimeout(() => {
      this.debounceTimer = null;
      void this.flush();
    }, this.debounceMs);
  }

  async flushNow(): Promise<void> {
    this.clearDebounceTimer();
    this.clearRetryTimer();
    await this.flush();
  }

  dispose(): void {
    this.disposed = true;
    this.latest = null;
    this.pending = null;
    this.client = null;
    this.clearDebounceTimer();
    this.clearRetryTimer();
    this.clearConnectionCheckTimer();
    this.setState('disposed');
  }

  private async flush(): Promise<void> {
    if (
      this.disposed ||
      this.running ||
      this.state === 'version-mismatch' ||
      !this.pending
    ) {
      return;
    }

    this.running = true;
    try {
      while (this.pending && !this.disposed) {
        const request = this.pending;
        this.pending = null;

        try {
          const client = await this.getReadyClient();
          this.onSyncStart?.(request);
          const response = await client.call<
            KeyLimiterSyncResponse,
            KeyLimiterSyncRequest
          >({
            namespace: NAMESPACE,
            method: SYNC_METHOD,
            params: request,
          });
          this.onSyncResult?.(request, response);
          this.retryMs = this.initialRetryMs;
          this.setState('ready');
          this.scheduleConnectionCheck();
        } catch (error) {
          this.onSyncError?.(request, error);
          this.client = null;
          this.pending ??= request;

          if (error instanceof IpcVersionMismatchError) {
            this.setState('version-mismatch', error);
            return;
          }

          this.setState('retrying', error);
          this.scheduleRetry();
          return;
        }
      }
    } finally {
      this.running = false;
    }
  }

  private async getReadyClient(): Promise<KeyLimiterIpcClient> {
    if (this.client) return this.client;

    this.setState('connecting');
    const client = await this.connect();
    await client.waitForNamespace(NAMESPACE, {
      status: 'ready',
      timeoutMs: 10_000,
      pollIntervalMs: 100,
    });
    this.client = client;
    this.setState('ready');
    return client;
  }

  private scheduleRetry(): void {
    if (this.retryTimer || this.disposed) return;

    const delayMs = this.retryMs;
    this.retryMs = Math.min(this.retryMs * 2, this.maximumRetryMs);
    this.retryTimer = setTimeout(() => {
      this.retryTimer = null;
      void this.flush();
    }, delayMs);
  }

  private scheduleConnectionCheck(): void {
    if (
      this.connectionCheckTimer ||
      this.disposed ||
      this.state !== 'ready' ||
      !this.client
    ) {
      return;
    }

    this.connectionCheckTimer = setTimeout(() => {
      this.connectionCheckTimer = null;
      void this.checkConnection();
    }, this.connectionCheckMs);
  }

  private async checkConnection(): Promise<void> {
    if (this.disposed || this.state !== 'ready' || !this.client) return;

    const client = this.client;
    try {
      await client.health();
      if (this.client !== client || this.disposed) return;
      this.scheduleConnectionCheck();
    } catch (error) {
      if (this.client !== client || this.disposed) return;
      this.client = null;
      this.pending ??= this.latest;
      this.setState('retrying', error);
      this.scheduleRetry();
    }
  }

  private clearDebounceTimer(): void {
    if (!this.debounceTimer) return;
    clearTimeout(this.debounceTimer);
    this.debounceTimer = null;
  }

  private clearRetryTimer(): void {
    if (!this.retryTimer) return;
    clearTimeout(this.retryTimer);
    this.retryTimer = null;
  }

  private clearConnectionCheckTimer(): void {
    if (!this.connectionCheckTimer) return;
    clearTimeout(this.connectionCheckTimer);
    this.connectionCheckTimer = null;
  }

  private setState(state: KeyLimiterConnectionState, error?: unknown): void {
    if (this.state === state && error === undefined) return;
    this.state = state;
    this.onStateChange?.(state, error);
  }
}
