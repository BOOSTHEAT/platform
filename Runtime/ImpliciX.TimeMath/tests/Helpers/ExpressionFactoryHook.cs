using System;
using System.Linq;
using DynamicExpresso;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Model;
using TechTalk.SpecFlow;

namespace ImpliciX.TimeMath.Tests.Helpers;

[Binding]
public class ExpressionFactoryHook
{
  [BeforeScenario]
  public static void BeforeScenario(ScenarioContext scenarioContext)
  {
    scenarioContext.Set(new ExpressionsFactory());
  }
}

  
internal class ExpressionsFactory : Interpreter
{
  public ExpressionsFactory()
  {
    Reference(typeof(Metrics));
  }

  public Urn GetUrn(string expression)
  {
    var parts = expression.Split(':');
    return parts.Length == 1 ? Eval<Urn>(expression) : MetricUrn.Build(parts);
  }

  public object GetPropertyUrnValue(Urn urn, string expression)
  {
    if (urn is MetricUrn)
      return Convert.ToSingle(Eval(expression));
    var propertyType = urn.GetType().GetGenericArguments().First();
    if (propertyType.IsEnum)
      return Enum.Parse(propertyType, expression);
    return Eval(expression, propertyType);
  }

  public IDataModelValue GetProperty(Urn urn, object value, TimeSpan at)
  {
    dynamic helper = Activator.CreateInstance(typeof(PropertyHelper<>).MakeGenericType(value.GetType()));
    return helper!.Create(urn, value, at);
  }
  class PropertyHelper<T>
  {
    public Property<T> Create(Urn urn, object value, TimeSpan at) => Property<T>.Create((PropertyUrn<T>)urn, (T)value, at);
  }

  public IMetricDefinition CreateMetricDefinition(string urn, string expression)
  {
    SetVariable(urn, MetricUrn.Build(urn));
    return Eval<IMetricDefinition>("Metrics." + expression);
  }

  public ExpressionsFactory AddType<T>()
  {
    Reference(typeof(T));
    return this;
  }

  public ExpressionsFactory AddProperty<T>(string urn)
  {
    var urnValue = CreateUrn<T>(urn);
    SetVariable(urn, urnValue);
    return this;
  }

  public static PropertyUrn<T> CreateUrn<T>(string urn)
  {
    var urnType = typeof(LocalUrn<>).MakeGenericType(typeof(T));
    var urnValue = Activator.CreateInstance(urnType, urn);
    return (PropertyUrn<T>) urnValue;
  }

  class LocalUrn<T> : PropertyUrn<T>
  {
    public LocalUrn(string value) : base(value)
    {
    }
  }
}

