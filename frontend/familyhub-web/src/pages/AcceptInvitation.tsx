import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../hooks/useAuth';
import { invitationService } from '../services/invitationService';
import { getApiErrorMessage } from '../utils/apiError';
import {
  clearPendingInvitationToken,
  getPendingInvitationToken,
  setPendingInvitationToken,
} from '../utils/pendingInvitation';
import { AuthLayout } from '../layouts/AuthLayout';
import { FullPageLoader } from '../components/FullPageLoader';

type Phase = 'idle' | 'accepting' | 'success' | 'error';

export default function AcceptInvitation() {
  const { t } = useTranslation();
  const { isAuthenticated, isLoading } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const tokenFromUrl = searchParams.get('token');
  const token = tokenFromUrl ?? getPendingInvitationToken();

  const [phase, setPhase] = useState<Phase>('idle');
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const attempted = useRef(false);

  // Remember the token so it survives a login/registration round-trip.
  useEffect(() => {
    if (tokenFromUrl) {
      setPendingInvitationToken(tokenFromUrl);
    }
  }, [tokenFromUrl]);

  // Once authenticated with a token, accept exactly once.
  useEffect(() => {
    if (isLoading || !isAuthenticated || !token || attempted.current) {
      return;
    }
    attempted.current = true;
    setPhase('accepting');
    invitationService
      .accept(token)
      .then(() => {
        clearPendingInvitationToken();
        setPhase('success');
      })
      .catch((err) => {
        clearPendingInvitationToken();
        setErrorMsg(getApiErrorMessage(err, t('acceptInvitation.errors.generic')));
        setPhase('error');
      });
  }, [isLoading, isAuthenticated, token, t]);

  if (isLoading) {
    return <FullPageLoader />;
  }

  // Missing token — the link is malformed.
  if (!token) {
    return (
      <AuthLayout>
        <Card title={t('acceptInvitation.invalidTitle')} hint={t('acceptInvitation.invalidHint')}>
          <Link
            to="/login"
            className="block w-full rounded-lg bg-brand-600 px-4 py-2.5 text-center text-sm font-semibold text-white hover:bg-brand-700"
          >
            {t('acceptInvitation.backToLogin')}
          </Link>
        </Card>
      </AuthLayout>
    );
  }

  // Not signed in — offer to sign in or create an account, then return here.
  if (!isAuthenticated) {
    return (
      <AuthLayout>
        <Card title={t('acceptInvitation.title')} hint={t('acceptInvitation.invitedSubtitle')}>
          <div className="space-y-3">
            <Link
              to="/login"
              className="block w-full rounded-lg bg-brand-600 px-4 py-2.5 text-center text-sm font-semibold text-white hover:bg-brand-700"
            >
              {t('acceptInvitation.signIn')}
            </Link>
            <Link
              to="/register"
              className="block w-full rounded-lg border border-brand-600 px-4 py-2.5 text-center text-sm font-semibold text-brand-700 hover:bg-brand-50"
            >
              {t('acceptInvitation.createAccount')}
            </Link>
            <p className="text-center text-xs text-gray-400">
              {t('acceptInvitation.continueNote')}
            </p>
          </div>
        </Card>
      </AuthLayout>
    );
  }

  // Authenticated — show the acceptance outcome.
  return (
    <AuthLayout>
      {phase === 'success' ? (
        <Card title={t('acceptInvitation.successTitle')} hint={t('acceptInvitation.successHint')}>
          <button
            type="button"
            onClick={() => navigate('/dashboard', { replace: true })}
            className="w-full rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700"
          >
            {t('acceptInvitation.goToFamily')}
          </button>
        </Card>
      ) : phase === 'error' ? (
        <Card title={t('acceptInvitation.errorTitle')} hint={errorMsg ?? undefined}>
          <button
            type="button"
            onClick={() => navigate('/dashboard', { replace: true })}
            className="w-full rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700"
          >
            {t('acceptInvitation.goToDashboard')}
          </button>
        </Card>
      ) : (
        <Card title={t('acceptInvitation.joining')}>
          <p className="text-center text-sm text-gray-400">{t('common.loading')}</p>
        </Card>
      )}
    </AuthLayout>
  );
}

function Card({
  title,
  hint,
  children,
}: {
  title: string;
  hint?: string;
  children?: React.ReactNode;
}) {
  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-xl font-semibold text-gray-900">{title}</h1>
        {hint && <p className="mt-1 text-sm text-gray-500">{hint}</p>}
      </div>
      {children}
    </div>
  );
}
