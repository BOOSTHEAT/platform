using ImpliciX.Designer.DesignViewModels;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests.DesignViewModels;

public class LivePropertiesDataContextTest
{
  [Test]
  public void InstantiationFromScratchShouldWork()
  {
    var _sut = new LivePropertiesDataContext();
    Assert.That(
      _sut,
      !Is.Null
    );
    Assert.That(
      _sut.Items,
      !Is.Empty
    );
  }
}
