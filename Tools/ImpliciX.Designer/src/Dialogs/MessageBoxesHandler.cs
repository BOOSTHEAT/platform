using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using ReactiveUI;

namespace ImpliciX.Designer.Dialogs;

public class MessageBoxesHandler
{
  public MessageBoxesHandler<MessageBoxCustomParams, string> Custom { get; } =
    new(MessageBoxManager.GetMessageBoxCustom);

  public MessageBoxesHandler<MessageBoxStandardParams, ButtonResult> Standard { get; } =
    new(MessageBoxManager.GetMessageBoxStandard);

  public MessageBoxesHandlerWithInput<MessageBoxCustomParams, string> Input { get; } =
    new(MessageBoxManager.GetMessageBoxCustom);

  public MessageBoxesHandlerWithInput<MessageBoxCustomParams, string> Password { get; } =
    new(GetMessageBoxCustomPassword);

  public void RegisterOn(
    TopLevel topLevel
  )
  {
    var registration = new CompositeDisposable(
      Custom.RegisterOn(topLevel),
      Standard.RegisterOn(topLevel),
      Input.RegisterOn(topLevel),
      Password.RegisterOn(topLevel)
    );
    topLevel.Closed += (
      sender,
      args
    ) => registration.Dispose();
  }

  public static IMsBox<string> GetMessageBoxCustomPassword(
    MessageBoxCustomParams @params
  )
  {
    var viewModel = new PasswordViewModel(@params);
    var view = new PasswordView
    {
      DataContext = viewModel
    };
    view.Focus();
    return new MsBox<PasswordView, PasswordViewModel, string>(
      view,
      viewModel
    );
  }
}

public abstract class MessageBoxesHandler<TI, TC, TO>
{
  private readonly Func<TI, IMsBox<TC>> _createWindow;

  protected MessageBoxesHandler(
    Func<TI, IMsBox<TC>> createWindow
  )
  {
    _createWindow = createWindow;
    Free = new Interaction<TI, TO>();
    ModalChild = new Interaction<TI, TO>();
  }

  public Interaction<TI, TO> Free { get; }
  public Interaction<TI, TO> ModalChild { get; }

  internal IDisposable RegisterOn(
    TopLevel topLevel
  )
  {
    return new CompositeDisposable(this.RegisterHandler(
        Free,
        w => w.ShowAsync()
      ), this.RegisterHandler(
        ModalChild,
        w => w.ShowAsPopupAsync(topLevel)
      )
    );
  }

  private IDisposable RegisterHandler(
    Interaction<TI, TO> interaction,
    Func<IMsBox<TC>, Task<TC>> display
  )
  {
    return interaction.RegisterHandler(
      async context =>
      {
        var window = _createWindow(context.Input);
        var result = await display(window);
        context.SetOutput(this.CreateResult(
            window,
            result
          )
        );
      }
    );
  }

  protected abstract TO CreateResult(
    IMsBox<TC> window,
    TC choice
  );
}

public class MessageBoxesHandler<TI, TO> : MessageBoxesHandler<TI, TO, TO>
{
  public MessageBoxesHandler(
    Func<TI, IMsBox<TO>> createWindow
  ) : base(createWindow)
  {
  }

  protected override TO CreateResult(
    IMsBox<TO> window,
    TO choice
  )
  {
    return choice;
  }
}

public class MessageBoxesHandlerWithInput<TI, TO> : MessageBoxesHandler<TI, TO, (TO, string)>
{
  public MessageBoxesHandlerWithInput(
    Func<TI, IMsBox<TO>> createWindow
  ) : base(createWindow)
  {
  }

  protected override (TO, string) CreateResult(
    IMsBox<TO> window,
    TO choice
  )
  {
    return (choice, window.InputValue);
  }
}
