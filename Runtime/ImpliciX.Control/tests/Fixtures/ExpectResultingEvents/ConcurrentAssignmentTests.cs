using System;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.TestUtilities;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using static ImpliciX.Language.Control.Condition;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Control.Tests.Fixtures.ExpectResultingEvents
{
  [TestFixture]
  public class ConcurrentAssignmentTests
  {
    [Test]
    public void external_property_change_after_on_entry()
    {
      var sut = new UserDefinedControlSystem(DomainEventFactory(TimeSpan.Zero), new ConcurrentAssignmentSubsystem());
      var resultingEvents = sut.PlayEvents(
        EventPropertyChanged(concurrent.starter, 0.2f, TimeSpan.FromSeconds(1)),
        EventPropertyChanged(concurrent.value, 0.005f, TimeSpan.FromSeconds(1))
      );
      Check.That(resultingEvents).Contains(
        EventPropertyChanged(new(Urn,object)[]
        {
          (concurrent.subsystem.state, SubsystemState.Create(ConcurrentAssignmentSubsystem.State.B)),
          (concurrent.value, 1.0f)
        }, TimeSpan.Zero),
        EventPropertyChanged(new(Urn,object)[]
        {
          (concurrent.subsystem.state, SubsystemState.Create(ConcurrentAssignmentSubsystem.State.C)),
          (concurrent.check, 0.005f)
        }, TimeSpan.Zero)
      );
    }
    
    [Test]
    public void external_property_change_before_on_entry()
    {
      var sut = new UserDefinedControlSystem(DomainEventFactory(TimeSpan.Zero), new ConcurrentAssignmentSubsystem());
      var resultingEvents = sut.PlayEvents(
        EventPropertyChanged(new(Urn,object)[]
        {
          (concurrent.starter, 0.2f),
          (concurrent.value, 0.005f)
        }, TimeSpan.FromSeconds(1))
      );
      Check.That(resultingEvents).Contains(
        EventPropertyChanged(new(Urn,object)[]
        {
          (concurrent.subsystem.state, SubsystemState.Create(ConcurrentAssignmentSubsystem.State.B)),
          (concurrent.value, 1.0f)
        }, TimeSpan.Zero)
      );
    }
    
    [SetUp]
    public void Init()
    {
      EventsHelper.ModelFactory = new ModelFactory(typeof(assignment).Assembly);
    }
  }

  public class ConcurrentAssignmentSubsystem : SubSystemDefinition<ConcurrentAssignmentSubsystem.State>
  {
    public enum State
    {
      A,
      B,
      C
    }

    public ConcurrentAssignmentSubsystem()
    {
      // @formatter:off
      Subsystem(concurrent.subsystem)
        .Initial(State.A)
        .Define(State.A)
          .Transitions
             .When(GreaterThan(concurrent.starter, constant.parameters.percentage.one)).Then(State.B)
        .Define(State.B)
          .OnEntry
            .Set(concurrent.value, constant.parameters.percentage.hundred)
          .Transitions
             .When(LowerThan(concurrent.value, constant.parameters.percentage.one)).Then(State.C)
        .Define(State.C)
          .OnEntry
            .Set(concurrent.check, concurrent.value)
        ;
      // @formatter:on
    }
  }
  
  public class concurrent : SubSystemNode
  {
    public static concurrent subsystem => new concurrent();
    public static PropertyUrn<Percentage> starter => PropertyUrn<Percentage>.Build(subsystem.Urn, nameof(starter));
    public static PropertyUrn<Percentage> value => PropertyUrn<Percentage>.Build(subsystem.Urn, nameof(value));
    public static PropertyUrn<Percentage> check => PropertyUrn<Percentage>.Build(subsystem.Urn, nameof(check));

    public concurrent() : base(nameof(concurrent), new examples())
    {
    }
  }
}