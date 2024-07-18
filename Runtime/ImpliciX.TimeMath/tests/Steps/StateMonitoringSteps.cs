using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Control;
using ImpliciX.Language.Core;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Collections;
using ImpliciX.TimeMath.Access;
using ImpliciX.TimeMath.Tests.Helpers;
using NUnit.Framework;
using TechTalk.SpecFlow;

namespace ImpliciX.TimeMath.Tests.Steps;

[Binding]
public class StateMonitoringSteps
{
  private readonly ScenarioContext _scenarioContext;
  private static string[] _specialHeaders = new[] { "time", "start", "end" };

  public StateMonitoringSteps(ScenarioContext scenarioContext)
  {
    _scenarioContext = scenarioContext;
  }

  [Given(@"a (.*) property of StandBy/Running enum type")]
  public void GivenAPropertyOfStandByRunningEnumType(string propertyUrn)
  {
    _scenarioContext.Get<ExpressionsFactory>().AddProperty<Status>(propertyUrn);
  }

  public enum Status
  {
    StandBy,
    Running
  }


  [Given(@"a (.*) metric defined as")]
  public void GivenAMetricDefinedAs(string metricUrn, Table table)
  {
    var definition = _scenarioContext.Get<ExpressionsFactory>()
      .CreateMetricDefinition(metricUrn, table.Rows[0].Values.First());
    var metricInfoSet = CreateMetricInfos.Execute(
      new[]
      {
        definition.Builder.Build<Metric<MetricUrn>>()
      }, Array.Empty<ISubSystemDefinition>());
    _scenarioContext.Set(metricInfoSet);
  }

  [When(@"the application starts at (.*)")]
  [When(@"the application restarts at (.*)")]
  public void WhenTheApplicationStartsAt(string startTime)
  {
    var startTimeSpan = TimeSpan.Parse(startTime);
    var metricInfoSet = _scenarioContext.Get<MetricInfoSet>();
    var db = _scenarioContext.Get<TimeSeriesDb>();
    var service = new TimeMathService(() => startTimeSpan);
    var reader = new TimeBasedTimeMathReader(db);
    var writer = new TimeBasedTimeMathWriter(db);
    var initialEvents = service.Initialize(
      metricInfoSet, writer, reader
    ).Cast<DomainEvent>().ToArray();
    _scenarioContext.Set(initialEvents, "initialEvents");
    var context = new TimeMathServiceTestContext(service);
    _scenarioContext.Set(context);
    _scenarioContext.Set(startTimeSpan, "startTime");
  }

  [Then(@"the following publications occur")]
  public void ThenTheFollowingPublicationsOccur(Table table)
  {
    var periodStart = _scenarioContext.Get<TimeSpan>("startTime");
    foreach (var row in table.Rows)
    {
      Option<TimeSpan> GetTime(string header) =>
        row.TryGetValue(header, out var cell)
          ? TimeSpan.TryParse(cell, out var time)
            ? Option<TimeSpan>.Some(time)
            : Option<TimeSpan>.None()
          : Option<TimeSpan>.None();

      var time = GetTime("time").GetValue();
      var start = GetTime("start");
      var end = GetTime("end");
      var rowProperties = GetPropertiesFor(table, row);
      var hasPublish =
        start.IsSome
        || end.IsSome
        || rowProperties.Keys.Any(urn => urn is MetricUrn);
      var isTrigger = !hasPublish && rowProperties.Any();
      if (isTrigger)
        Receive(time, rowProperties);
      else
        periodStart = hasPublish
          ? AdvanceTimeAndExpect(periodStart, time, rowProperties, start, end)
          : AdvanceTimeAndExpectNoPublication(periodStart, time);
    }
  }

  private void Receive(TimeSpan time, IDictionary<Urn, object> properties)
  {
    var expressions = _scenarioContext.Get<ExpressionsFactory>();
    var changed = properties
      .Select(x => expressions.GetProperty(x.Key, x.Value, time))
      .ToArray();
    var events = _scenarioContext
      .Get<TimeMathServiceTestContext>()
      .ChangeValues(time, changed);
  }

  private TimeSpan AdvanceTimeAndExpectNoPublication(TimeSpan periodStart, TimeSpan time)
  {
    var actuallyPublishedProperties = AdvanceTimeAndGetPublishedProperties(time).ToArray();
    Assert.That(actuallyPublishedProperties, Is.Empty);
    return time;
  }

  private TimeSpan AdvanceTimeAndExpect(
    TimeSpan periodStart,
    TimeSpan time,
    IDictionary<Urn, object> expectedPublishedProperties,
    Option<TimeSpan> start,
    Option<TimeSpan> end)
  {
    var actuallyPublishedProperties = AdvanceTimeAndGetPublishedProperties(time).ToArray();
    CheckPublishedProperties(periodStart, time, expectedPublishedProperties, actuallyPublishedProperties);
    CheckSamplingDate(nameof(MetricValue.SamplingStartDate), start,
      actuallyPublishedProperties, x => x.SamplingStartDate);
    CheckSamplingDate(nameof(MetricValue.SamplingEndDate), end,
      actuallyPublishedProperties, x => x.SamplingEndDate);
    return time;
  }

  private static void CheckSamplingDate(
    string title,
    Option<TimeSpan> expected,
    IDataModelValue[] actuallyPublishedProperties,
    Func<MetricValue, TimeSpan> getTimeOfMetric
  )
  {
    if (expected.IsNone)
      return;
    foreach (var property in actuallyPublishedProperties)
      Assert.That(
        getTimeOfMetric((MetricValue)property.ModelValue()),
        Is.EqualTo(expected.GetValue()),
        $"{title} of {property.Urn}");
  }

  private static void CheckPublishedProperties(
    TimeSpan periodStart,
    TimeSpan time,
    IDictionary<Urn, object> expectedPublishedProperties,
    IEnumerable<IDataModelValue> actuallyPublishedProperties)
  {
    var expectedProperties = expectedPublishedProperties.ToDictionary(
      x => x.Key,
      x => (float)x.Value
    );
    var checkedProperties = actuallyPublishedProperties
      .Where(mv => expectedPublishedProperties.ContainsKey(mv.Urn))
      .ToDictionary(mv => mv.Urn, mv => ((MetricValue)mv.ModelValue()).Value);
    Assert.That(checkedProperties, Is.EquivalentTo(expectedProperties), $"At time {time}");
    // Check.WithCustomMessage($"At time {time}").That(checkedProperties).IsEquivalentTo(expected);
  }

  private IEnumerable<IDataModelValue> AdvanceTimeAndGetPublishedProperties(TimeSpan time)
  {
    var events = GetDomainEventsForTime(time);
    var propertiesChanged = events
      .Where(e => e is PropertiesChanged)
      .Cast<PropertiesChanged>()
      .SelectMany(pc => pc.ModelValues);
    return propertiesChanged;
  }

  private DomainEvent[] GetDomainEventsForTime(TimeSpan time)
  {
    var initialEvents = _scenarioContext.Get<DomainEvent[]>("initialEvents");
    if (initialEvents.Length == 0)
      return _scenarioContext
        .Get<TimeMathServiceTestContext>()
        .AdvanceTimeTo(time);

    foreach (var initialEvent in initialEvents)
      Assert.That(time, Is.EqualTo(initialEvent.At),
        () => $"Initial event {initialEvent} was not properly checked");
    _scenarioContext.Set(Array.Empty<DomainEvent>(), "initialEvents");
    return initialEvents;
  }

  private IDictionary<Urn, object> GetPropertiesFor(Table table, TableRow row)
  {
    var expressions = _scenarioContext.Get<ExpressionsFactory>();
    var propertiesChanged =
      from header in table.Header
      where !_specialHeaders.Contains(header)
      let cell = row[header]
      where !cell.IsEmpty()
      let urn = expressions.GetUrn(header)
      let value = expressions.GetPropertyUrnValue(urn, cell)
      select (urn, value);
    return propertiesChanged.ToDictionary(x => x.urn, x => x.value);
  }
}