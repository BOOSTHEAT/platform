using System;
using System.Linq;
using Avalonia.Controls;
using ImpliciX.Language.Model;
using ReactiveUI;

namespace ImpliciX.Designer.ViewModels;

public abstract class LivePropertyViewModel : ViewModelBase
{
  private static readonly string[] SiUnits =
  {
    "Temperature",
    "Length",
    "Energy",
    "Duration",
    "Flow",
    "Mass",
    "Power",
    "Pressure",
    "Torque",
    "Volume",
    "Voltage",
    "AngularSpeed",
    "RotationalSpeed",
    "DifferentialTemperature",
    "DifferentialPressure"
  };

  private readonly LivePropertiesViewModel _parent;
  private bool _isEditable;
  private bool _isInModel;

  protected LivePropertyViewModel(
    Urn urn,
    LivePropertiesViewModel parent,
    bool inModel
  )
  {
    Urn = urn;
    _parent = parent;
    IsEditable = CheckEditability();
    IsInModel = inModel;
    Classes = new Classes();
    var type = urn.GetType();
    EnumValues = Array.Empty<string>();
    foreach (var typeGenericTypeArgument in type.GenericTypeArguments)
    {
      Classes.Add(typeGenericTypeArgument.Name);
      if (typeGenericTypeArgument.IsEnum) EnumValues = Enum.GetNames(typeGenericTypeArgument);
    }

    // if (Classes.Contains("Percentage")) IsPercentage = true;
    IsFloat = IsFloatUrn(urn);
    IsPercentage = IsPercentageUrn(urn);
    foreach (var typeGenericTypeArgument in type.GenericTypeArguments)
      AsUnit = AsUnit || ItSIUnit(typeGenericTypeArgument) || IsPercentage;
    IsEnumeration = EnumValues.Length > 0;
  }

  public bool IsPercentage { get ; }

  public Urn Urn { get; set; }

  public Action SetNewValue { get; set; }

  public Classes Classes { get; set; }

  public bool IsUnit => Classes.Count > 0;

  public bool IsFloat { get ; }

  public bool IsEditable
  {
    get => _isEditable;
    set => this.RaiseAndSetIfChanged(
      ref _isEditable,
      value
    );
  }

  public bool IsInModel
  {
    get => _isInModel;
    set => this.RaiseAndSetIfChanged(
      ref _isInModel,
      value
    );
  }

  public bool AsUnit { get ; }

  public bool IsEnumeration { get ; }
  public string[] EnumValues { get ; }
  public virtual bool IsValidated => true;
  public bool IsFunction => IsFunctionDefinitionUrn(Urn);

  private static bool IsFunctionDefinitionUrn(
    Urn urn
  )
  {
    var urnType = urn.GetType();
    return urnType.GenericTypeArguments.Length == 1 && urnType.GenericTypeArguments[0] == typeof(FunctionDefinition);
  }

  private static bool IsEnumerationUrn(
    Urn urn
  )
  {
    var urnType = urn.GetType();
    return urnType.GenericTypeArguments.Length == 1 && urnType.GenericTypeArguments[0].IsEnum;
  }

  private static bool IsPercentageUrn(
    Urn urn
  )
  {
    var urnType = urn.GetType();
    return urnType.GenericTypeArguments
      .Any(
        typeGenericTypeArgument =>
          typeGenericTypeArgument.Name.Equals("Percentage")
      );
  }

  private static bool IsFloatUrn(
    Urn urn
  )
  {
    var urnType = urn.GetType();
    return urnType.GenericTypeArguments
      .Any(
        typeGenericTypeArgument =>
          typeGenericTypeArgument.GetInterfaces()
            .Select(InterfaceType => InterfaceType.FullName)
            .Contains("ImpliciX.Language.Model.IFloat")
      );
  }

  private bool ItSIUnit(
    Type typeGenericTypeArgument
  )
  {
    return SiUnits.Contains(typeGenericTypeArgument.Name);
  }

  public static LivePropertyViewModel CreateLivePropertyViewModel(
    LivePropertiesViewModel.PropertyInfo pi,
    LivePropertiesViewModel parent
  )
  {
    var urn = pi.Definition.Match(
      () => Urn.BuildUrn(Urn.Deconstruct(pi.Name)),
      urn => urn
    );

    return IsFunctionDefinitionUrn(urn)
      ? new LiveFunctionDefinitionViewModel(
        urn,
        parent,
        pi.IsInModel
      )
      : IsEnumerationUrn(urn)
        ? new LiveEnumPropertyViewModel(
          urn,
          parent,
          pi.IsInModel
        )
        : IsPercentageUrn(urn)
          ? new LivePercentagePropertyViewModel(
            urn,
            parent,
            pi.IsInModel
          )
          : IsFloatUrn(urn)
            ? new LiveFloatPropertyViewModel(
              urn,
              parent,
              pi.IsInModel
            )
            : new LiveTextPropertyViewModel(
              urn,
              parent,
              pi.IsInModel
            );
  }

  private bool CheckEditability()
  {
    return Urn is ISettingUrn;
  }

  public void SearchUrn()
  {
    _parent.Search = Urn.Value;
  }
}
