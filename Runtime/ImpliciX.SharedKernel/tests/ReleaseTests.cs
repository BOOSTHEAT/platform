using ImpliciX.Language.Core;
using NUnit.Framework;

namespace ImpliciX.SharedKernel.Tests
{
    [TestFixture]
    public class ReleaseTests
    {
        [Test]
        public void should_not_pass_release_validation()
        {
            int input = -1;
            Assert.Throws<ContractException>(()=>Release.Ensure(()=>input >= 0, ()=>"Input should be a positive integer."));
        }
        
        
        [Test]
        public void should_pass_release_validation()
        {
            int input = 0;
            Assert.DoesNotThrow(()=>Release.Ensure(()=>input >= 0, ()=>"Input should be a positive integer."));
        }
    }
}