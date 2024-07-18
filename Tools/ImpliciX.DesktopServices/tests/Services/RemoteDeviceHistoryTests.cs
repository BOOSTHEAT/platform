using ImpliciX.DesktopServices.Helpers;
using ImpliciX.DesktopServices.Services;

namespace ImpliciX.DesktopServices.Tests.Services;

public class RemoteDeviceHistoryTests
{
  private RemoteDeviceHistory _sut = null!;

  [SetUp]
  public void Init()
  {
    UserSettings.Clear(RemoteDeviceHistory.PersistenceKey);
    _sut = new();
  }
  
  [Test]
  public void SuggestThreeLatestConnectionsByDefault()
  {
    SimulatePastConnections("buzz", "fizz", "qix", "bar", "foo", "10.33.45.98");
    var suggestions = _sut.Get(string.Empty);
    Assert.That(suggestions, Is.EqualTo(new [] {"10.33.45.98", "foo", "bar"}));
  }

  [Test]
  public void SuggestWithMatchingTestIfSupplied()
  {
    SimulatePastConnections("buzz", "fizz", "qix", "bar", "foo", "10.33.45.98");
    var suggestions = _sut.Get("z");
    Assert.That(suggestions, Is.EqualTo(new [] {"fizz", "buzz"}));
  }

  [Test]
  public void SuggestionsAreUnique()
  {
    SimulatePastConnections("buzz", "fizz", "qix", "bar", "foo", "10.33.45.98");
    SimulatePastConnections("buzz", "fizz", "qix", "bar", "foo", "10.33.45.98");
    var suggestions = _sut.Get("z");
    Assert.That(suggestions, Is.EqualTo(new [] {"fizz", "buzz"}));
  }

  [Test]
  public void KeepHundredConnectionHistory()
  {
    SimulatePastConnections("yolo1", "yolo2", "yolo3", "yolo4");
    for (int i = 0; i < 98; i++)
    {
      SimulatePastConnections($"plop{i}");
    }
    var suggestions = _sut.Get("yo");
    Assert.That(suggestions, Is.EqualTo(new [] {"yolo4", "yolo3"}));
  }
  
  [Test]
  public void SuggestionsArePersistent()
  {
    SimulatePastConnections("buzz", "fizz");
    _sut = new();
    SimulatePastConnections("qix", "bar", "foo", "10.33.45.98");
    _sut = new();
    var suggestions = _sut.Get("f");
    Assert.That(suggestions, Is.EqualTo(new [] {"foo", "fizz"}));
  }

  
  private void SimulatePastConnections(params string[] targets)
  {
    foreach (var target in targets)
      _sut.Add(target);
  }
}