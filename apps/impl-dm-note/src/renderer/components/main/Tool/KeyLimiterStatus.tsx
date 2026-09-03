import { useMemo } from 'react';
import { useTranslation } from '@contexts/useTranslation';
import { useKeyLimiterStatusStore } from '@stores/useKeyLimiterStatusStore';
import FloatingTooltip from '../Modal/FloatingTooltip';
import {
  getKeyLimiterStatusPresentation,
  type StatusIcon,
} from './keyLimiterStatusPresentation';

function StatusGlyph({ icon }: { icon: StatusIcon }) {
  if (icon === 'check') {
    return (
      <svg
        className="key-limiter-status-glyph key-limiter-status-glyph--complete"
        viewBox="0 0 16 16"
        aria-hidden="true"
      >
        <path
          d="m4.1 8.2 2.35 2.35L11.9 5.1"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.8"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    );
  }
  if (icon === 'warning') {
    return (
      <svg
        className="key-limiter-status-glyph"
        viewBox="0 0 16 16"
        aria-hidden="true"
      >
        <path
          d="M8 2.4 14 13H2L8 2.4Z"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.35"
          strokeLinejoin="round"
        />
        <path
          d="M8 6v3.2M8 11.5v.1"
          stroke="currentColor"
          strokeWidth="1.5"
          strokeLinecap="round"
        />
      </svg>
    );
  }
  if (icon === 'error') {
    return (
      <svg
        className="key-limiter-status-glyph"
        viewBox="0 0 16 16"
        aria-hidden="true"
      >
        <circle
          cx="8"
          cy="8"
          r="5.5"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.35"
        />
        <path
          d="m6 6 4 4m0-4-4 4"
          stroke="currentColor"
          strokeWidth="1.45"
          strokeLinecap="round"
        />
      </svg>
    );
  }
  if (icon === 'progress') {
    return (
      <svg
        className="key-limiter-status-glyph key-limiter-status-glyph--progress"
        viewBox="0 0 16 16"
        aria-hidden="true"
      >
        <circle
          cx="8"
          cy="8"
          r="5.25"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.5"
          strokeLinecap="round"
          strokeDasharray="23 10"
        />
      </svg>
    );
  }
  return <span className="key-limiter-status-dot" aria-hidden="true" />;
}

export default function KeyLimiterStatus() {
  const { t, i18n } = useTranslation();
  const connectionState = useKeyLimiterStatusStore(
    (state) => state.connectionState,
  );
  const deliveryState = useKeyLimiterStatusStore(
    (state) => state.deliveryState,
  );
  const acknowledgement = useKeyLimiterStatusStore(
    (state) => state.lastAcknowledgement,
  );
  const error = useKeyLimiterStatusStore((state) => state.error);
  const unsupportedKeys = acknowledgement?.unsupportedKeys ?? [];
  const presentation = getKeyLimiterStatusPresentation(
    connectionState,
    deliveryState,
    unsupportedKeys.length > 0,
  );
  const label = t(presentation.labelKey);
  const formattedTime = useMemo(() => {
    if (!acknowledgement) return null;
    return new Intl.DateTimeFormat(i18n.language, {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    }).format(acknowledgement.acknowledgedAt);
  }, [acknowledgement, i18n.language]);

  const detail = (
    <div className="key-limiter-status-detail">
      <div
        className={`key-limiter-status-detail__heading key-limiter-status-detail__heading--${presentation.tone}`}
      >
        <StatusGlyph icon={presentation.icon} />
        <span>{label}</span>
      </div>
      {acknowledgement ? (
        <div className="key-limiter-status-detail__body">
          <div>
            <span>{t('keyLimiter.profile')}</span>
            <strong>{acknowledgement.profile.name}</strong>
          </div>
          <div>
            <span>{t('keyLimiter.appliedKeys')}</span>
            <strong>{acknowledgement.supportedKeys.length}</strong>
          </div>
          <div>
            <span>{t('keyLimiter.revision')}</span>
            <strong>{acknowledgement.revision}</strong>
          </div>
          <div>
            <span>{t('keyLimiter.lastApplied')}</span>
            <strong>{formattedTime}</strong>
          </div>
          {unsupportedKeys.length > 0 && (
            <div className="key-limiter-status-detail__unsupported">
              <span>{t('keyLimiter.unsupportedKeys')}</span>
              <strong>{unsupportedKeys.join(', ')}</strong>
            </div>
          )}
        </div>
      ) : (
        <p className="key-limiter-status-detail__empty">
          {t('keyLimiter.waitingDetail')}
        </p>
      )}
      {error && <p className="key-limiter-status-detail__error">{error}</p>}
    </div>
  );

  return (
    <FloatingTooltip content={detail} delay={180}>
      <button
        type="button"
        className={`key-limiter-status key-limiter-status--${presentation.tone}`}
        aria-label={label}
      >
        <span
          className="key-limiter-status__live"
          role="status"
          aria-live="polite"
          aria-atomic="true"
        >
          <StatusGlyph icon={presentation.icon} />
          <span className="key-limiter-status__label">{label}</span>
        </span>
      </button>
    </FloatingTooltip>
  );
}
