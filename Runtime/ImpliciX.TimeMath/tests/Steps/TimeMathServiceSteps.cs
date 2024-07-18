using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Control;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Collections;
using ImpliciX.TestsCommon;
using ImpliciX.TimeMath.Access;
using NFluent;
using TechTalk.SpecFlow;
using MetricsDSL = ImpliciX.Language.Metrics.Metrics;

namespace ImpliciX.TimeMath.Tests.Steps;

[Binding]
public class TimeMathServiceSteps
{
  private const string Start = ".Start";
  private const string End = ".End";
  private const string MinuteColumnName = "minute";

  private readonly List<Metric<MetricUrn>> _measureMetrics = new ();
  private readonly List<string> _outputsMetrics = new ();
  private readonly ScenarioContext _scenarioContext;
  private Metric<MetricUrn> _measureMetric;
  private ScheduledRootMetric _scheduledRoot;

  private TimeMathServiceTestContext _timeMathContext;
  private ITimeMathReader _timeMathReader;
  private TimeMathService _timeMathService;

  private ITimeMathWriter _timeMathWriter;
  private WindowedMetric _window;

  public TimeMathServiceSteps(
    ScenarioContext scenarioContext
  )
  {
    _scenarioContext = scenarioContext;
    scenarioContext["measureMetrics"] = _measureMetrics;
    scenarioContext["outputsMetrics"] = _outputsMetrics;
  }

  [When(@"the application is restarted")]
  public void WhenTheApplicationIsRestarted()
  {
    WhenTheApplicationIsRestartedAt(0);
  }

  [When(@"the application is restarted at (.*)")]
  public void WhenTheApplicationIsRestartedAt(float at)
  {
    GivenATimeMathServiceAt(at);
    var measureMetrics = _scenarioContext.Get<List<Metric<MetricUrn>>>("measureMetrics");
    var timeMathWriter = _scenarioContext.Get<ITimeMathWriter>("timeMathWriter");
    var timeMathReader = _scenarioContext.Get<ITimeMathReader>("timeMathReader");
    var events = _scenarioContext.Get<List<DomainEvent>>("events");

    events.Clear();
    events.AddRange(
      _timeMathService.Initialize(
          CreateMetricInfos.Execute(measureMetrics, _noStateMachines),
          timeMathWriter,
          timeMathReader
        )
        .ToList()
    );

    Check.That(events).IsNotNull();
    _scenarioContext.Set(at, "lastMinute");
  }

  [Then(@"The system returns:")]
  public void ThenTheSystemReturns(Table table) => ScenarioContext.StepIsPending();

  [Before]
  public void Init()
  {
    _timeMathWriter = _scenarioContext.Get<ITimeMathWriter>("timeMathWriter");
    _timeMathReader = _scenarioContext.Get<ITimeMathReader>("timeMathReader");
    _scenarioContext["events"] = new List<DomainEvent>();
    _scenarioContext.Set(
      0.0f,
      "lastMinute"
    );
  }

  [Given(@"a TimeMath service start at (.*) minutes")]
  public void GivenATimeMathService(
    float minutes
  )
  {
    GivenATimeMathServiceAt(minutes);
  }

  [Given(@"a TimeMath service")]
  public void GivenATimeMathService()
  {
    GivenATimeMathServiceAt(0);
  }

  private void GivenATimeMathServiceAt(
    float minutes
  )
  {
    _timeMathService = new TimeMathService(() => TimeSpan.FromMinutes(minutes));
    _timeMathContext = new TimeMathServiceTestContext(_timeMathService);
    _scenarioContext["timeMathContext"] = _timeMathContext;
  }

  [Given(@"the ""(.*)"" service primary period is define to (.*) minutes")]
  public void GivenTheServicePrimaryPeriodIsDefineToMinutes(
    string outputUrn,
    int number
  )
  {
    var namedMetric = MetricsDSL.Metric(MetricUrn.Build(outputUrn));
    _scheduledRoot = namedMetric.Is.Every(number).Minutes;
    _outputsMetrics.Add(outputUrn);
  }

  [Given(@"the service has a window period of (.*) minutes")]
  public void GivenTheServiceHasAWindowPeriodOfMinutes(
    int number
  )
  {
    _window = _scheduledRoot.OnAWindowOf(number).Minutes;
  }

  private void AddMeasure(
    StandardMetric standardMetric
  )
  {
    _measureMetric = standardMetric
      .Builder
      .Build<Metric<MetricUrn>>();

    _measureMetrics.Add(_measureMetric);
  }

  [Then(@"I get the error ""(.*)""")]
  public void ThenIGetTheError(
    string message
  )
  {
    var ex = Check.ThatCode(
        () => _timeMathService.Initialize(
          CreateMetricInfos.Execute(_measureMetrics, _noStateMachines),
          _timeMathWriter,
          _timeMathReader
        )
      )
      .Throws<InvalidOperationException>()
      .Value;

    Check
      .That(ex.Message)
      .Contains(message);
  }

  [Then(@"I get TimeMaths event")]
  public void ThenIGetTimeMathsEvent()
  {
    var events = _timeMathService.Initialize(
      CreateMetricInfos.Execute(_measureMetrics, _noStateMachines),
      _timeMathWriter,
      _timeMathReader
    );

    Check.That(events).IsNotNull();
  }

  [Given(@"a ""(.*)"" Gauge Computer")]
  public void GivenAGaugeComputer(
    string inputUrn
  )
  {
    AddMeasure(_scheduledRoot.GaugeOf(inputUrn));
  }

  [Given(@"a ""(.*)"" Accumulator Computer")]
  public void GivenAAccumulatorComputer(
    string inputUrn
  )
  {
    AddMeasure(
      _window == null
        ? _scheduledRoot.AccumulatorOf(inputUrn)
        : _window.AccumulatorOf(inputUrn)
    );
  }

  [Given(@"a ""(.*)"" Variation Computer")]
  public void GivenAVariationComputer(
    string inputUrn
  )
  {
    AddMeasure(
      _window == null
        ? _scheduledRoot.VariationOf(inputUrn)
        : _window.VariationOf(inputUrn)
    );
  }

  [Given(@"the service is started")]
  [When(@"the service is started")]
  public void GivenTheGaugeServiceIsStarted()
  {
    ThenIGetTimeMathsEvent();
  }

  [Given(@"these ""(.*)"" are received:")]
  [Then(@"these ""(.*)"" are received:")]
  public void GivenTheseValuesAreReceived(
    string inputUrn,
    Table table
  )
  {
    var events = _scenarioContext.Get<List<DomainEvent>>("events");
    events.Clear();
    foreach (var row in table.Rows)
    {
      var minute = float.Parse(row[MinuteColumnName]);
      var value = float.Parse(row[inputUrn]);
      events.AddRange(
        _timeMathContext.ChangeValue(
          inputUrn,
          minute,
          value
        ).ToList()
      );
    }
  }

  [Given(@"the time now (.*)")]
  [When(@"the time now (.*)")]
  public void WhenTheTimeNow(
    float at
  )
  {
    var events = _scenarioContext.Get<List<DomainEvent>>("events");
    events.Clear();
    events.AddRange(
      _timeMathContext
        .AdvanceTimeTo(at)
        .ToList()
    );
  }

  [Then(@"I get (.*) TimeMaths Event")]
  public void ThenIGetEvent(
    int numberOfEvents
  )
  {
    var events = _scenarioContext.Get<List<DomainEvent>>("events");
    Check.That(events).IsNotNull();
    Check.That(events.Count).As("nb values").Equals(numberOfEvents);
  }

  [Then(@"the ""(.*)"" TimeMaths Event value should be (.*)")]
  public void ThenTheTimeMathsEventValueShouldBe(
    string source,
    float value
  )
  {
    var events = _scenarioContext.Get<List<DomainEvent>>("events");
    var domainEvent = CheckEventValue(
      source,
      value,
      events
    );
  }

  private static IDataModelValue CheckEventValue(
    string urn,
    float expectedValue,
    List<DomainEvent> events
  )
  {
    Check.That(events).IsNotNull();
    Check.That(events).Not.IsEmpty();
    var eventById = events.Select(@event => @event.As<PropertiesChanged>())
        .SelectMany(
          changed =>
            changed.ModelValues
        )
        .GroupBy(
          values =>
            values.Urn
        )
        .ToDictionary(
          g => g.Key.ToString(),
          g => g.ToList()
        )
      ;

    var domainEvent = eventById[urn].First();
    Check.That(domainEvent).IsNotNull();
    Check.That(domainEvent.ToFloat().Value).As(urn + "(" + domainEvent.At + ")").Equals(expectedValue);
    return domainEvent;
  }

  [Then(@"these events occurs before system tick:")]
  public void ThenTheseEventsOccursBeforeSystemTick(
    Table table
  )
  {
    var inputs = _measureMetrics.Select(
      metric =>
        metric.InputUrn
    );

    var allOutputUrns = _outputsMetrics.SelectMany(
          metric =>
            table.Header.Where(
              header =>
                header.StartsWith(metric)
            )
        )
        .ToArray()
      ;

    var outputUrns = allOutputUrns.Where(
          urn =>
            !urn.Contains(".")
        )
        .ToArray()
      ;

    var starts = allOutputUrns.Where(
          urn =>
            urn.EndsWith(Start)
        )
        .Select(
          urn =>
            urn.Split(".")[0]
        )
        .ToArray()
      ;

    var ends = allOutputUrns.Where(
          urn =>
            urn.EndsWith(End)
        )
        .Select(
          urn =>
            urn.Split(".")[0]
        )
        .ToArray()
      ;

    var events = _scenarioContext.Get<List<DomainEvent>>("events");
    var lastMinute = _scenarioContext.Get<float>("lastMinute");
    foreach (var row in table.Rows)
    {
      var minute = float.Parse(row[MinuteColumnName]);
      events.AddRange(
        inputs
          .Where(
            inputUrn =>
              !row[inputUrn].IsEmpty()
          )
          .SelectMany(
            inputUrn =>
              _timeMathContext.ChangeValue(
                inputUrn,
                minute,
                float.Parse(row[inputUrn])
              ).ToList()
          )
      );

      if (minute > lastMinute)
        events.AddRange(
          _timeMathContext
            .AdvanceTimeTo(minute)
            .ToList()
        );

      _scenarioContext.Set(
        lastMinute,
        "lastMinute"
      );

      foreach (var outputUrn in outputUrns)
      {
        var content = row[outputUrn];
        if (content.IsEmpty()) continue;
        var value = float.Parse(content);
        var domainEvent = (Property<MetricValue>) CheckEventValue(
          outputUrn,
          value,
          events
        );

        var infoMinute = $"({row[MinuteColumnName]})";
        if (starts.Contains(outputUrn))
        {
          var expectedStart = TimeSpan.FromMinutes(float.Parse(row[outputUrn + Start]));
          Check.That(domainEvent.Value.SamplingStartDate).As($"start{infoMinute}").Equals(expectedStart);
        }

        if (ends.Contains(outputUrn))
        {
          var expectedEnd = TimeSpan.FromMinutes(float.Parse(row[outputUrn + End]));
          Check.That(domainEvent.Value.SamplingEndDate).As($"end{infoMinute}").Equals(expectedEnd);
        }

        Check.That(domainEvent.At).As($"publish{infoMinute}").Equals(TimeSpan.FromMinutes(minute));
      }

      events.Clear();
    }
  }
  
  private readonly ISubSystemDefinition[] _noStateMachines = Array.Empty<ISubSystemDefinition>();

}
