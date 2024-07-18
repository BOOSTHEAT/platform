using System.Reactive.Subjects;
using System.Threading.Tasks;
using ImpliciX.Designer.ViewModels;
using ImpliciX.DesktopServices;
using Moq;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests.ViewModels;

public class ConditionalMenuViewModelTests
{
  private int _executed;
  private Subject<int> _ints;
  private ConditionalMenuViewModel<int> _sut;

  [Test]
  public void IsDisabledByDefault()
  {
    Check.That(_sut.IsEnabled).IsFalse();
  }

  [Test]
  public void IsDisabledWhenConditionIsNotMet()
  {
    _ints.OnNext(0);
    Check.That(_sut.IsEnabled).IsFalse();
  }

  [Test]
  public void IsEnabledWhenConditionIsMet()
  {
    _ints.OnNext(2);
    Check.That(_sut.IsEnabled).IsTrue();
  }
  
  [Test]
  public void IsExecutedOnOpen()
  {
    _ints.OnNext(2);
    _sut.Open();
    Check.That(_executed).IsEqualTo(2);
    Check.That(_sut.IsEnabled).IsTrue();
  }
  
  [Test]
  public void IsNoLongerAvailableWhenConditionIsNoLongerMetAfterExecution()
  {
    _executed = 2;
    _ints.OnNext(2);
    _sut.Open();
    Check.That(_executed).IsEqualTo(3);
    Check.That(_sut.IsEnabled).IsFalse();
  }

  [SetUp]
  public void Init()
  {
    var concierge = new Mock<ILightConcierge>();
    _ints = new Subject<int>();
    _executed = 1;
    _sut = new ConditionalMenuViewModel<int>(concierge.Object, "", _ints,
      i => i is > 0 and < 3, (m, i) =>
      {
        Check.That(m.IsEnabled).IsFalse();
        _executed++;
        _ints.OnNext(_executed);
        return Task.CompletedTask;
      });
  }
}