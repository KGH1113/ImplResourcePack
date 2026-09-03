import { create } from 'zustand';

const UPDATE_DISABLED_MESSAGE = 'ImplDmNote update channel is not configured';
const POST_UPDATE_NOTICE_KEY =
  'impl-dm-note:post-update-release-notice-version';

export interface UpdateInfo {
  currentVersion: string;
  latestVersion: string;
  releaseUrl: string;
  releaseName: string;
  releaseNotes: string;
  publishedAt: string;
}

interface UpdateState {
  updateAvailable: boolean;
  isLatestVersion: boolean;
  updateInfo: UpdateInfo | null;
  isChecking: boolean;
  isAutoUpdating: boolean;
  error: string | null;
  dismissed: boolean;
  checkForUpdates: (manual?: boolean) => Promise<void>;
  runAutoUpdate: (targetTag: string) => Promise<void>;
  dismissUpdate: () => void;
  skipVersion: () => void;
}

export function clearPendingPostUpdateReleaseNotice(): void {
  try {
    localStorage.removeItem(POST_UPDATE_NOTICE_KEY);
  } catch {
    // 무시
  }
}

export function hasPendingPostUpdateReleaseNotice(): boolean {
  return false;
}

export const useUpdateStore = create<UpdateState>((set) => ({
  updateAvailable: false,
  isLatestVersion: false,
  updateInfo: null,
  isChecking: false,
  isAutoUpdating: false,
  error: null,
  dismissed: false,
  checkForUpdates: async () => {
    set({ error: UPDATE_DISABLED_MESSAGE, isChecking: false });
  },
  runAutoUpdate: async () => {
    throw new Error(UPDATE_DISABLED_MESSAGE);
  },
  dismissUpdate: () => set({ dismissed: true, updateAvailable: false }),
  skipVersion: () => set({ dismissed: true, updateAvailable: false }),
}));

export function useUpdateCheck() {
  return useUpdateStore();
}
