using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using ImpliciX.DesktopServices;
using JetBrains.Annotations;
using ReactiveUI;

namespace ImpliciX.Designer.ViewModels;

internal sealed class BuildWebHelpViewModel : ViewModelBase, IDisposable
{
  [NotNull] private readonly CompositeDisposable _compositeDisposable;
  [NotNull] private readonly IConcierge _concierge;

  [CanBeNull] private string _inputFolderPath;
  [CanBeNull] private string _outputFolderPath;

  public BuildWebHelpViewModel(
    [NotNull] IConcierge concierge
  )
  {
    _concierge = concierge ?? throw new ArgumentNullException(nameof(concierge));

    var canExecuteCreateWebHelp =
      this.WhenAnyValue(
          o => o.InputFolderPath,
          o => o.OutputFolderPath
        )
        .Select(paths => !string.IsNullOrEmpty(paths.Item1) && !string.IsNullOrEmpty(paths.Item2));

    CreateWebHelpCommand = ReactiveCommand.CreateFromObservable(
      () => Observable.FromAsync(RunWebHelpAsync).Select(_ => Unit.Default),
      canExecuteCreateWebHelp
    );

    SelectInputFolderCommand = ReactiveCommand.CreateFromObservable(SelectInputFolderAsync);
    SelectOutputFolderCommand = ReactiveCommand.CreateFromObservable(SelectOutputFolderAsync);

    _compositeDisposable = new CompositeDisposable(
      CreateWebHelpCommand,
      RegisterThrownExceptions(CreateWebHelpCommand),
      SelectInputFolderCommand,
      RegisterThrownExceptions(SelectInputFolderCommand),
      SelectOutputFolderCommand,
      RegisterThrownExceptions(SelectOutputFolderCommand)
    );
  }

  [CanBeNull]
  public string InputFolderPath
  {
    get => _inputFolderPath;
    set => this.RaiseAndSetIfChanged(
      ref _inputFolderPath,
      value
    );
  }

  [CanBeNull]
  public string OutputFolderPath
  {
    get => _outputFolderPath;
    set => this.RaiseAndSetIfChanged(
      ref _outputFolderPath,
      value
    );
  }

  [NotNull] public ReactiveCommand<Unit, Unit> CreateWebHelpCommand { get; }

  [NotNull] public ReactiveCommand<Unit, Unit> SelectInputFolderCommand { get; }

  [NotNull] public ReactiveCommand<Unit, Unit> SelectOutputFolderCommand { get; }

  public void Dispose()
  {
    _compositeDisposable.Dispose();
  }

  [NotNull]
  private IDisposable RegisterThrownExceptions(
    [NotNull] IReactiveCommand command
  )
  {
    if (command == null) throw new ArgumentNullException(nameof(command));

    return command.ThrownExceptions.Subscribe(
      ex =>
      {
        new Errors(_concierge).Display(
          ex,
          true
        ).ToObservable();
      }
    );
  }

  [NotNull]
  private IObservable<Unit> SelectInputFolderAsync()
  {
    return Observable.FromAsync(() => SelectFolderAsync("Select Input Folder"))
      .WhereNotNull()
      .ObserveOn(RxApp.MainThreadScheduler)
      .Select(folder => InputFolderPath = folder)
      .Select(_ => Unit.Default);
  }

  [NotNull]
  private IObservable<Unit> SelectOutputFolderAsync()
  {
    return Observable.FromAsync(() => SelectFolderAsync("Select Output Folder"))
      .WhereNotNull()
      .ObserveOn(RxApp.MainThreadScheduler)
      .Select(folder => OutputFolderPath = folder)
      .Select(_ => Unit.Default);
  }

  [NotNull]
  private async Task<string> SelectFolderAsync(
    [CanBeNull] string title
  )
  {
    return (await _concierge.User.OpenFolder(new IUser.FileSelection {Title = title})).Path;
  }


  [NotNull]
  public async Task ShowDialogAsync(
    [NotNull] Window owner
  )
  {
    var window = (Window)Application.Current.DataTemplates.First(dt => dt.Match(this)).Build(this);
    window.DataContext = this;
    await window.ShowDialog(owner);
  }

  private async Task RunWebHelpAsync()
  {
    const string containerName = "bhOxygen";
    const string imageName = "implicix.azurecr.io/oxygen:1.0";

    var docker = _concierge.Docker;
    await docker.Pull(imageName);

    await docker.Launch(
      imageName,
      containerName,
      true,
      null,
      // Cmd = new[] { "ls" },
      // Cmd = new[] { "ls /" },
      // Cmd = new[] { "ls /opt/dita-ot/out" },
      // TODO : 6731 : A reprendre : Ne fonctionne pas comme attendu, "ls" fonctionne, mais même un "ls /" échoue !
      // Cmd = new[] { "./ditaWebhelp.sh webhelp Applications/BHInsight/BHInsight_data_model.ditamap skin.css" },
      new[]
      {
        (
          "/opt/dita-ot/data",
          _inputFolderPath
        ),
        (
          "/opt/dita-ot/out",
          _outputFolderPath)
      }
      // ,User = 1000:1000
    );
  }
}
