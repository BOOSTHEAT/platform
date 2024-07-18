using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImpliciX.Designer.Dialogs;
using ImpliciX.DesktopServices;
using MsBox.Avalonia.Enums;
using NFluent;
using NUnit.Framework;
using ReactiveUI;

namespace ImpliciX.Designer.Tests;

public class MessageBoxesTests
{
  private static TestCaseData[] _data = new[]
  {
    new TestCaseData(IUser.StandardButtons(IUser.ChoiceType.Ok), ButtonEnum.Ok),
    new TestCaseData(IUser.StandardButtons(IUser.ChoiceType.Ok, IUser.ChoiceType.Cancel), ButtonEnum.OkCancel),
    new TestCaseData(IUser.StandardButtons(IUser.ChoiceType.Yes, IUser.ChoiceType.No), ButtonEnum.YesNo),
    new TestCaseData(IUser.StandardButtons(IUser.ChoiceType.Yes, IUser.ChoiceType.No, IUser.ChoiceType.Cancel), ButtonEnum.YesNoCancel),
    new TestCaseData(IUser.StandardButtons(IUser.ChoiceType.Ok, IUser.ChoiceType.Abort), ButtonEnum.OkAbort),
    new TestCaseData(IUser.StandardButtons(IUser.ChoiceType.Yes, IUser.ChoiceType.No, IUser.ChoiceType.Abort), ButtonEnum.YesNoAbort),
  };

  [TestCaseSource(nameof(_data))]
  public async Task StandardMessageBoxInputs(IEnumerable<IUser.Choice> choices, ButtonEnum expected)
  {
    var sut = new MessageBoxes();
    var actual = SetupInteraction(sut.MessageBox.Standard.ModalChild, ButtonResult.None);
    var box = new IUser.Box
    {
      Title = "The Title",
      Message = "The Message",
      Icon = IUser.Icon.Success,
      Buttons = choices
    };
    await sut.Show(box);
    Check.That(actual.Input!.ContentTitle).IsEqualTo("The Title");
    Check.That(actual.Input!.ContentMessage).IsEqualTo("The Message");
    Check.That(actual.Input!.Icon).IsEqualTo(Icon.Success);
    Check.That(actual.Input!.ButtonDefinitions).IsEqualTo(expected);
  }
  
  [TestCase(ButtonResult.Ok, IUser.ChoiceType.Ok)]
  [TestCase(ButtonResult.Cancel, IUser.ChoiceType.Cancel)]
  [TestCase(ButtonResult.Yes, IUser.ChoiceType.Yes)]
  [TestCase(ButtonResult.No, IUser.ChoiceType.No)]
  [TestCase(ButtonResult.Abort, IUser.ChoiceType.Abort)]
  [TestCase(ButtonResult.None, IUser.ChoiceType.None)]
  public async Task StandardMessageBoxResults(ButtonResult sendBack, IUser.ChoiceType expected)
  {
    var sut = new MessageBoxes();
    SetupInteraction(sut.MessageBox.Standard.ModalChild, sendBack);
    var result = await sut.Show(new IUser.Box());
    Check.That(result).IsEqualTo(expected);
  }
  
  [TestCase("Ok", IUser.ChoiceType.Ok)]
  [TestCase("Cancel", IUser.ChoiceType.Cancel)]
  [TestCase("Foo", IUser.ChoiceType.Custom1)]
  [TestCase("Bar", IUser.ChoiceType.Custom2)]
  [TestCase("Qix", IUser.ChoiceType.Custom3)]
  public async Task CustomMessageBox(string userChoice, IUser.ChoiceType expectedChoice)
  {
    var sut = new MessageBoxes();
    var actual = SetupInteraction(sut.MessageBox.Custom.ModalChild, userChoice);
    var result = await sut.Show(new IUser.Box
    {
      Title = "The Title",
      Message = "The Message",
      Icon = IUser.Icon.Setting,
      Buttons = IUser.ChoiceType.Ok.With(isDefault:true)
                +IUser.ChoiceType.Cancel.With(isCancel:true)
                +IUser.ChoiceType.Custom1.With(text:"Foo")
                +IUser.ChoiceType.Custom2.With(text:"Bar")
                +IUser.ChoiceType.Custom3.With(text:"Qix")
    });
    Check.That(actual.Input!.ContentTitle).IsEqualTo("The Title");
    Check.That(actual.Input!.ContentMessage).IsEqualTo("The Message");
    Check.That(actual.Input!.Icon).IsEqualTo(Icon.Setting);
    Check.That(actual.Input!.ButtonDefinitions.Select(b => (b.Name,b.IsDefault,b.IsCancel)))
      .IsEqualTo(new []
      {
        ("Ok", true, false),
        ("Cancel", false, true),
        ("Foo", false, false),
        ("Bar", false, false),
        ("Qix", false, false),
      });
    Check.That(result).IsEqualTo(expectedChoice);
  }
  
  [TestCase("Cancel", IUser.ChoiceType.Cancel)]
  [TestCase("Abort", IUser.ChoiceType.Abort)]
  public async Task CustomMessageBoxOnStandardButtons(string userChoice, IUser.ChoiceType expectedChoice)
  {
    var sut = new MessageBoxes();
    var actual = SetupInteraction(sut.MessageBox.Custom.ModalChild, userChoice);
    var result = await sut.Show(new IUser.Box
    {
      Title = "The Title",
      Message = "The Message",
      Icon = IUser.Icon.Setting,
      Buttons = IUser.StandardButtons(IUser.ChoiceType.Cancel, IUser.ChoiceType.Abort)
    });
    Check.That(actual.Input!.ContentTitle).IsEqualTo("The Title");
    Check.That(actual.Input!.ContentMessage).IsEqualTo("The Message");
    Check.That(actual.Input!.Icon).IsEqualTo(Icon.Setting);
    Check.That(actual.Input!.ButtonDefinitions.Select(b => (b.Name,b.IsDefault,b.IsCancel)))
      .IsEqualTo(new []
      {
        ("Cancel", false, false),
        ("Abort", false, false),
      });
    Check.That(result).IsEqualTo(expectedChoice);
  }
  
  [TestCase("Vazy", "pwd1", IUser.Icon.Warning, Icon.Warning)]
  [TestCase("Zyva","pwd2", IUser.Icon.Info, Icon.Info)]
  public async Task EnterPassword(string okText, string password, IUser.Icon icon, Icon expectedIcon)
  {
    var sut = new MessageBoxes();
    var actual = SetupInteraction(sut.MessageBox.Password.ModalChild, (okText, password));
    var result = await sut.EnterPassword(new IUser.Box
    {
      Title = "The Title",
      Message = "The Message",
      Icon = icon,
      Buttons = IUser.ChoiceType.Custom1.With(text:okText, isDefault:true) + IUser.ChoiceType.Cancel.With(isCancel:true)
    });
    Check.That(actual.Input!.ContentTitle).IsEqualTo("The Title");
    Check.That(actual.Input!.ContentMessage).IsEqualTo("The Message");
    Check.That(actual.Input!.Icon).IsEqualTo(expectedIcon);
    Check.That(actual.Input!.ButtonDefinitions.Select(b => (b.Name,b.IsDefault,b.IsCancel)))
      .IsEqualTo(new []
      {
        (okText, true, false),
        ("Cancel", false, true)
      });
    Check.That(result.Item1).IsEqualTo(IUser.ChoiceType.Custom1);
    Check.That(result.Item2).IsEqualTo(password);
  }
  
  private static InteractionResult<TI> SetupInteraction<TI, TO>(Interaction<TI, TO> interaction, TO sendBack)
  {
    var result = new InteractionResult<TI>();
    interaction.RegisterHandler(x =>
    {
      result.Input = x.Input;
      x.SetOutput(sendBack);
    });
    return result;
  }

  private class InteractionResult<T>
  {
    public T? Input = default(T);
  }
 }