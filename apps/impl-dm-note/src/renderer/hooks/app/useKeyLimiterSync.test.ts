import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { useKeyStore } from '@stores/data/useKeyStore';
import type { KeyPositions } from '@src/types/key/keys';
import { KeyLimiterIpcCoordinator } from '@src/renderer/ipc/keyLimiterIpcCoordinator';
import { startKeyLimiterSync } from './useKeyLimiterSync';

describe('startKeyLimiterSync', () => {
  const originalState = useKeyStore.getState();

  beforeEach(() => {
    vi.useFakeTimers();
    useKeyStore.setState({
      selectedKeyType: 'hand-main',
      tabs: [],
      selectedViewerTabs: { hand: 'hand-main', foot: 'foot-main' },
      keyMappings: {},
      positions: {},
      isBootstrapped: false,
    });
  });

  afterEach(() => {
    useKeyStore.setState(originalState, true);
    vi.useRealTimers();
  });

  it('syncs both viewer selections but ignores editor-only tab changes', async () => {
    const call = vi.fn().mockResolvedValue({ applied: true });
    const coordinator = new KeyLimiterIpcCoordinator({
      connect: vi.fn().mockResolvedValue({
        waitForNamespace: vi.fn().mockResolvedValue({
          namespace: 'impl-resourcepack',
          displayName: 'ImplResourcePack',
          version: '0.1.0',
          status: 'ready',
          methods: ['key-limiter.sync'],
        }),
        call,
      }),
    });
    const stop = startKeyLimiterSync(coordinator);

    useKeyStore.setState({
      isBootstrapped: true,
      keyMappings: {
        'hand-main': ['KeyA'],
        'hand-alt': ['KeyS'],
        'foot-main': ['F8'],
        'foot-alt': ['F7'],
      },
      positions: {
        'hand-main': [{}] as KeyPositions[string],
        'hand-alt': [{}] as KeyPositions[string],
        'foot-main': [{}] as KeyPositions[string],
        'foot-alt': [{}] as KeyPositions[string],
      },
      tabs: [
        { id: 'hand-main', name: 'Hand main', viewerKind: 'hand' },
        { id: 'hand-alt', name: 'Hand alt', viewerKind: 'hand' },
        { id: 'foot-main', name: 'Foot main', viewerKind: 'foot' },
        { id: 'foot-alt', name: 'Foot alt', viewerKind: 'foot' },
      ],
    });
    await vi.advanceTimersByTimeAsync(75);

    useKeyStore.setState({ selectedKeyType: 'hand-alt' });
    await vi.advanceTimersByTimeAsync(75);
    expect(call).toHaveBeenCalledTimes(1);

    useKeyStore.setState({
      selectedViewerTabs: { hand: 'hand-alt', foot: 'foot-main' },
    });
    await vi.advanceTimersByTimeAsync(75);

    useKeyStore.setState({
      selectedViewerTabs: { hand: 'hand-alt', foot: 'foot-alt' },
    });
    await vi.advanceTimersByTimeAsync(75);

    expect(call).toHaveBeenCalledTimes(3);
    expect(call.mock.calls[0][0].params).toMatchObject({
      revision: 1,
      profile: {
        id: 'dual:hand-main:foot-main',
        name: 'Hand main + Foot main',
      },
      keys: ['KeyA', 'F8'],
    });
    expect(call.mock.calls[1][0].params).toMatchObject({
      revision: 2,
      profile: {
        id: 'dual:hand-alt:foot-main',
        name: 'Hand alt + Foot main',
      },
      keys: ['KeyS', 'F8'],
    });
    expect(call.mock.calls[2][0].params).toMatchObject({
      revision: 3,
      profile: {
        id: 'dual:hand-alt:foot-alt',
        name: 'Hand alt + Foot alt',
      },
      keys: ['KeyS', 'F7'],
    });

    stop();
  });

  it('syncs limiter metadata changes but ignores visual-only position changes', async () => {
    const call = vi.fn().mockResolvedValue({ applied: true });
    const coordinator = new KeyLimiterIpcCoordinator({
      connect: vi.fn().mockResolvedValue({
        waitForNamespace: vi.fn().mockResolvedValue({
          namespace: 'impl-resourcepack',
          displayName: 'ImplResourcePack',
          version: '0.1.0',
          status: 'ready',
          methods: ['key-limiter.sync'],
        }),
        call,
      }),
    });
    const stop = startKeyLimiterSync(coordinator);

    useKeyStore.setState({
      selectedViewerTabs: { hand: 'hand', foot: 'foot' },
      isBootstrapped: true,
      keyMappings: { hand: ['KeyA'], foot: ['F8'] },
      positions: {
        hand: [{ dx: 0, keyLimiterExcluded: false }] as KeyPositions[string],
        foot: [{}] as KeyPositions[string],
      },
    });
    await vi.advanceTimersByTimeAsync(75);

    useKeyStore.setState({
      positions: {
        hand: [{ dx: 50, keyLimiterExcluded: false }] as KeyPositions[string],
        foot: [{}] as KeyPositions[string],
      },
    });
    await vi.advanceTimersByTimeAsync(75);
    expect(call).toHaveBeenCalledTimes(1);

    useKeyStore.setState({
      positions: {
        hand: [{ dx: 50, keyLimiterExcluded: true }] as KeyPositions[string],
        foot: [{}] as KeyPositions[string],
      },
    });
    await vi.advanceTimersByTimeAsync(75);

    expect(call).toHaveBeenCalledTimes(2);
    expect(call.mock.calls[1][0].params).toMatchObject({
      revision: 2,
      enabled: true,
      keys: ['F8'],
      excludedGhostKeys: ['KeyA'],
    });

    stop();
  });
});
