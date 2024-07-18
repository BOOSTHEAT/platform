using ImpliciX.Language.Model;

namespace ImpliciX.ReferenceApp.Model.Enums;

[ValueObject]
public enum States {
    Disabled = 0,
    StandBy = 1,
    Running = 2,
    Failure = -1, 
    Other = -2
}