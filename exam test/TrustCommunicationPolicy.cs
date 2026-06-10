namespace exam_test
{
    public static class TrustCommunicationPolicy
    {
        public static string GetBetaTrustStatement()
        {
            return "QuickForge encrypts your vault before cloud sync and includes safety controls such as recovery keys, Authenticator Lock, Device Trust, auto-lock, encrypted backups and sync-conflict protection. It is still beta software and has not received an external security audit yet.";
        }

        public static string GetExternalAuditStatus()
        {
            return "Not externally audited";
        }

        public static string GetRealPasswordReadinessStatus(bool hasExternalAudit, bool hasAuthenticatorLock, bool hasTrustedDevice)
        {
            if (!hasExternalAudit)
            {
                return "Real password readiness: Candidate beta / not externally audited";
            }

            if (!hasAuthenticatorLock || !hasTrustedDevice)
            {
                return "Real password readiness: Not ready";
            }

            return "Real password readiness: Candidate";
        }
    }
}
