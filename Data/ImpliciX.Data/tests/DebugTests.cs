using ImpliciX.Language.Core;
using NUnit.Framework;

namespace ImpliciX.Data.Tests
{
  [TestFixture]
  public class DebugTests
  {
    [Test]
    [Category("ExcludeFromCI")]
    [Category("ExcludeWindows")]
    public void should_raise_an_contract_exception()
    {
      int input = -1;
      Assert.Throws<ContractException>(() => Debug.PreCondition(()=>input >= 0, ()=>"Input should be a positive integer."));
    }

    [Test]
    public void should_pass_contract_validation()
    {
      int input = 0;
      Assert.DoesNotThrow(()=>Debug.PreCondition(()=>input >= 0, ()=>"Input should be a positive integer."));
    }
  }
}