using exam_test;
using Xunit;

namespace QuickForge.Tests
{
    public class TrustCommunicationPolicyTests
    {
        [Fact]
        public void BetaTrustStatement_IsHonestAboutAuditStatus()
        {
            string statement = TrustCommunicationPolicy.GetBetaTrustStatement();

            Assert.Contains("encrypts", statement);
            Assert.Contains("beta", statement);
            Assert.Contains("external security audit", statement);
        }

        [Fact]
        public void BetaTrustStatement_DoesNotClaimNoRisk()
        {
            string statement = TrustCommunicationPolicy.GetBetaTrustStatement().ToLowerInvariant();

            Assert.DoesNotContain("no risk", statement);
            Assert.DoesNotContain("impossible to hack", statement);
        }

        [Fact]
        public void ExternalAuditStatus_IsExplicit()
        {
            Assert.Equal("Not externally audited", TrustCommunicationPolicy.GetExternalAuditStatus());
        }

        [Fact]
        public void RealPasswordReadiness_StaysCarefulWithoutAudit()
        {
            string status = TrustCommunicationPolicy.GetRealPasswordReadinessStatus(
                hasExternalAudit: false,
                hasAuthenticatorLock: true,
                hasTrustedDevice: true);

            Assert.Contains("Candidate beta", status);
            Assert.Contains("not externally audited", status);
        }
    }
}
