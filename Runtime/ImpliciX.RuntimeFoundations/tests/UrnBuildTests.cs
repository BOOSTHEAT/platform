using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Tests.FactoryTests.Elements;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.RuntimeFoundations.Tests
{
    [TestFixture]
    public class UrnBuildTests
    {
        [TestCase(new string[] { }, "")]
        [TestCase(new[] { "root" }, "root")]
        [TestCase(new[] { "root","level1","level2" }, "root:level1:level2")]
        public void build_urn(string[] components, string expectedKey)
        {
            var key = Urn.BuildUrn(components);
            Assert.AreEqual(key, (Urn)expectedKey);
        }
        [Test]
        public void urns_should_be_ref_equal()
        {
            var c1 = lightning.interior.kitchen._tune;
            var c2 = lightning.interior.kitchen._tune;
            
            var p1 = lightning.interior.kitchen.consumption;
            var p2 = lightning.interior.kitchen.consumption;

            Check.That(ReferenceEquals(p1, p2)).IsTrue();
            Check.That(ReferenceEquals(c1, c2)).IsTrue();
        }


        [Test]
        public void urns_built_with_local_model()
        {
            var u1 = lightning.interior._private<local_private_node>().my_secret_temp;
            var u2 = PropertyUrn<Temperature>.Build("lightning", "interior", "local_private_node" ,"my_secret_temp");
            Check.That(u1).IsEqualTo(u2);
        }
    }
}
