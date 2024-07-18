using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Core;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;

namespace ImpliciX.Designer.Dialogs;

public class MessageBoxes
{
  public readonly MessageBoxesHandler MessageBox;

  public MessageBoxes()
  {
    MessageBox = new MessageBoxesHandler();
  }

  public void RegisterOn(TopLevel topLevel)
  {
    MessageBox.RegisterOn(topLevel);
  }

  public async Task<IUser.ChoiceType> Show(IUser.Box box)
  {
    var buttons = ComputeDefinitions(box.Buttons.ToArray());
    return buttons.Item1.IsSome || !buttons.Item2.Any()
      ? await ShowStandardDialog(box, buttons)
      : await ShowCustomDialog(box, buttons);
  }

  private async Task<IUser.ChoiceType> ShowStandardDialog(
    IUser.Box box,
    (Option<ButtonEnum>, IEnumerable<(IUser.ChoiceType, ButtonDefinition)>) buttons
  )
  {
    var standardParams = new MessageBoxStandardParams
    {
      ContentTitle = box.Title,
      ContentMessage = box.Message,
      Icon = GetIcon(box.Icon),
      ButtonDefinitions = buttons.Item1.GetValue()
    };
    var result = await MessageBox.Standard.ModalChild.Handle(standardParams);
    return result switch
    {
      ButtonResult.Ok => IUser.ChoiceType.Ok,
      ButtonResult.Yes => IUser.ChoiceType.Yes,
      ButtonResult.No => IUser.ChoiceType.No,
      ButtonResult.Abort => IUser.ChoiceType.Abort,
      ButtonResult.Cancel => IUser.ChoiceType.Cancel,
      ButtonResult.None => IUser.ChoiceType.None,
      _ => throw new ArgumentOutOfRangeException()
    };
  }

  private async Task<IUser.ChoiceType> ShowCustomDialog(
    IUser.Box box,
    (Option<ButtonEnum>, IEnumerable<(IUser.ChoiceType, ButtonDefinition)>) buttons
  )
  {
    var customParams = new MessageBoxCustomParams
    {
      ContentTitle = box.Title,
      ContentMessage = box.Message,
      Icon = GetIcon(box.Icon),
      ButtonDefinitions = buttons.Item2.Select(x => x.Item2)
    };
    var cresult = await MessageBox.Custom.ModalChild.Handle(customParams);
    var choice = buttons.Item2.Single(b => b.Item2.Name == cresult).Item1;
    return choice;
  }

  private static Icon GetIcon(IUser.Icon icon)
  {
    return icon switch
    {
      IUser.Icon.None => Icon.None,
      IUser.Icon.Error => Icon.Error,
      IUser.Icon.Info => Icon.Info,
      IUser.Icon.Setting => Icon.Setting,
      IUser.Icon.Stop => Icon.Stop,
      IUser.Icon.Success => Icon.Success,
      IUser.Icon.Warning => Icon.Warning,
      _ => throw new ArgumentOutOfRangeException(nameof(icon), icon, null)
    };
  }

  private static (Option<ButtonEnum>, IEnumerable<(IUser.ChoiceType, ButtonDefinition)>)
    ComputeDefinitions(IUser.Choice[] boxButtons)
  {
    return (boxButtons.Select(x => (int)x.Type).Sum() switch
    {
      1 => Option<ButtonEnum>.Some(ButtonEnum.Ok),
      3 => Option<ButtonEnum>.Some(ButtonEnum.OkCancel),
      12 => Option<ButtonEnum>.Some(ButtonEnum.YesNo),
      14 => Option<ButtonEnum>.Some(ButtonEnum.YesNoCancel),
      17 => Option<ButtonEnum>.Some(ButtonEnum.OkAbort),
      28 => Option<ButtonEnum>.Some(ButtonEnum.YesNoAbort),
      _ => Option<ButtonEnum>.None()
    }, boxButtons.Select(c => (c.Type, new ButtonDefinition
    {
      Name = c.Text ?? c.Type.ToString(),
      IsDefault = c.IsDefault,
      IsCancel = c.IsCancel
    })));
  }

  public async Task<(IUser.ChoiceType, string)> EnterPassword(IUser.Box box)
  {
    var buttons = ComputeDefinitions(box.Buttons.ToArray());
    var customParams = new MessageBoxCustomParams
    {
      ContentTitle = box.Title,
      ContentMessage = box.Message,
      Icon = GetIcon(box.Icon),
      ButtonDefinitions = buttons.Item2.Select(x => x.Item2),
      InputParams = new InputParams()
    };
    var result = await MessageBox.Password.ModalChild.Handle(customParams);
    var choice = buttons.Item2.Single(b => b.Item2.Name == result.Item1).Item1;
    return (choice, result.Item2);
  }
}
