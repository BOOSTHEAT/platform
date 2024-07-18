using System.Linq;
using System.Security.Cryptography.X509Certificates;
using ImpliciX.Harmony.Infrastructure;
using NUnit.Framework;

namespace ImpliciX.Harmony.Tests
{
    public class InfrastructureTest
    {
        [Test]
        [Category("ExcludeFromCI")]
        public void InstallCert()
        {
            AzureIotHubAdapter.InstallCaCert("ImpliciX.Harmony.Infrastructure.BaltimoreCyberTrustRoot.crt.pem");
            AzureIotHubAdapter.InstallCaCert("ImpliciX.Harmony.Infrastructure.DigiCertGlobalRootG2.crt.pem");

            using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var expectedThumbPrints = new[]
            {
                "D4DE20D05E66FC53FE1A50882C78DB2852CAE474",
                "DF3C24F9BFD666761B268073FE06D1CC8D4F82A4"
            };
            Assert.That(store.Certificates,
                Has.Some.Matches<X509Certificate2>(x => expectedThumbPrints.Contains(x.Thumbprint)));
        }
    }
}