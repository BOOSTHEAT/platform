using System;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.TestUtilities;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Control.Tests.Fixtures.ExpectResultingEvents
{
  [TestFixture]
  public class AssignmentTests
  {
    [Test]
    public void set_property_into_property()
    {
      var sut = new UserDefinedControlSystem(DomainEventFactory(TimeSpan.Zero), new AssignmentSubsystem());
      var resultingEvents = sut.PlayEvents(
        EventPropertyChanged(assignment.percentage1, 0.8f, TimeSpan.Zero),
        EventPropertyChanged(assignment.gas1, LocalGasType.G20, TimeSpan.Zero),
        EventPropertyChanged(assignment.function1, FunctionDefinition.From(new []{("a0",1.5f),("a1",0.5f)}).GetValueOrDefault(), TimeSpan.Zero)
      );
      Check.That(resultingEvents).ContainsExactly(
        EventPropertyChanged(assignment.percentage2, 0.8f, TimeSpan.Zero),
        EventPropertyChanged(assignment.gas2, LocalGasType.G20, TimeSpan.Zero),
        EventPropertyChanged(assignment.function2, FunctionDefinition.From(new []{("a0",1.5f),("a1",0.5f)}).GetValueOrDefault(), TimeSpan.Zero)
      );
    }
    
    [SetUp]
    public void Init()
    {
      EventsHelper.ModelFactory = new ModelFactory(typeof(assignment).Assembly);
    }
  }
  
  public class AssignmentSubsystem : SubSystemDefinition<AssignmentSubsystem.State>
  {
    public enum State
    {
      A
    }

    public AssignmentSubsystem()
    {
      // @formatter:off
      Subsystem(assignment.x)
        .Initial(State.A)
        .Define(State.A)
          .OnState
            .Set(assignment.percentage2, assignment.percentage1)
            .Set(assignment.gas2, assignment.gas1)
            .Set(assignment.function2, assignment.function1)
        ;
      // @formatter:on
    }
  }
  
  public class assignment : SubSystemNode
  {
    public static assignment x => new assignment();
    public static PropertyUrn<Percentage> percentage1 =>PropertyUrn<Percentage>.Build(x.Urn, nameof(percentage1));
    public static PropertyUrn<Percentage> percentage2 =>PropertyUrn<Percentage>.Build(x.Urn, nameof(percentage2));
    public static PropertyUrn<LocalGasType> gas1 =>PropertyUrn<LocalGasType>.Build(x.Urn, nameof(gas1));
    public static PropertyUrn<LocalGasType> gas2 =>PropertyUrn<LocalGasType>.Build(x.Urn, nameof(gas2));
    public static PropertyUrn<FunctionDefinition> function1 =>PropertyUrn<FunctionDefinition>.Build(x.Urn, nameof(function1));
    public static PropertyUrn<FunctionDefinition> function2 =>PropertyUrn<FunctionDefinition>.Build(x.Urn, nameof(function2));

    public assignment() : base(nameof(assignment), new examples())
    {
    }
  }
  
  [ValueObject]
  public enum LocalGasType
  {
    G20 = 20,
    G25 = 25,
    G31 = 31
  }

}