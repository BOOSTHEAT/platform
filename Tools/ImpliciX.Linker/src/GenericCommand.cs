using System.Collections;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace ImpliciX.Linker;

public abstract class GenericCommand : ICommandHandler
{
  protected GenericCommand(Command command)
  {
    command.Handler = this;
  }
  
  public Task<int> InvokeAsync(InvocationContext context)
  {
    var options = context.ParseResult.CommandResult.Command.Options;
    var args =
      (from option in options
      let value = GetValueForOption(context, option)
      where value != null
      select (option.Name,value)).ToDictionary(x => x.Name, x => x.value! );
    return Task.Run(() => Execute(args));
  }

  private static object? GetValueForOption(InvocationContext context, IOption o)
  {
    var value = context.ParseResult.GetValueForOption((Option)o);
    if (value == null)
      return null;
    static object GetTypedValue(IOption o, object value) =>
      value.GetType() == o.ValueType ? value : Activator.CreateInstance(o.ValueType, value)!;
    return ((Option)o).Arity.MaximumNumberOfValues > 1
      ? ConvertEnumerableToTypedList(o.ValueType, ((IEnumerable<object>)value).Select(x => GetTypedValue(o, x)))
      : GetTypedValue(o, value);
  }

  private static IList ConvertEnumerableToTypedList(Type t, IEnumerable xs)
  {
    var l = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(t))!;
    foreach(var x in xs)
      l.Add(x);
    return l;
  }


  protected abstract int Execute(Dictionary<string, object> arguments);
}