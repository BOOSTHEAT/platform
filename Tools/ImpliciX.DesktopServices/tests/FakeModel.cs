using ImpliciX.Language.Model;

namespace ImpliciX.DesktopServices.Tests;

public class root : RootModelNode
{
    public root() : base(nameof(root))
    {
    }
    public static fakeSubsystem fakeSubsystem => new fakeSubsystem(new root());
    
    public static PropertyUrn<DummyEnum> dummy => PropertyUrn<DummyEnum>.Build(nameof(root), nameof(dummy));
    
    public static PropertyUrn<Temperature> t1 => PropertyUrn<Temperature>.Build(nameof(root), nameof(t1)); 
    
    public static PropertyUrn<SubsystemState> state => PropertyUrn<SubsystemState>.Build(nameof(root), nameof(state));

}

[ValueObject]
public enum DummyEnum
{
    Foo=-1,
    Bar=0,
}

public enum DummyState
{
    A = 1,
    B = 2,
}