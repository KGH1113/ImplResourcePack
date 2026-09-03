import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import {
  afterAll,
  afterEach,
  beforeAll,
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest';
import { useKeyStore } from '@stores/data/useKeyStore';
import TabList from './TabList';

vi.mock('@assets/svgs/plus2.svg', () => ({
  default: () => <svg data-icon="plus" />,
}));

vi.mock('@assets/svgs/minus.svg', () => ({
  default: () => <svg data-icon="minus" />,
}));

vi.mock('@contexts/useTranslation', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

vi.mock('@hooks/useLenis', () => ({
  useLenis: () => ({
    scrollContainerRef: vi.fn(),
    wrapperElement: null,
    lenisInstance: { current: { resize: vi.fn() } },
    scrollbarWidth: 0,
  }),
}));

vi.mock('../dialogs/Alert.jsx', () => ({ default: () => null }));
vi.mock('../editors/TabNameModal', () => ({ default: () => null }));

describe('TabList', () => {
  let container: HTMLDivElement;
  let root: Root;
  let originalRequestAnimationFrame: typeof requestAnimationFrame;
  let originalCancelAnimationFrame: typeof cancelAnimationFrame;

  beforeAll(() => {
    globalThis.IS_REACT_ACT_ENVIRONMENT = true;
    originalRequestAnimationFrame = globalThis.requestAnimationFrame;
    originalCancelAnimationFrame = globalThis.cancelAnimationFrame;
    globalThis.requestAnimationFrame = (callback) => {
      callback(0);
      return 1;
    };
    globalThis.cancelAnimationFrame = vi.fn();
  });

  afterAll(() => {
    globalThis.IS_REACT_ACT_ENVIRONMENT = false;
    globalThis.requestAnimationFrame = originalRequestAnimationFrame;
    globalThis.cancelAnimationFrame = originalCancelAnimationFrame;
  });

  beforeEach(() => {
    container = document.createElement('div');
    document.body.appendChild(container);
    root = createRoot(container);
    useKeyStore.setState({
      selectedKeyType: 'hand-two',
      selectedViewerTabs: { hand: 'hand-two', foot: 'foot-one' },
      tabs: [
        { id: 'hand-one', name: 'Hand 1', viewerKind: 'hand' },
        { id: 'hand-two', name: 'Hand 2', viewerKind: 'hand' },
        { id: 'foot-one', name: 'Foot 1', viewerKind: 'foot' },
      ],
    });
  });

  afterEach(() => {
    act(() => root.unmount());
    container.remove();
  });

  it('separates the selected tab from hover state with a check and aria state', () => {
    act(() => root.render(<TabList viewerKind="hand" />));

    const selected = container.querySelector<HTMLButtonElement>(
      'button[data-selected="true"]',
    );
    const unselected = container.querySelector<HTMLButtonElement>(
      'button[data-selected="false"]',
    );

    expect(container.textContent).toContain('tabs.handViewer');
    expect(selected?.textContent).toContain('Hand 2');
    expect(selected?.getAttribute('aria-current')).toBe('true');
    expect(selected?.querySelector('path')).not.toBeNull();
    expect(selected?.className).toContain('bg-button-active');
    expect(unselected?.getAttribute('aria-current')).toBeNull();
    expect(unselected?.querySelector('path')).toBeNull();
    expect(document.activeElement).toBe(selected);
  });
});
