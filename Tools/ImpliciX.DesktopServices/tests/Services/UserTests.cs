using NFluent;

namespace ImpliciX.DesktopServices.Tests.Services;

public class UserTests
{
   
  [Test]
  public void ChoiceCombination()
  {
    var choices = new[] { IUser.ChoiceType.Ok, IUser.ChoiceType.Cancel };
    Check.That(choices.Is(IUser.ChoiceType.Ok | IUser.ChoiceType.Cancel)).IsTrue();
    Check.That(choices.Is(IUser.ChoiceType.Ok)).IsFalse();
    Check.That(choices.Contains(IUser.ChoiceType.Ok)).IsTrue();
    Check.That(choices.Contains(IUser.ChoiceType.Abort)).IsFalse();
  }

}