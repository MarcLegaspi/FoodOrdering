import { Injectable, effect, inject, signal } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel
} from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { Order } from '../models/api.models';

export function upsertOrderList(current: Order[], incoming: Order): Order[] {
  const index = current.findIndex((o) => o.publicId === incoming.publicId);
  if (index === -1) {
    return [incoming, ...current];
  }

  const next = [...current];
  next[index] = incoming;
  return next;
}

@Injectable({ providedIn: 'root' })
export class OrderLiveService {
  private readonly auth = inject(AuthService);

  private connection: HubConnection | null = null;
  private startPromise: Promise<void> | null = null;
  private activeKitchenSlug: string | null = null;

  readonly connectionState = signal<'disconnected' | 'connecting' | 'connected'>('disconnected');
  readonly orderPlaced$ = new Subject<Order>();
  readonly orderUpdated$ = new Subject<Order>();
  readonly reconnected$ = new Subject<void>();

  constructor() {
    effect(() => {
      if (!this.auth.token()) {
        void this.stop();
      }
    });
  }

  async connectForCustomer(): Promise<void> {
    await this.ensureConnected();
  }

  async joinStoreKitchen(storeSlug: string): Promise<void> {
    const normalized = storeSlug.trim().toLowerCase();
    if (!normalized) {
      return;
    }

    this.activeKitchenSlug = normalized;
    const connected = await this.ensureConnected();
    if (!connected || !this.connection) {
      return;
    }

    try {
      await this.connection.invoke('JoinStoreKitchen', normalized);
    } catch {
      // Ignore transient join errors; reconnected handler retries automatically
    }
  }

  async leaveStoreKitchen(storeSlug: string): Promise<void> {
    const normalized = storeSlug.trim().toLowerCase();
    if (this.activeKitchenSlug === normalized) {
      this.activeKitchenSlug = null;
    }

    if (!this.connection || this.connection.state !== HubConnectionState.Connected || !normalized) {
      return;
    }

    try {
      await this.connection.invoke('LeaveStoreKitchen', normalized);
    } catch {
      // Ignore errors if socket is already closing
    }
  }

  async stop(): Promise<void> {
    this.activeKitchenSlug = null;
    if (!this.connection) {
      return;
    }

    const conn = this.connection;
    this.connection = null;
    this.startPromise = null;
    this.connectionState.set('disconnected');

    try {
      await conn.stop();
    } catch {
      // Ignore stop errors
    }
  }

  private async ensureConnected(): Promise<boolean> {
    if (!this.auth.token()) {
      return false;
    }

    const conn = this.getOrCreateConnection();
    if (conn.state === HubConnectionState.Connected) {
      this.connectionState.set('connected');
      return true;
    }

    if (!this.startPromise) {
      this.connectionState.set('connecting');
      this.startPromise = conn
        .start()
        .then(() => {
          this.connectionState.set('connected');
        })
        .catch(() => {
          this.connectionState.set('disconnected');
        })
        .finally(() => {
          this.startPromise = null;
        });
    }

    await this.startPromise;
    return this.connectionState() === 'connected';
  }

  private getOrCreateConnection(): HubConnection {
    if (this.connection) {
      return this.connection;
    }

    const conn = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/orders`, {
        accessTokenFactory: () => this.auth.token() ?? ''
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    conn.on('OrderPlaced', (order: Order) => this.orderPlaced$.next(order));
    conn.on('OrderUpdated', (order: Order) => this.orderUpdated$.next(order));

    conn.onreconnecting(() => this.connectionState.set('connecting'));
    conn.onreconnected(async () => {
      this.connectionState.set('connected');
      if (this.activeKitchenSlug) {
        try {
          await conn.invoke('JoinStoreKitchen', this.activeKitchenSlug);
        } catch {
          // Ignore join failure on reconnect
        }
      }
      this.reconnected$.next();
    });
    conn.onclose(() => this.connectionState.set('disconnected'));

    this.connection = conn;
    return conn;
  }
}

