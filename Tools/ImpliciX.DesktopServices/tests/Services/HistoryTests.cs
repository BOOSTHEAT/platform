using ImpliciX.DesktopServices.Helpers;
using ImpliciX.DesktopServices.Services;

namespace ImpliciX.DesktopServices.Tests.Services;

public class HistoryTests
{
  private History<string> _sut = null!;
  const string PersistenceKey = "HistoryKey";

  [SetUp]
  public void Init()
  {
    UserSettings.Clear(PersistenceKey);
  }

  private static History<string> Create(int size) => new(size, PersistenceKey);

  [Test]
  public void RecordLastN()
  {
    _sut = Create(3);
    Record("buzz", "fizz", "qix", "bar", "foo");
    Assert.That(_sut, Is.EqualTo(new[] { "foo", "bar", "qix" }));
  }

  [Test]
  public void RecordsAreUnique()
  {
    _sut = Create(10);
    Record("buzz", "fizz", "fizz", "buzz", "buzz", "fizz");
    Assert.That(_sut, Is.EqualTo(new[] { "fizz", "buzz" }));
  }

  [Test]
  public void LatestRecordIsAlwaysFirstEvenIfItWasAlreadyPresent()
  {
    _sut = Create(3);
    Record("qix", "bar", "foo", "qix");
    Assert.That(_sut, Is.EqualTo(new[] { "qix", "foo", "bar" }));
  }

  [Test]
  public void RecordsArePersistent()
  {
    _sut = Create(4);
    Record("qix", "bar", "foo");
    _sut = Create(4);
    Record("buzz", "fizz");
    _sut = Create(10);
    Assert.That(_sut, Is.EqualTo(new[] { "fizz", "buzz", "foo", "bar" }));
  }

  [Test]
  public void RecordsArePublishedOnSubject()
  {
    var log = new List<string[]>();
    _sut = Create(3);
    _sut.Subject.Subscribe(items => log.Add(items.ToArray()));
    Record("qix", "qix", "bar", "foo", "qix", "buzz", "fizz");
    Assert.That(log, Is.EqualTo(new[]
    {
      new[] { "qix" },
      new[] { "bar", "qix" },
      new[] { "foo", "bar", "qix" },
      new[] { "qix", "foo", "bar" },
      new[] { "buzz", "qix", "foo" },
      new[] { "fizz", "buzz", "qix" },
    }));
  }

  private void Record(params string[] items)
  {
    foreach (var item in items)
      _sut.Record(item);
  }
}