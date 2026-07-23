import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { vaultService } from '../../services/vaultService';

export interface UiAttachment {
  /** Attachment id (vault records) or the owning document id (documents). */
  key: string;
  fileName: string | null;
  contentType: string;
  /** Relative URL to fetch the file blob. */
  url: string;
  /** Whether this attachment can be individually removed (vault records only). */
  deletable: boolean;
}

interface AttachmentPreviewProps {
  attachment: UiAttachment;
  onDelete?: (attachment: UiAttachment) => void;
  deleting?: boolean;
}

export function AttachmentPreview({ attachment, onDelete, deleting }: AttachmentPreviewProps) {
  const { t } = useTranslation();
  const [objectUrl, setObjectUrl] = useState<string | null>(null);
  const [failed, setFailed] = useState(false);

  const isImage = attachment.contentType.startsWith('image/');

  useEffect(() => {
    let active = true;
    let created = '';
    setObjectUrl(null);
    setFailed(false);

    vaultService
      .fetchBlob(attachment.url)
      .then((blob) => {
        if (!active) {
          return;
        }
        created = URL.createObjectURL(blob);
        setObjectUrl(created);
      })
      .catch(() => {
        if (active) {
          setFailed(true);
        }
      });

    return () => {
      active = false;
      if (created) {
        URL.revokeObjectURL(created);
      }
    };
  }, [attachment.url]);

  const openFull = () => {
    if (objectUrl) {
      window.open(objectUrl, '_blank', 'noopener,noreferrer');
    }
  };

  return (
    <div className="overflow-hidden rounded-lg border border-gray-200 bg-white">
      <button
        type="button"
        onClick={openFull}
        disabled={!objectUrl}
        className="flex h-28 w-full items-center justify-center bg-gray-50"
        aria-label={t('vault.attachments.view')}
      >
        {failed ? (
          <span className="text-xs text-gray-400">{t('vault.attachments.previewFailed')}</span>
        ) : !objectUrl ? (
          <span className="text-xs text-gray-400">{t('common.loading')}</span>
        ) : isImage ? (
          <img src={objectUrl} alt={attachment.fileName ?? ''} className="h-full w-full object-cover" />
        ) : (
          <svg className="h-10 w-10 text-red-500" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
          </svg>
        )}
      </button>

      <div className="flex items-center justify-between gap-2 px-2 py-1.5">
        <span className="min-w-0 flex-1 truncate text-xs text-gray-600" title={attachment.fileName ?? ''}>
          {attachment.fileName ?? t('vault.attachments.file')}
        </span>
        <div className="flex shrink-0 items-center gap-2">
          <button
            type="button"
            onClick={openFull}
            disabled={!objectUrl}
            className="text-xs font-medium text-brand-600 hover:underline disabled:opacity-50"
          >
            {t('vault.attachments.view')}
          </button>
          {attachment.deletable && onDelete && (
            <button
              type="button"
              onClick={() => onDelete(attachment)}
              disabled={deleting}
              className="text-xs font-medium text-red-600 hover:underline disabled:opacity-50"
            >
              {t('common.delete')}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
