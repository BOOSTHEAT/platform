using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using ImpliciX.Language.Model;
using ReactiveUI;

namespace ImpliciX.Designer.ViewModels
{
  public class DefinitionItemViewModel : ViewModelBase
  {
    private DefinitionItemViewModel(string symbol, string description, Urn[] urns, IEnumerable<string> strUrns = null, bool isManuallyActivable = false, object raw = null, bool isVisible = true, bool isTransition = false)
    {
      Contract.Assert(urns.Length > 0, $"No urn defined for {description}");
      Symbol = symbol;
      Description = description;
      
      void Activation(bool isActive, Action<object> action)
      {
        Action<object> a = isActive ? action : _ => { };
        this.RaiseAndSetIfChanged(ref _manualActivation, () => a(raw), nameof(ManualActivation));
        this.RaiseAndSetIfChanged(ref _isActive, isActive, nameof(IsActive));
      }
      DefineUserAction = isManuallyActivable ? Activation : (Action<bool,Action<object>>)((_, __) => { });

      Urns = urns;
      IsVisible = isVisible;
      IsTransition = isTransition;
    }
    
    public static DefinitionItemViewModel CreateTransitionMessage(string description, bool isManuallyActivable, object raw, params Urn[] urns) =>
      new DefinitionItemViewModel("🗣", description, urns, isManuallyActivable:isManuallyActivable, raw:raw, isTransition: true);
    public static DefinitionItemViewModel CreateStateEntry(string description, params Urn[] urns) => new DefinitionItemViewModel("⇉⭘", description, urns);
    public static DefinitionItemViewModel CreateStateDuring(string description, params Urn[] urns) => new DefinitionItemViewModel("⭗", description, urns);
    public static DefinitionItemViewModel CreatePeriodical(string description, params Urn[] urns) => new DefinitionItemViewModel("⥁", description, urns);
    public static DefinitionItemViewModel CreateStateExit(string description, params Urn[] urns) => new DefinitionItemViewModel("⭘⇉", description, urns);
    public static DefinitionItemViewModel CreateStartTimer(string description, params Urn[] urns) => new DefinitionItemViewModel("⇉⏳", description, urns);
    public static DefinitionItemViewModel CreateTimeout(string description, params Urn[] urns) => 
      new DefinitionItemViewModel("⏳⇉", description, urns);
    public static DefinitionItemViewModel CreateTransitionTimeout(string description, params Urn[] urns) => 
      new DefinitionItemViewModel("⏳⇉", description, urns, isTransition: true);
    public static DefinitionItemViewModel CreateCondition(string description, params Urn[] urns) => 
      new DefinitionItemViewModel("😺", description, urns); //😺😻🙀🙉🙈🙊🗣
    public static DefinitionItemViewModel CreateTransitionCondition(string description, IEnumerable<string> strUrns, params Urn[] urns) => 
      new DefinitionItemViewModel("😺", description, urns, strUrns, isTransition: true); //😺😻🙀🙉🙈🙊🗣
    public string Symbol { get; private set; }
    public string Description { get; private set; }
    public IEnumerable<Urn> Urns { get; }

    private bool _isActive = false;
    public bool IsActive => _isActive;

    public bool _isVisible;
    public bool IsVisible
    {
      get => _isVisible;
      set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    public bool IsTransition { get; }

    private Action _manualActivation = null;
    public Action ManualActivation => _manualActivation;

    public readonly Action<bool, Action<object>> DefineUserAction;
  }
}
