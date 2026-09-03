import CanvasTool from './CanvasTool';
import SettingTool from './SettingTool';
import TabTool from './TabTool';
import Github from '@assets/svgs/github.svg';
import Bug from '@assets/svgs/code.svg';
import NoteIcon from '@assets/svgs/note.svg';
import { TooltipGroup } from '../Modal/TooltipGroup';
import { useTranslation } from '@contexts/useTranslation';
import FloatingTooltip from '../Modal/FloatingTooltip';
import { useSettingsStore } from '@stores/useSettingsStore';
import KeyLimiterStatus from './KeyLimiterStatus';

interface ToolBarProps {
  onAddItem: (type: 'key' | 'stat' | 'graph' | 'knob') => void;
  onTogglePalette: () => void;
  isPaletteOpen: boolean;
  onResetCurrentMode: () => void;
  onResetCounters?: () => void;
  activeTool: string;
  setActiveTool: (tool: string) => void;
  isSettingsOpen?: boolean;
  onOpenSettings?: () => void;
  onCloseSettings?: () => void;
  showAlert?: (message: string) => void;
  onOpenNoteSetting?: () => void;
  primaryButtonRef?: React.RefObject<HTMLButtonElement>;
}

const ToolBar = ({
  onAddItem,
  onTogglePalette,
  isPaletteOpen,
  onResetCurrentMode,
  onResetCounters,
  activeTool,
  setActiveTool,
  isSettingsOpen = false,
  onOpenSettings,
  onCloseSettings,
  showAlert,
  onOpenNoteSetting,
  primaryButtonRef,
}: ToolBarProps) => {
  const { t } = useTranslation();
  const handleClick = (link: string) => {
    window.api.app.openExternal(link);
  };

  return (
    <div className="grid grid-cols-[auto_minmax(0,1fr)_auto] items-center w-full h-[60px] min-h-[60px] p-[10px] bg-primary border-t border-t-1 border-t-[#2A2A30]">
      <div className="flex min-w-0 justify-self-start overflow-hidden">
        {isSettingsOpen ? (
          <TooltipGroup>
            <div className="flex gap-[10px]">
              <FloatingTooltip content={t('tooltip.github')}>
                <button
                  onClick={() =>
                    handleClick('https://github.com/KGH1113/ImplResourcePack')
                  }
                  className="flex items-center justify-center w-[40px] h-[40px] p-[5px] bg-[#000000] rounded-[7px]"
                >
                  <div className="flex h-full w-full items-center justify-center rounded-[7px] hover:bg-button-hover active:bg-button-active">
                    <Github className="flex-shrink-0 mb-[3px]" />
                  </div>
                </button>
              </FloatingTooltip>
              <FloatingTooltip content={t('tooltip.issue')}>
                <button
                  onClick={() =>
                    handleClick(
                      'https://github.com/KGH1113/ImplResourcePack/issues',
                    )
                  }
                  className="flex items-center justify-center w-[127px] h-[40px] p-[5px] bg-[#000000] rounded-[7px]"
                >
                  <div className="flex h-full w-full items-center justify-center gap-[8px] rounded-[7px] hover:bg-button-hover active:bg-button-active">
                    <Bug className="flex-shrink-0" />
                    <p className="text-style-3 text-[#DBDEE8] truncate">
                      {/* {t("tooltip.issue")} */}
                      Report
                    </p>
                  </div>
                </button>
              </FloatingTooltip>
            </div>
          </TooltipGroup>
        ) : (
          <TabTool />
        )}
      </div>
      <div className="key-limiter-status-slot flex min-w-0 items-center justify-center px-[12px]">
        <KeyLimiterStatus />
      </div>
      <div className="flex gap-[10px] justify-self-end">
        {!isSettingsOpen && (
          <CanvasTool
            onAddItem={onAddItem}
            onTogglePalette={onTogglePalette}
            isPaletteOpen={isPaletteOpen}
            onResetCurrentMode={onResetCurrentMode}
            onResetCounters={onResetCounters}
            activeTool={activeTool}
            setActiveTool={setActiveTool}
            primaryButtonRef={primaryButtonRef}
          />
        )}
        {!isSettingsOpen && (
          <TrackSettingButton onOpenNoteSetting={onOpenNoteSetting} t={t} />
        )}
        <SettingTool
          isSettingsOpen={isSettingsOpen}
          onOpenSettings={onOpenSettings}
          onCloseSettings={onCloseSettings}
          showAlert={showAlert}
          // onOpenNoteSetting={onOpenNoteSetting}
        />
      </div>
    </div>
  );
};

const TrackSettingButton = ({
  onOpenNoteSetting,
  t,
}: {
  onOpenNoteSetting?: () => void;
  t: (key: string) => string;
}) => {
  const { noteEffect } = useSettingsStore();

  if (!noteEffect) return null;

  return (
    <TooltipGroup>
      <div className="flex items-center h-[40px] p-[5px] bg-button-primary rounded-[7px]">
        <FloatingTooltip content={t('tooltip.trackSettings') || '트랙 설정'}>
          <button
            type="button"
            onClick={onOpenNoteSetting}
            className="flex items-center justify-center h-[30px] w-[30px] rounded-[7px] transition-colors bg-button-primary hover:bg-button-hover active:bg-button-active"
          >
            <NoteIcon />
          </button>
        </FloatingTooltip>
      </div>
    </TooltipGroup>
  );
};

export default ToolBar;
